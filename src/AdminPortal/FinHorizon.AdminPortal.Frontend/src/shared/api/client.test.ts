import { describe, expect, it } from 'vitest'
import { readFieldErrors } from './client'

const json = (body: unknown) => new Response(JSON.stringify(body), { status: 400 })

describe('readFieldErrors', () => {
  it('returns the field codes of a ValidationProblem', async () => {
    const errors = { companyName: ['Required'], 'profiles[1].name': ['NotUnique'] }
    expect(await readFieldErrors(json({ title: 'One or more validation errors occurred.', errors }))).toEqual(errors)
  })

  it('returns no errors when the problem has none', async () => {
    expect(await readFieldErrors(json({ title: 'Bad Request' }))).toEqual({})
  })

  it('returns no errors when the body is not JSON', async () => {
    expect(await readFieldErrors(new Response('oops', { status: 400 }))).toEqual({})
  })
})
