# Technology Research: nspell for Custom Spell-Check in Tiptap/ProseMirror
<!-- Last Updated: 2026-03-15 -->
<!-- Version: 1.0 -->
<!-- Owner: Technology Researcher Agent -->
<!-- Status: Complete -->

## Executive Summary

**Decision Required**: Whether to use nspell (Hunspell-compatible JS spell checker) to build a custom ProseMirror spell-check plugin for the Tiptap editor, and if so, how to architect it.

**Recommendation**: nspell is viable but carries significant complexity. For an admin-only editor with a small audience, consider a **simpler word-list Set approach** first, reserving nspell for when suggestion quality matters enough to justify the cost.

**Key Factors**:
1. nspell works but has a ~300ms dictionary init time and ~5MB memory footprint
2. The dictionary files (en_US) are ~800KB uncompressed, must be lazy-loaded
3. Building the full ProseMirror plugin (tokenization, decoration, debouncing, suggestions popup) is ~300-500 lines of TypeScript

---

## 1. How nspell Works

### Dictionary Format
nspell uses standard Hunspell `.aff` + `.dic` file pairs:
- **`.aff` (affix) file**: Defines language rules -- prefix/suffix rules, replacement patterns, keyboard layout, character sets, compound word rules. For en_US, this file has **14 prefix rules and 59 suffix rules** (relatively small compared to languages like Italian with 489+2744).
- **`.dic` (dictionary) file**: One word per line, first line is approximate word count. Words can have affix flags appended with `/`. For en_US, approximately **49,000 root words** that expand via affix rules to cover ~100,000+ word forms.

### Initialization
```typescript
import nspell from 'nspell'
// dictionary-en exports { aff: Buffer, dic: Buffer }
import en from 'dictionary-en'

const spell = nspell(en)  // Synchronous -- blocks while parsing
```

The constructor parses the `.aff` file to build affix rules, then processes the `.dic` file to build an internal hash map of all valid word forms. This is a **synchronous, blocking operation**.

### Core API

| Method | Signature | Returns | Purpose |
|--------|-----------|---------|---------|
| `correct(word)` | `(string) => boolean` | `true`/`false` | Check if a word is spelled correctly |
| `suggest(word)` | `(string) => string[]` | Array of suggestions | Get spelling suggestions for a misspelled word |
| `spell(word)` | `(string) => {correct, forbidden, warn}` | Object | Detailed spell info (correct, forbidden, warning) |
| `add(word, model?)` | `(string, string?) => void` | void | Add a word to the runtime dictionary |
| `remove(word)` | `(string) => void` | void | Remove a word from the runtime dictionary |
| `wordCharacters()` | `() => string \| undefined` | String of chars | Returns extra word characters defined in .aff |
| `personal(dic)` | `(string) => void` | void | Load a personal dictionary (lines starting with `*` = forbidden) |
| `dictionary(dic)` | `(string) => void` | void | Add an extra dictionary document |

### Handling of Contractions, Possessives, Capitalization
- **Contractions**: nspell v2.1.5 specifically fixed "uppercase suggestions for contractions" -- it handles `don't`, `can't`, etc. via the affix rules
- **Possessives**: Handled through affix rules in en_US (e.g., `'s` suffixes)
- **Capitalization**: The `correct()` method is case-aware -- it checks capitalized forms, ALL CAPS forms, and lowercase forms per Hunspell's algorithm
- **No tokenizer included**: nspell explicitly does NOT include a tokenizer. You must split text into words yourself. This is intentional -- the README states "nspell does not contain a tokenizer but leaves many details up to implementors."

---

## 2. Dictionary Files

### Source Package
**Package**: `dictionary-en` (npm)
- **Version**: 4.0.0
- **Last Published**: ~2 years ago (2024)
- **Weekly Downloads**: Part of the wooorm/dictionaries monorepo
- **ESM Only**: Yes (`"type": "module"`)
- **License**: MIT (wrapper), dictionary files retain original license (usually LGPL/MPL)
- **Note**: `dictionary-en-us` is deprecated; use `dictionary-en` instead

The `dictionary-en` package wraps the en_US Hunspell dictionary from the wooorm/dictionaries repository, which normalizes dictionaries to UTF-8.

### File Sizes (en_US)

| File | Approximate Size (uncompressed) | Gzipped |
|------|-------------------------------|---------|
| `index.aff` (affix rules) | ~7 KB | ~2-3 KB |
| `index.dic` (word list) | ~700-800 KB | ~200-250 KB |
| **Total dictionary** | **~800 KB** | **~250 KB** |

