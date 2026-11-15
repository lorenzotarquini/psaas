import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { CalculatorButton } from './CalculatorButton'

// The first window downloads mathjs (lazy chunk): slower than the default waits.
const lazyLoad = { timeout: 15_000 }

describe('CalculatorButton', () => {
  async function openOne() {
    const user = userEvent.setup()
    render(<CalculatorButton />)
    await user.click(screen.getByRole('button', { name: 'Apri una calcolatrice' }))
    const window = await screen.findByRole('dialog', { name: 'Calcolatrice' })
    await within(window).findByLabelText('Espressione', undefined, lazyLoad)
    const press = async (...labels: string[]) => {
      for (const label of labels) await user.click(within(window).getByRole('button', { name: label }))
    }
    return { user, window, press }
  }

  it('computes from the keypad, with the precedence of the operators', { timeout: 20_000 }, async () => {
    const { window, press } = await openOne()

    await press('2', '+', '3', '×', '4', 'Calcola')

    expect(within(window).getByLabelText('Risultato').textContent).toBe('14')
  })

  it('takes the keyboard, in degrees by default', { timeout: 20_000 }, async () => {
    const { user, window, press } = await openOne()

    // The keypad keeps the focus on the expression: typed digits go on from there.
    await press('sin')
    await user.keyboard('30)=')

    expect(within(window).getByLabelText('Espressione')).toHaveProperty('value', 'sin(30)')
    expect(within(window).getByLabelText('Risultato').textContent).toBe('0,5')
  })

  it('uses the decimal comma of the language, with no thousands separator', { timeout: 20_000 }, async () => {
    const { user, window, press } = await openOne()

    await user.keyboard('22.242-30')
    await press('Calcola')

    expect(within(window).getByLabelText('Espressione')).toHaveProperty('value', '22,242-30')
    expect(within(window).getByLabelText('Risultato').textContent).toBe('-7,758')
    await press('Azzera', '1', '2', '0', '0', '0', '×', '2', 'Calcola')
    expect(within(window).getByLabelText('Risultato').textContent).toBe('24000')
  })

  it('drops the thousands separator from pasted numbers', { timeout: 20_000 }, async () => {
    const { user, window, press } = await openOne()

    await user.click(within(window).getByLabelText('Espressione'))
    await user.paste('1.234.567,89')
    await press('+', '1', 'Calcola')

    expect(within(window).getByLabelText('Espressione')).toHaveProperty('value', '1234567,89+1')
    expect(within(window).getByLabelText('Risultato').textContent).toBe('1234568,89')
  })

  it('says when the expression is not valid', { timeout: 20_000 }, async () => {
    const { window, press } = await openOne()

    await press('2', '+', 'Calcola')

    expect(within(window).getByLabelText('Risultato').textContent).toBe('Espressione non valida')
  })

  it('opens as many calculators as wanted and closes each one', { timeout: 20_000 }, async () => {
    const { user } = await openOne()
    await user.click(screen.getByRole('button', { name: 'Apri una calcolatrice' }))
    await user.click(screen.getByRole('button', { name: 'Apri una calcolatrice' }))
    expect(screen.getAllByRole('dialog')).toHaveLength(3)

    const second = screen.getByRole('dialog', { name: 'Calcolatrice 2' })
    await user.click(within(second).getByRole('button', { name: 'Chiudi' }))

    expect(screen.getAllByRole('dialog').map((dialog) => dialog.getAttribute('aria-label'))).toEqual(['Calcolatrice', 'Calcolatrice 3'])
  })
})
