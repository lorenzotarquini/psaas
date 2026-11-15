import { describe, expect, it } from 'vitest'
import { initTheme, setThemeMode } from './theme'

describe('theme', () => {
  it('applies the daisyUI theme of theme.css and remembers the choice', () => {
    setThemeMode('dark')
    expect(document.documentElement.dataset.theme).toBe('mysync-dark')
    expect(localStorage.getItem('mysync.theme')).toBe('dark')

    setThemeMode('light')
    initTheme()
    expect(document.documentElement.dataset.theme).toBe('mysync')
  })
})
