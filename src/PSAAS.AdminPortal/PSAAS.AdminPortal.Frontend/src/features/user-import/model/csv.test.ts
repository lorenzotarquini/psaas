import { describe, expect, it } from 'vitest'
import { csvTemplate, parseCsv, readUserRows } from './csv'

describe('parseCsv', () => {
  it('handles quotes, escaped quotes, CRLF and the Italian Excel semicolon', () => {
    expect(parseCsv('a;"b;c";"say ""hi"""\r\n1;2;3\r\n')).toEqual([
      ['a', 'b;c', 'say "hi"'],
      ['1', '2', '3'],
    ])
  })

  it('skips blank lines', () => {
    expect(parseCsv('a,b\n\n1,2')).toEqual([
      ['a', 'b'],
      ['1', '2'],
    ])
  })
})

describe('readUserRows', () => {
  it('reads the template by column name', () => {
    expect(readUserRows(csvTemplate)).toEqual([
      {
        firstName: 'Giulia',
        lastName: 'Ferri',
        email: 'giulia.ferri@example.com',
        birthDate: '1991-04-12',
        profiles: 'Officine Rinaldi Spa:Capoturno|Ceramiche Bassi Srl:Operatore',
      },
    ])
  })

  it('accepts English headers in any order and rejects a missing column', () => {
    expect(readUserRows('email,lastName,firstName,birthDate\na@x.it,Rossi,Mario,1980-01-01')?.[0]).toMatchObject({ firstName: 'Mario', profiles: '' })
    expect(readUserRows('nome,cognome\nMario,Rossi')).toBeNull()
  })
})
