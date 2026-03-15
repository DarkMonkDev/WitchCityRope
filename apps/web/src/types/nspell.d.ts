declare module 'nspell' {
  interface NSpell {
    correct(word: string): boolean
    suggest(word: string): string[]
    add(word: string): void
    remove(word: string): void
    spell(word: string): { correct: boolean }
    wordCharacters(): string | null
  }

  function nspell(aff: string | Buffer, dic: string | Buffer): NSpell

  export default nspell
}
