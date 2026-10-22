/**
 * Time of an event as in mock 1b: "10:31" today, "ieri 23:00" yesterday, otherwise "20/10/2026 15:02".
 * now is a parameter so the function stays pure (and testable).
 */
export function formatWhen(iso: string, language: string, yesterdayLabel: string, now = new Date()) {
  const date = new Date(iso)
  const time = date.toLocaleTimeString(language, { hour: '2-digit', minute: '2-digit' })
  const dayStart = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
  const days = Math.round((dayStart(now) - dayStart(date)) / 86_400_000)
  if (days === 0) return time
  if (days === 1) return `${yesterdayLabel} ${time}`
  return `${date.toLocaleDateString(language, { day: '2-digit', month: '2-digit', year: 'numeric' })} ${time}`
}