Note: Exact sizes vary by distribution. The en_US dictionary is one of the smaller Hunspell dictionaries. Languages like Italian, German, and French have much larger dictionaries (causing known memory/performance issues with nspell).

### Lazy Loading

**Yes, dictionaries can and should be lazy-loaded.** Recommended approach:

```typescript
// Only load when editor gains focus
const loadSpellChecker = async () => {
  const [{ default: nspell }, { default: en }] = await Promise.all([
    import('nspell'),
    import('dictionary-en'),
  ])
  return nspell(en)
}
```

With Vite's code splitting, both `nspell` and `dictionary-en` would be split into a separate chunk, only downloaded when the admin opens an editor. This is critical for keeping the main bundle clean.

### Smaller Dictionary Alternatives

There are no official "small" en_US Hunspell dictionaries. However:
- You could use a **simple word list** instead (see Section 6)
- `an-array-of-english-words` npm package: ~275,000 words, 3.37 MB uncompressed (larger than Hunspell, but simpler)
- Custom word list: You could curate a 30,000-word list at ~300 KB

---

## 3. Performance

### Dictionary Initialization

From the nspell GitHub issue #9, the developer @wooorm documented these loading times:

| Language | Init Time | Notes |
|----------|-----------|-------|
| **English (en_US)** | **~295ms** | Acceptable -- 14 prefix + 59 suffix rules |
| French | ~3,712ms | Much larger affix rules |
| German | ~3,442ms | Much larger affix rules |
| Italian | ~24,802ms | Crashes/hangs, 489+2744 prefix/suffix rules |

**English is the best-case scenario** for nspell. 295ms init is acceptable if done once on editor focus, especially in a Web Worker.

### Individual Word Checking

No published benchmarks exist for `correct()` per-word speed. Based on the algorithm (hash map lookup of expanded word forms):
- **`correct(word)`**: Estimated **sub-millisecond** (<0.1ms) per word -- it's essentially a hash lookup
- **`suggest(word)`**: Estimated **5-50ms** per word depending on word length and edit distance. This is the expensive operation. typo-js is known to take **7+ seconds** on some words for suggestions; nspell is significantly faster but still not instant.

### Web Worker Compatibility

**Yes, nspell can run in a Web Worker.** It is pure JavaScript with no DOM dependencies. Recommended architecture:

```
Main Thread                    Web Worker
  |                               |
  |-- postMessage(words[]) -----> |
  |                               | spell.correct(word) for each
  |<-- postMessage(misspelled[])--|
  |                               |
  | (on click misspelled word)    |
  |-- postMessage(word) --------> |
  |                               | spell.suggest(word)
  |<-- postMessage(suggestions[])--|
```

This keeps the main thread completely free. The 295ms init happens in the worker thread, invisible to the user.

### Memory Footprint

nspell expands all affix rules at init time and stores all word variants in memory. For en_US:
- **Estimated memory**: ~3-5 MB for the expanded dictionary in a V8 heap
- This is a one-time cost per page load
- For admin-only usage, this is acceptable

---

## 4. Building the ProseMirror Plugin

### Architecture Overview

```
Tiptap Extension (SpellCheck)
  |
  +-- ProseMirror Plugin
  |     |-- Plugin State: DecorationSet of misspelled word underlines
  |     |-- apply(): On doc change, compute changed regions, recheck words
  |     |-- props.decorations(): Return current DecorationSet
  |
  +-- Web Worker (spell-check-worker.ts)
  |     |-- Loads nspell + dictionary-en on init
  |     |-- Handles "check" messages: receives words, returns misspelled
  |     |-- Handles "suggest" messages: receives word, returns suggestions
  |
  +-- Suggestion Popup (React component via tippy.js)
        |-- Shows suggestions on click/right-click of misspelled word
        |-- Handles "replace" and "add to dictionary" actions
```

### Step-by-Step Plugin Design

#### a. Tokenizing Document Text into Words

ProseMirror documents are tree-structured. You need to walk text nodes:

```typescript
// Conceptual approach - walk text nodes and extract word positions
doc.descendants((node, pos) => {
  if (node.isText && node.text) {
    const regex = /[a-zA-Z']+/g
    let match
    while ((match = regex.exec(node.text)) !== null) {
      words.push({
        word: match[0],
        from: pos + match.index,
        to: pos + match.index + match[0].length,
      })
    }
  }
})
```

