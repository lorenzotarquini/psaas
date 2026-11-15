import { useLayoutEffect, useRef, useState, type KeyboardEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCopy } from '@fortawesome/free-regular-svg-icons'
import { faCheck, faDeleteLeft } from '@fortawesome/free-solid-svg-icons'
import { calculate, type AngleUnit, type Outcome } from '../lib/calculate'
import { sanitizePasted } from '../lib/sanitizePasted'

type Tone = 'digit' | 'operator' | 'function' | 'danger'

interface Key {
  label: string
  /** What the key writes in the expression; keys with an action write nothing. */
  insert?: string
  action?: 'clear' | 'backspace' | 'angle' | 'equals'
  tone: Tone
  /** For screen readers when the label is a symbol. */
  ariaKey?: string
}

const fn = (label: string, insert = `${label}(`): Key => ({ label, insert, tone: 'function' })
const op = (label: string, insert = label): Key => ({ label, insert, tone: 'operator' })
const digit = (label: string): Key => ({ label, insert: label, tone: 'digit' })

// Five columns: scientific functions on top, the usual keypad below.
const keys: Key[] = [
  { label: 'DEG', action: 'angle', tone: 'function', ariaKey: 'calculator.angle' },
  fn('π', 'π'),
  fn('e', 'e'),
  op('('),
  op(')'),
  fn('sin'),
  fn('cos'),
  fn('tan'),
  fn('ln'),
  fn('log'),
  fn('sin⁻¹', 'asin('),
  fn('cos⁻¹', 'acos('),
  fn('tan⁻¹', 'atan('),
  fn('eˣ', 'e^'),
  fn('10ˣ', '10^'),
  fn('√', '√('),
  fn('x²', '^2'),
  fn('xʸ', '^'),
  fn('x!', '!'),
  fn('1/x', '^(-1)'),
  digit('7'),
  digit('8'),
  digit('9'),
  op('÷'),
  { label: '⌫', action: 'backspace', tone: 'danger', ariaKey: 'calculator.backspace' },
  digit('4'),
  digit('5'),
  digit('6'),
  op('×'),
  { label: 'AC', action: 'clear', tone: 'danger', ariaKey: 'calculator.clear' },
  digit('1'),
  digit('2'),
  digit('3'),
  op('−'),
  { label: 'Ans', insert: 'Ans', tone: 'function', ariaKey: 'calculator.ans' },
  digit('0'),
  digit('.'),
  fn('|x|', 'abs('),
  op('+'),
  { label: '=', action: 'equals', tone: 'operator', ariaKey: 'calculator.equals' },
]

// daisyUI classes only: the calculator follows the light and dark theme like every other control.
const toneClass: Record<Tone, string> = {
  digit: 'btn-neutral btn-soft',
  operator: 'btn-primary btn-soft',
  function: 'btn-ghost bg-base-200',
  danger: 'btn-error btn-soft',
}

/** Keys that write themselves into the expression when typed anywhere in the window. */
const typable = /^[\d.,+\-*/^()!%a-zA-Z]$/

/**
 * A scientific calculator: the keypad or the keyboard write the expression, Enter or "=" evaluate it with mathjs.
 * The expression is a normal text field (copy and paste work) and the result can be selected or copied.
 */
