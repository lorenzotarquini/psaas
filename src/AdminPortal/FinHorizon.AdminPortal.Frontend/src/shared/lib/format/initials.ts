/** Two letters for an avatar: given and family name, else the first two of the fallback (username, email). */
export function initials(given?: string, family?: string, fallback = '') {
  const letters = given && family ? `${given[0]}${family[0]}` : fallback.slice(0, 2)
  return letters.toUpperCase()
}