Key considerations:
- The regex should handle apostrophes for contractions (`don't`, `it's`)
- Skip words inside code blocks or inline code marks
- Handle words split across text nodes (rare but possible with inline marks)

#### b. Checking Words Against nspell

Send the word list to the Web Worker, receive back which words are misspelled with their positions.

#### c. Creating DecorationSet

```typescript
// For each misspelled word, create an inline decoration
const decorations = misspelledWords.map(({ from, to, word }) =>
  Decoration.inline(from, to, {
    class: 'spell-error',
    'data-word': word,
  })
)
const decoSet = DecorationSet.create(doc, decorations)
```

CSS for the wavy underline:
```css
.spell-error {
  text-decoration: underline wavy red;
  text-decoration-skip-ink: none;
  cursor: pointer;
}
```

#### d. Incremental Re-checking (Changed Regions Only)

This is the most complex part. The ProseMirror lint example rechecks the entire doc on every change. For production:

1. **On each transaction**: Use `tr.mapping` to map existing decoration positions forward
2. **Identify changed ranges**: Use `tr.steps` to find what changed
3. **Expand to word boundaries**: Find the full words around each changed range
4. **Re-check only those words**: Send just the affected words to the worker
5. **Merge results**: Replace decorations in the changed range while keeping others

The ProseMirror approach uses plugin state with `init` and `apply`:

```typescript
// Simplified conceptual approach
state: {
  init(_, state) {
    // Check entire document on first load
    return checkAllWords(state.doc)  // returns DecorationSet
  },
  apply(tr, oldDecoSet, oldState, newState) {
    if (!tr.docChanged) return oldDecoSet
    // Map existing decorations through the change
    let decoSet = oldDecoSet.map(tr.mapping, tr.doc)
    // Find changed ranges, recheck those words
    // This is where debouncing ties in
    scheduleRecheck(tr, newState.doc)
    return decoSet  // return mapped set immediately, update later
  }
}
```

#### e. Debouncing

Check words only after the user stops typing for ~300ms. During typing:
- Map existing decorations forward (instant, keeps underlines in right place)
- Queue changed ranges
- After 300ms idle, send queued words to worker
- When worker responds, dispatch a transaction that updates the DecorationSet

#### f. Suggestion Popup

On click of a misspelled word:
1. Detect the click hit a `.spell-error` decoration
2. Extract the `data-word` attribute
3. Send to worker for `suggest()` call
4. Show a popup (tippy.js or similar) with suggestions
5. On suggestion click, replace the word range using ProseMirror commands
6. Include "Add to Dictionary" option that calls `spell.add(word)` via the worker

### Rough Lines of Code Estimate

| Component | Estimated LOC |
|-----------|---------------|
| Tiptap Extension wrapper | ~30 |
| ProseMirror Plugin (state, decorations, incremental) | ~150-200 |
| Web Worker (nspell init, message handling) | ~60-80 |
| Tokenizer (text-to-words with positions) | ~40-50 |
| Suggestion Popup component | ~80-100 |
| CSS | ~10-15 |
| **Total** | **~370-475 lines** |

This is a non-trivial but contained feature. The incremental checking logic is the hardest part to get right.

---

## 5. Integration with Tiptap and @mantine/tiptap

### Wrapping as a Tiptap Extension

Tiptap's `Extension.create()` has an `addProseMirrorPlugins()` method:

```typescript
import { Extension } from '@tiptap/core'
import { Plugin, PluginKey } from '@tiptap/pm/state'

const SpellCheck = Extension.create({
  name: 'spellCheck',

  addProseMirrorPlugins() {
    return [
      new Plugin({
        key: new PluginKey('spellCheck'),
        state: { /* init, apply */ },
        props: {
          decorations(state) { /* return DecorationSet */ },
          handleClick(view, pos, event) { /* show suggestions */ },
        },
      }),
    ]
  },
})
```

Then add to the editor:
```typescript
const editor = useEditor({
  extensions: [
    StarterKit,
    SpellCheck,  // Just add it to the list
    // ... other extensions
  ],
})
```

### Gotchas with @mantine/tiptap

1. **CSS conflicts**: Mantine's RichTextEditor applies its own styles. The `.spell-error` class needs sufficient specificity to show the wavy underline through Mantine's styles. May need `.mantine-RichTextEditor-content .spell-error` selector.

