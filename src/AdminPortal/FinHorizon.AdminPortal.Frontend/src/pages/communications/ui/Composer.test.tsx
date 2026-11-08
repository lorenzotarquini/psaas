// @vitest-environment jsdom
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { Composer } from './Composer'

const send = vi.fn()
vi.mock('@/entities/catalog', () => ({ usePlans: () => ({ data: [] }), useModules: () => ({ data: [] }) }))
vi.mock('@/entities/communication', () => ({
  useCommunicationPreview: () => ({ data: { count: 2, sample: ['Alfa', 'Beta'] } }),
  useSendCommunication: () => ({ mutateAsync: send, error: null, isPending: false }),
}))

beforeAll(() => i18n.use(initReactI18next).init({ lng: 'cimode', interpolation: { escapeValue: false } }))
afterEach(cleanup)

describe('Composer', () => {
  it('asks for confirmation and goes back to the form on cancel, without sending', () => {
    render(<Composer selection={[]} onSent={() => {}} onClose={() => {}} />)

    fireEvent.click(screen.getByRole('button', { name: 'communications.send' }))
    expect(screen.getByRole('alertdialog')).toBeTruthy()

    // shortcut: jsdom flushes React after the click's default action, so it cannot reproduce the
    // re-submit on cancel that a real browser shows (fixed with keys in Composer); checked with Playwright.
    fireEvent.click(screen.getByRole('button', { name: 'unsaved.discard' }))
    expect(screen.queryByRole('alertdialog')).toBeNull()
    expect(send).not.toHaveBeenCalled()
  })
})
