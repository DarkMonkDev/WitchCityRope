import { useImperativeHandle, forwardRef, useState } from 'react'

export interface SpellCheckSuggestionsProps {
  suggestions: string[]
  misspelledWord: string
  onSelect: (word: string) => void
  onIgnore: () => void
  onAddToDictionary: () => void
}

export interface SpellCheckSuggestionsRef {
  onKeyDown: (event: KeyboardEvent) => boolean
}

export const SpellCheckSuggestions = forwardRef<
  SpellCheckSuggestionsRef,
  SpellCheckSuggestionsProps
>(({ suggestions, misspelledWord, onSelect, onIgnore, onAddToDictionary }, ref) => {
  const [selectedIndex, setSelectedIndex] = useState(0)
  // Total items: suggestions + 2 actions (ignore, add to dictionary)
  const totalItems = suggestions.length + 2

  useImperativeHandle(ref, () => ({
    onKeyDown: (event: KeyboardEvent) => {
      if (event.key === 'ArrowUp') {
        setSelectedIndex((prev) => (prev + totalItems - 1) % totalItems)
        return true
      }
      if (event.key === 'ArrowDown') {
        setSelectedIndex((prev) => (prev + 1) % totalItems)
        return true
      }
      if (event.key === 'Enter') {
        if (selectedIndex < suggestions.length) {
          onSelect(suggestions[selectedIndex])
        } else if (selectedIndex === suggestions.length) {
          onIgnore()
        } else {
          onAddToDictionary()
        }
        return true
      }
      if (event.key === 'Escape') {
        return true
      }
      return false
    },
  }))

  return (
    <div className="spell-suggestions">
      {suggestions.length > 0 ? (
        suggestions.map((word, index) => (
          <div
            key={word}
            className="spell-suggestion-item"
            data-selected={index === selectedIndex}
            onClick={() => onSelect(word)}
            onMouseEnter={() => setSelectedIndex(index)}
          >
            {word}
          </div>
        ))
      ) : (
        <div style={{ padding: '6px 12px', color: '#999', fontStyle: 'italic' }}>
          No suggestions for &ldquo;{misspelledWord}&rdquo;
        </div>
      )}
      <div className="spell-suggestion-divider" />
      <div
        className="spell-suggestion-action"
        data-selected={selectedIndex === suggestions.length}
        onClick={onIgnore}
        onMouseEnter={() => setSelectedIndex(suggestions.length)}
      >
        Ignore
      </div>
      <div
        className="spell-suggestion-action"
        data-selected={selectedIndex === suggestions.length + 1}
        onClick={onAddToDictionary}
        onMouseEnter={() => setSelectedIndex(suggestions.length + 1)}
      >
        Add to Dictionary
      </div>
    </div>
  )
})

SpellCheckSuggestions.displayName = 'SpellCheckSuggestions'