2. **spellcheck="true" attribute**: The current editor already sets `spellcheck: 'true'` in editorProps (line 225 of MantineTiptapEditor.tsx). If you add a custom spell checker, you should set this to `'false'` to disable browser native spell checking and avoid double-underlining.

3. **Popup z-index**: Mantine uses specific z-index layers. The suggestion popup (tippy.js) needs to be above Mantine's modal/overlay layers if the editor is inside a modal.

4. **No Mantine-specific issues**: The `@mantine/tiptap` wrapper is thin -- it provides styled toolbar components but does not interfere with ProseMirror plugins or decorations.

---

## 6. Alternatives to nspell

### Option A: Simple Word-List Set Approach

Instead of Hunspell, use a flat list of valid English words loaded into a JavaScript `Set`:

**Pros**:
- `Set.has(word)` is O(1), instant lookup
- No affix parsing, no complex init
- Init time: ~50ms to build Set from array
- Much simpler code (no `.aff` parsing)
- Can use a curated 30K-50K word list (~300-500 KB)
- Memory: just the Set (~2-3 MB for 275K words)

**Cons**:
- No morphological awareness -- every word form must be in the list
- No suggestions (you'd need to implement edit-distance yourself, or skip suggestions)
- No handling of compound words or complex affixes
- A 275K word list still misses many valid words (technical terms, proper nouns, newer words)
- No personal dictionary or affix-based word generation

**Verdict**: For admin-only event descriptions where most text is simple English, a word-list approach could work well enough. You lose suggestions but gain massive simplicity. The `an-array-of-english-words` npm package has 275K words but is 3.37 MB -- larger than the Hunspell dictionary.

### Option B: typo-js vs nspell

| Criteria | nspell | typo-js |
|----------|--------|---------|
| **Version** | 2.1.5 | 1.3.1 |
| **Last Published** | Jan 2025 (releases page) | Dec 2024 |
| **Weekly Downloads** | ~64K | ~198K |
| **GitHub Stars** | 292 | 550 |
| **Open Issues** | 8 | 26 |
| **TypeScript** | ESM, JS only (needs `@types/nspell` or manual) | Has TypeScript source (`ts/typo.ts`) |
| **Module Format** | ESM only | CJS + script tag |
| **Suggestion Speed** | Significantly faster (key advantage) | Can take **7+ seconds** on some words |
| **Browser Compatibility** | Good (pure JS, ESM) | Good (designed for browsers, Chrome ext origin) |
| **Dictionary Handling** | via separate `dictionary-*` packages | Bundles dict loading or accepts paths/strings |
| **Maintenance** | wooorm (prolific OSS author, many related packages) | Christopher Finke (single maintainer) |
| **Known Issues** | Italian/Portuguese dictionaries crash | Slow suggestions on long words |

**Verdict**: nspell wins on suggestion performance (critical for UX) and cleaner architecture. typo-js wins on download popularity and having TypeScript source. For this use case, **nspell is the better choice** because suggestion speed directly impacts user experience.

### Option C: Pre-built ProseMirror Spell Check Plugins

| Plugin | Status | Approach |
|--------|--------|----------|
| **prosemirror-spellchecker** (kofifus) | Abandoned (2017, 9 stars, 8 commits, uses typo-js, known bugs) | Client-side with typo-js |
| **prosemirror-proofread** (nullpointerexceptionkek) | Active-ish (10 stars, 28 commits, wraps external APIs) | API wrapper only, no client-side checking |
| **Tiptap Content AI** | Commercial (Tiptap Pro) | Tiptap's paid AI proofreading service |

**Verdict**: No viable pre-built plugin exists. prosemirror-spellchecker is dead. prosemirror-proofread could theoretically wrap nspell's `correct()` call, but it's designed for API-based services and adds unnecessary abstraction for client-side checking. A custom plugin is the only practical path.

---

## 7. Bundle Size Analysis

### nspell Library

| Metric | Value |
|--------|-------|
| npm unpacked size | ~85 KB (source + README) |
| Minified | ~25-30 KB (estimated, pure JS) |
| Minified + gzipped | ~8-10 KB (estimated) |
| Dependencies | 0 runtime dependencies |
| Tree-shakeable | Yes (ESM) |

### dictionary-en Package

| Metric | Value |
|--------|-------|
| npm unpacked size | ~800 KB (mostly the .dic file) |
| .aff file | ~7 KB |
| .dic file | ~700-800 KB |
| Gzipped total | ~250 KB |

### Total Bundle Impact

| Component | Raw | Gzipped |
|-----------|-----|---------|
| nspell library | ~30 KB | ~10 KB |
| dictionary-en (aff + dic) | ~800 KB | ~250 KB |
| **Total** | **~830 KB** | **~260 KB** |

### Code Splitting Strategy

**These MUST be code-split / lazy-loaded.** The dictionary is too large for the main bundle.

With Vite, dynamic `import()` automatically creates separate chunks:

```typescript
// This creates a separate chunk loaded on demand
const initSpellChecker = () => import('./spell-check-worker?worker')
```

The 260 KB gzipped dictionary would only be downloaded when an admin opens a rich text editor. For regular members browsing events, the cost is zero.

**Comparison to current bundle**: A typical Vite+React+Mantine app is 200-400 KB gzipped. Adding 260 KB for spell checking (lazy-loaded, admin-only) is acceptable but not trivial -- it roughly doubles the size of the initial admin editor experience.

---

## Risk Assessment

### Medium Risk
- **nspell's init blocking**: 295ms synchronous parse could cause a visible pause
  - **Mitigation**: Run in Web Worker; init on editor focus, not page load
- **Dictionary file size**: 800 KB uncompressed is significant for mobile
  - **Mitigation**: Lazy load + cache; admin-only so mobile bandwidth less critical
- **Incremental checking complexity**: Getting changed-region-only rechecking right is tricky
  - **Mitigation**: Start with full-document recheck (debounced); optimize to incremental later

### Low Risk
- **nspell en_US stability**: English dictionary is the best-tested and smallest; no known crashes
- **Tiptap integration**: addProseMirrorPlugins() is well-documented and widely used
- **Browser compatibility**: Pure JS, ESM, no native dependencies

---

## Recommendation

### For WitchCityRope (Admin-Only Editor)

**Primary Recommendation: nspell in a Web Worker**
**Confidence Level**: Medium-High (75%)

**Rationale**:
1. English dictionary init (295ms) is fast enough, especially in a worker
2. Suggestion quality matters for admin polish -- nspell provides real Hunspell-quality suggestions
3. The ~260 KB gzipped cost is acceptable when lazy-loaded for admin-only use
4. Pure JS + ESM + no dependencies = clean integration with Vite

**Alternative**: If suggestions are not needed (just red underlines, no "did you mean?"), a simple word-list `Set` approach would cut complexity by ~60% and eliminate the dictionary dependency entirely. You would lose suggestion accuracy but gain simplicity.

### Implementation Priority

This is a **nice-to-have polish feature** for admin experience. It does not affect core functionality, safety, or member-facing pages. Implement after higher-priority features are complete.

### Estimated Implementation Effort

- **Web Worker + nspell integration**: 2-3 hours
- **ProseMirror plugin (basic, full-doc recheck)**: 3-4 hours
- **Suggestion popup UI**: 2-3 hours
- **Incremental checking optimization**: 2-4 hours (can defer)
- **Testing and edge cases**: 2-3 hours
- **Total**: ~11-17 hours for full implementation, or ~7-10 hours for basic version without incremental optimization

---

## Research Sources

- [nspell GitHub repository](https://github.com/wooorm/nspell) - API, architecture, dictionary format
- [nspell npm](https://www.npmjs.com/package/nspell) - Package details
- [nspell Issue #9](https://github.com/wooorm/nspell/issues/9) - Init time benchmarks (295ms for en_US)
- [wooorm/dictionaries](https://github.com/wooorm/dictionaries) - Dictionary packages
- [dictionary-en npm](https://www.npmjs.com/package/dictionary-en) - English dictionary package
- [typo-js GitHub](https://github.com/cfinke/Typo.js/) - Alternative spell checker comparison
- [npmtrends: nspell vs typo-js](https://npmtrends.com/nspell-vs-typo-js) - Download comparison
- [prosemirror-spellchecker](https://github.com/kofifus/prosemirror-spellchecker) - Existing (abandoned) PM plugin
- [prosemirror-proofread](https://github.com/nullpointerexceptionkek/prosemirror-proofread) - API wrapper plugin
- [ProseMirror lint example](https://prosemirror.net/examples/lint/) - Decoration pattern reference
- [Tiptap Extension API](https://tiptap.dev/docs/editor/extensions/custom-extensions/create-new/extension) - Custom extension docs
- [an-array-of-english-words](https://www.npmjs.com/package/an-array-of-english-words) - Word list alternative
- [Bundlephobia](https://bundlephobia.com/package/nspell) - Bundle size analysis tool
