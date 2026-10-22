/** "40121–40141" for a city with many postal codes (mock 1e), the code itself when there is one. */
export function postalCodeRange(codes: string[]) {
  if (codes.length === 0) return ''
  const sorted = [...codes].sort()
  return sorted.length === 1 ? sorted[0] : `${sorted[0]}–${sorted.at(-1)}`
}

/** Splits a city name around the typed text, to highlight it in the suggestions. */
export function highlight(name: string, typed: string) {
  const at = name.toLowerCase().indexOf(typed.trim().toLowerCase())
  if (!typed.trim() || at < 0) return { before: name, match: '', after: '' }
  const end = at + typed.trim().length
  return { before: name.slice(0, at), match: name.slice(at, end), after: name.slice(end) }
}