export default function Calculator() {
  const { t, i18n } = useTranslation()
  const [copied, setCopied] = useState(false)
  const input = useRef<HTMLInputElement>(null)
  /** Where the caret goes after a key wrote into the expression; applied right after the render. */
  const caret = useRef<number | null>(null)
  const [expression, setExpression] = useState('')
  const [angle, setAngle] = useState<AngleUnit>('deg')
  const [outcome, setOutcome] = useState<Outcome | null>(null)
  const [ans, setAns] = useState(0)

  // One convention per language for typing and reading: the decimal separator of the language (a comma in Italian)
  // and no thousands separator, so "22242" and "22,242" can never be mistaken for each other.
  const numbers = new Intl.NumberFormat(i18n.language, { useGrouping: false, maximumFractionDigits: 10 })
  const decimal = numbers.formatToParts(1.5).find((part) => part.type === 'decimal')?.value ?? '.'
  /** Dot or comma, whichever is typed or pasted, is the decimal separator. */
  const localize = (text: string) => text.replace(/[.,]/g, decimal)

  /** Writes at the caret, so the expression can be fixed in the middle as well. */
  function insert(raw: string) {
    const text = localize(raw)
    const field = input.current
    const start = field?.selectionStart ?? expression.length
    const end = field?.selectionEnd ?? expression.length
    setExpression(expression.slice(0, start) + text + expression.slice(end))
    setOutcome(null)
    caret.current = start + text.length
  }

  // Before the next key arrives, so fast typing never lands in the wrong place.
  useLayoutEffect(() => {
    if (caret.current === null) return
    input.current?.focus()
    input.current?.setSelectionRange(caret.current, caret.current)
    caret.current = null
  })

  function backspace() {
    setExpression(expression.slice(0, -1))
    setOutcome(null)
  }

  /** The keyboard works wherever the focus is in the window, not only in the text field. */
  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.ctrlKey || event.metaKey || event.altKey) return
    const inField = event.target === input.current
    if (event.key === 'Enter' || event.key === '=') {
      event.preventDefault()
      evaluate()
    } else if (!inField && event.key === 'Backspace') {
      event.preventDefault()
      backspace()
    } else if (!inField && typable.test(event.key)) {
      event.preventDefault()
      insert(event.key)
    }
  }

  /** Without thousands separators, so it pastes into an amount field of MySync as it is. */
  async function copyResult() {
    if (!outcome || !('value' in outcome)) return
    await navigator.clipboard.writeText(numbers.format(outcome.value))
    setCopied(true)
    setTimeout(() => setCopied(false), 1500)
  }

  function evaluate() {
    const result = calculate(expression, angle, ans)
    setOutcome(result)
    if ('value' in result) setAns(result.value)
  }

  function press(key: Key) {
    if (key.insert !== undefined) return insert(key.insert)
    if (key.action === 'equals') return evaluate()
    if (key.action === 'clear') {
      setExpression('')
      setOutcome(null)
    }
    if (key.action === 'backspace') backspace()
    if (key.action === 'angle') setAngle(angle === 'deg' ? 'rad' : 'deg')
    input.current?.focus()
  }

  const result = outcome === null ? '' : 'value' in outcome ? numbers.format(outcome.value) : t(`calculator.errors.${outcome.error}`)

  return (
    // Keyboard shortcuts for the whole window; every key also has its button.
    <div className="flex flex-col gap-3" onKeyDown={onKeyDown}>
      <div className="rounded-box bg-base-200 flex flex-col gap-1 p-3">
        <div className="flex items-center justify-between">
          <span className="badge badge-ghost badge-sm">{angle === 'deg' ? 'DEG' : 'RAD'}</span>
          {ans !== 0 && <span className="muted text-xs">Ans = {numbers.format(ans)}</span>}
        </div>
        <input
          ref={input}
          autoFocus
          aria-label={t('calculator.expression')}
          className="w-full bg-transparent text-right font-mono text-lg outline-none"
          inputMode="none"
          autoComplete="off"
          spellCheck={false}
          value={expression}
          onPaste={(event) => {
            // Pasted numbers lose their thousands separator: "1.234,5" is 1234,5 in Italian, "1,234.5" is 1234.5 in English.
            event.preventDefault()
            insert(sanitizePasted(event.clipboardData.getData('text'), decimal))
          }}
          onChange={(event) => {
            setExpression(localize(event.target.value))
            setOutcome(null)
          }}
        />
        <div className="flex min-h-8 items-center justify-end gap-2">
          {outcome && 'value' in outcome && (
            <button
              type="button"
              className="btn btn-ghost btn-xs btn-circle"
              aria-label={t(copied ? 'calculator.copied' : 'calculator.copy')}
              title={t(copied ? 'calculator.copied' : 'calculator.copy')}
              onClick={() => void copyResult()}
            >
              <FontAwesomeIcon icon={copied ? faCheck : faCopy} />
            </button>
          )}
          <output
            aria-label={t('calculator.result')}
            aria-live="polite"
            className={`select-text text-right text-2xl font-semibold ${outcome && 'error' in outcome ? 'text-error text-base' : ''}`}
          >
            {result}
          </output>
        </div>
      </div>
      <div className="grid grid-cols-5 gap-1.5">
        {keys.map((key) => (
          <button
            key={key.label}
            type="button"
            className={`btn btn-sm h-10 font-medium normal-case ${toneClass[key.tone]} ${key.action === 'equals' ? 'btn-active' : ''}`}
            aria-label={key.ariaKey ? t(key.ariaKey) : undefined}
            aria-pressed={key.action === 'angle' ? angle === 'rad' : undefined}
            // The keys never take the focus away from the expression: the keyboard keeps typing into it.
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => press(key)}
          >
            {key.action === 'backspace' ? (
              <FontAwesomeIcon icon={faDeleteLeft} />
            ) : key.action === 'angle' ? (
              angle === 'deg' ? 'DEG' : 'RAD'
            ) : key.label === '.' ? (
              decimal
            ) : (
              key.label
            )}
          </button>
        ))}
      </div>
    </div>
  )
}
