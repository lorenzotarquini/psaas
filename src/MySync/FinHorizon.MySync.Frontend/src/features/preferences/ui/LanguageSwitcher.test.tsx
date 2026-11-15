import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import i18n from 'i18next'
import { afterEach, describe, expect, it } from 'vitest'
import { LanguageSwitcher } from './LanguageSwitcher'

describe('LanguageSwitcher', () => {
  afterEach(() => i18n.changeLanguage('it'))

  it('shows the flag of each language and changes language', async () => {
    const { container } = render(<LanguageSwitcher />)
    const user = userEvent.setup()

    expect(container.querySelector('.fi-it')).toBeTruthy()
    await user.click(screen.getByRole('button', { name: 'Lingua: Italiano' }))
    const options = screen.getAllByRole('option')
    expect(options.map((option) => option.querySelector('.fi')?.className)).toEqual(['fi fi-it rounded-sm', 'fi fi-gb rounded-sm'])

    await user.click(screen.getByRole('button', { name: 'English' }))

    expect(i18n.language).toBe('en')
    expect(screen.queryByRole('listbox')).toBeNull()
  })
})
