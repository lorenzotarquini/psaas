import type { UserImportRow } from '@/entities/user'

/** RFC 4180 lines: quotes around fields, "" inside quotes; comma or semicolon (Excel in Italian) as separator. */
export function parseCsv(text: string): string[][] {
  const firstLine = text.split(/\r?\n/, 1)[0] ?? ''
  const separator = (firstLine.match(/;/g)?.length ?? 0) > (firstLine.match(/,/g)?.length ?? 0) ? ';' : ','
  const rows: string[][] = []
  let row: string[] = []
  let field = ''
  let quoted = false
  const endRow = () => {
    row.push(field)
    rows.push(row)
    row = []
    field = ''
  }
  for (let i = 0; i < text.length; i++) {
    const c = text[i]
    if (quoted) {
      if (c === '"' && text[i + 1] === '"') {
        field += '"'
        i++
      } else if (c === '"') quoted = false
      else field += c
    } else if (c === '"') quoted = true
    else if (c === separator) {
      row.push(field)
      field = ''
    } else if (c === '\n' || c === '\r') {
      if (c === '\r' && text[i + 1] === '\n') i++
      endRow()
    } else field += c
  }
  if (field || row.length) endRow()
  return rows.filter((r) => r.some((value) => value.trim()))
}

// UC20: column names in Italian (as in the template) or English, any order.
const columns: Record<keyof UserImportRow, string[]> = {
  firstName: ['nome', 'firstname'],
  lastName: ['cognome', 'lastname'],
  email: ['email'],
  birthDate: ['datanascita', 'birthdate'],
  profiles: ['profili', 'profiles'],
}

/** The rows of the file, by header; null when a column is missing. */
export function readUserRows(text: string): UserImportRow[] | null {
  const [header, ...lines] = parseCsv(text)
  if (!header) return null
  const names = header.map((h) => h.trim().toLowerCase().replace(/[\s_]/g, ''))
  const index = Object.fromEntries(
    Object.entries(columns).map(([key, aliases]) => [key, names.findIndex((n) => aliases.includes(n))]),
  ) as Record<keyof UserImportRow, number>
  if (Object.entries(index).some(([key, i]) => i < 0 && key !== 'profiles')) return null
  return lines.map((line) => ({
    firstName: line[index.firstName]?.trim() ?? '',
    lastName: line[index.lastName]?.trim() ?? '',
    email: line[index.email]?.trim() ?? '',
    birthDate: line[index.birthDate]?.trim() ?? '',
    profiles: index.profiles < 0 ? '' : (line[index.profiles]?.trim() ?? ''),
  }))
}

export const csvTemplate = 'nome,cognome,email,dataNascita,profili\nGiulia,Ferri,giulia.ferri@example.com,1991-04-12,Officine Rinaldi Spa:Capoturno|Ceramiche Bassi Srl:Operatore\n'
