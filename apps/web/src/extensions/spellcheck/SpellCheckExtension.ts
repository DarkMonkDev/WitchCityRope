import { Extension } from '@tiptap/core'
import { Plugin, PluginKey } from '@tiptap/pm/state'
import { Decoration, DecorationSet, EditorView } from '@tiptap/pm/view'
import { Node as ProseMirrorNode } from '@tiptap/pm/model'
import { ReactRenderer } from '@tiptap/react'
import tippy, { Instance as TippyInstance } from 'tippy.js'
import { SpellCheckSuggestions } from './SpellCheckSuggestions'
import './spellcheck.css'

// Singleton nspell instance — loaded once, shared across all editors
// eslint-disable-next-line @typescript-eslint/no-explicit-any
let nspellInstance: any = null
let loadingPromise: Promise<void> | null = null

const IGNORE_LIST_KEY = 'wcr-spellcheck-ignore'

function getIgnoreList(): Set<string> {
  try {
    const stored = localStorage.getItem(IGNORE_LIST_KEY)
    return stored ? new Set(JSON.parse(stored)) : new Set()
  } catch {
    return new Set()
  }
}

function saveIgnoreList(list: Set<string>): void {
  localStorage.setItem(IGNORE_LIST_KEY, JSON.stringify([...list]))
}

async function loadDictionary(): Promise<void> {
  if (nspellInstance) return
  if (loadingPromise) return loadingPromise

  loadingPromise = (async () => {
    const [nspellModule, affResponse, dicResponse] = await Promise.all([
      import('nspell'),
      fetch('/dictionaries/en.aff'),
      fetch('/dictionaries/en.dic'),
    ])

    const [affText, dicText] = await Promise.all([affResponse.text(), dicResponse.text()])

    const NSpell = nspellModule.default || nspellModule
    nspellInstance = NSpell(affText, dicText)
  })()

  return loadingPromise
}

interface TokenMatch {
  word: string
  start: number
  end: number
}

function tokenizeText(text: string): TokenMatch[] {
  const regex = /[a-zA-Z'\u2019]+/g
  const tokens: TokenMatch[] = []
  let match: RegExpExecArray | null

  while ((match = regex.exec(text)) !== null) {
    const word = match[0]
    // Skip: short words, ALL_CAPS, words with digits, email-like, URL-like
    if (word.length < 2) continue
    if (word === word.toUpperCase() && word.length > 1) continue

    tokens.push({
      word,
      start: match.index,
      end: match.index + word.length,
    })
  }

  return tokens
}

interface MisspelledWord {
  from: number
  to: number
  word: string
}

function checkDocument(
  doc: ProseMirrorNode,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  nspell: any,
  ignoreList: Set<string>
): MisspelledWord[] {
  const misspelled: MisspelledWord[] = []

  doc.descendants((node, pos) => {
    if (!node.isText || !node.text) return

    const tokens = tokenizeText(node.text)
    for (const token of tokens) {
      if (ignoreList.has(token.word.toLowerCase())) continue
      if (!nspell.correct(token.word)) {
        misspelled.push({
          from: pos + token.start,
          to: pos + token.end,
          word: token.word,
        })
      }
    }
  })

  return misspelled
}

const spellCheckPluginKey = new PluginKey('spellCheck')
const RECHECK_META = 'spellCheckRecheck'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function createSpellCheckPlugin(editor: any): Plugin {
  let debounceTimer: ReturnType<typeof setTimeout> | null = null
  let activePopup: TippyInstance | null = null
  let activeComponent: ReactRenderer | null = null
  let ignoreList = getIgnoreList()

  function cleanupPopup() {
    if (activePopup) {
      activePopup.destroy()
      activePopup = null
    }
    if (activeComponent) {
      activeComponent.destroy()
      activeComponent = null
    }
  }

  function scheduleRecheck(view: EditorView) {
    if (debounceTimer) clearTimeout(debounceTimer)
    debounceTimer = setTimeout(async () => {
      if (!nspellInstance) return

      ignoreList = getIgnoreList()
      const misspelled = checkDocument(view.state.doc, nspellInstance, ignoreList)
      const decorations = misspelled.map((m) =>
        Decoration.inline(m.from, m.to, { class: 'spelling-error' }, { word: m.word })
      )

      const tr = view.state.tr.setMeta(RECHECK_META, DecorationSet.create(view.state.doc, decorations))
      view.dispatch(tr)
    }, 300)
  }

  return new Plugin({
    key: spellCheckPluginKey,

    state: {
      init() {
        return DecorationSet.empty
      },
      apply(tr, decorationSet) {
        const recheck = tr.getMeta(RECHECK_META)
        if (recheck) return recheck

        if (tr.docChanged) {
          return decorationSet.map(tr.mapping, tr.doc)
        }

        return decorationSet
      },
    },

    props: {
      decorations(state) {
        return spellCheckPluginKey.getState(state) as DecorationSet
      },

      handleClick(view, pos) {
        const decos = (spellCheckPluginKey.getState(view.state) as DecorationSet).find(pos, pos)
        const deco = decos.find((d) => d.spec?.word)
        if (!deco) {
          cleanupPopup()
          return false
        }

        cleanupPopup()

        const word = deco.spec.word as string
        const from = deco.from
        const to = deco.to
        const suggestions = nspellInstance ? nspellInstance.suggest(word).slice(0, 8) : []

        const component = new ReactRenderer(SpellCheckSuggestions, {
          props: {
            suggestions,
            misspelledWord: word,
            onSelect: (selected: string) => {
              const tr = view.state.tr.replaceWith(from, to, view.state.schema.text(selected))
              view.dispatch(tr)
              cleanupPopup()
              view.focus()
            },
            onIgnore: () => {
              ignoreList.add(word.toLowerCase())
              saveIgnoreList(ignoreList)
              cleanupPopup()
              scheduleRecheck(view)
              view.focus()
            },
            onAddToDictionary: () => {
              ignoreList.add(word.toLowerCase())
              saveIgnoreList(ignoreList)
              if (nspellInstance) {
                nspellInstance.add(word)
              }
              cleanupPopup()
              scheduleRecheck(view)
              view.focus()
            },
          },
          editor,
        })

        activeComponent = component

        const coords = view.coordsAtPos(from)
        const virtualEl = {
          getBoundingClientRect: () => ({
            top: coords.top,
            bottom: coords.bottom,
            left: coords.left,
            right: coords.right,
            width: coords.right - coords.left,
            height: coords.bottom - coords.top,
            x: coords.left,
            y: coords.top,
            toJSON: () => ({}),
          }),
        }

        const [popup] = tippy('body', {
          getReferenceClientRect: () => virtualEl.getBoundingClientRect(),
          appendTo: () => document.body,
          content: component.element,
          showOnCreate: true,
          interactive: true,
          trigger: 'manual',
          placement: 'bottom-start',
        })

        activePopup = popup

        return true
      },
    },

    view(editorView) {
      // Start loading dictionary, then do initial check
      loadDictionary().then(() => {
        scheduleRecheck(editorView)
      })

      return {
        update(view) {
          scheduleRecheck(view)
        },
        destroy() {
          if (debounceTimer) clearTimeout(debounceTimer)
          cleanupPopup()
        },
      }
    },
  })
}

export const SpellCheckExtension = Extension.create({
  name: 'spellCheck',

  addProseMirrorPlugins() {
    return [createSpellCheckPlugin(this.editor)]
  },
})
