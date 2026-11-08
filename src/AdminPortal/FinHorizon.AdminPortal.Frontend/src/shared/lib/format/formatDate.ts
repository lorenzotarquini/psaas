// "YYYY-MM-DD" is a calendar date: format it in UTC so no timezone shifts it by a day.
export const formatDate = (isoDate: string, language: string) =>
  new Date(isoDate).toLocaleDateString(language, { timeZone: 'UTC' })
