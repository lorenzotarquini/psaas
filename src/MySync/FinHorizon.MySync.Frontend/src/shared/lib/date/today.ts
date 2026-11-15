/** Today as a calendar date (yyyy-MM-dd) in the user's time zone, the default date of a new movement. */
export function todayIso(now: Date = new Date()): string {
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000)
  return local.toISOString().slice(0, 10)
}
