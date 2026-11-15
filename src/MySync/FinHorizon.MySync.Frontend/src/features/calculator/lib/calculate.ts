import { all, create, type MathType } from 'mathjs'

const math = create(all)

export type AngleUnit = 'deg' | 'rad'

export type Outcome = { value: number } | { error: 'invalid' | 'notReal' }

const toRadians = (degrees: number) => (degrees * Math.PI) / 180
const toDegrees = (radians: number) => (radians * 180) / Math.PI

/** In degrees the trigonometric functions take and give degrees; the scope wins over mathjs' own functions. */
const degreeFunctions = {
  sin: (x: number) => Math.sin(toRadians(x)),
  cos: (x: number) => Math.cos(toRadians(x)),
  tan: (x: number) => Math.tan(toRadians(x)),
  asin: (x: number) => toDegrees(Math.asin(x)),
  acos: (x: number) => toDegrees(Math.acos(x)),
  atan: (x: number) => toDegrees(Math.atan(x)),
}

/**
 * The keypad writes readable symbols (×, ÷, −, √, π); mathjs reads its own syntax.
 * As on pocket calculators, ln is the natural logarithm and log the base-10 one.
 * The window writes the decimal separator of the language: a comma is always a decimal comma here
 * (no keypad function takes two arguments).
 */
export const toMathSyntax = (expression: string) =>
  expression
    .replaceAll('×', '*')
    .replaceAll('÷', '/')
    .replaceAll('−', '-')
    .replaceAll('√', 'sqrt')
    .replaceAll('π', 'pi')
    .replace(/\blog\(/g, 'log10(')
    .replace(/\bln\(/g, 'log(')
    .replaceAll(',', '.')

/**
 * Evaluates an expression with mathjs (a parser, never JavaScript's eval). `Ans` is the previous result.
 * Rounded to 12 significant digits so that sin(30°) is 0.5 and not 0.49999999999999994.
 */
export function calculate(expression: string, angle: AngleUnit, ans = 0): Outcome {
  if (!expression.trim()) return { error: 'invalid' }
  let result: MathType
  try {
    result = math.evaluate(toMathSyntax(expression), { Ans: ans, ...(angle === 'deg' ? degreeFunctions : {}) }) as MathType
  } catch {
    return { error: 'invalid' }
  }
  if (typeof result !== 'number') return math.isComplex(result) ? { error: 'notReal' } : { error: 'invalid' }
  if (Number.isNaN(result)) return { error: 'notReal' }
  return { value: Number(result.toPrecision(12)) }
}
