import { describe, expect, it } from 'vitest'
import { calculate } from './calculate'

describe('calculate', () => {
  it.each([
    ['2+3×4', 14],
    ['(2+3)×4', 20],
    ['10÷4', 2.5],
    ['2^10', 1024],
    ['√(16)', 4],
    ['5!', 120],
    ['3,5×2', 7],
    ['22,242−30', -7.758],
    ['ln(e)', 1],
    ['log(1000)', 3],
    ['−2+π', Number((Math.PI - 2).toPrecision(12))],
  ])('%s = %d', (expression, expected) => expect(calculate(expression, 'rad')).toEqual({ value: expected }))

  it('works in degrees or radians', () => {
    expect(calculate('sin(30)', 'deg')).toEqual({ value: 0.5 })
    expect(calculate('asin(1)', 'deg')).toEqual({ value: 90 })
    expect(calculate('cos(π)', 'rad')).toEqual({ value: -1 })
  })

  it('reuses the previous result as Ans', () => expect(calculate('Ans×2', 'rad', 21)).toEqual({ value: 42 }))

  it('reports what it cannot compute', () => {
    expect(calculate('2+', 'rad')).toEqual({ error: 'invalid' })
    expect(calculate('', 'rad')).toEqual({ error: 'invalid' })
    expect(calculate('√(−1)', 'rad')).toEqual({ error: 'notReal' })
  })
})
