import { faBuilding, faMagnifyingGlass, faUser, type IconDefinition } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { useGlobalSearch } from '@/entities/overview'
import { useDebouncedValue } from '@/shared/lib/debounce'
import { routes } from '@/shared/routes'
import { Kbd } from '@/shared/ui/kbd'
import { isTyping, paletteEntries, type PaletteEntry } from '../model/entries'

const icons: Record<PaletteEntry['group'], IconDefinition> = { tenants: faBuilding, users: faUser, actions: faMagnifyingGlass }

// UC30 (mock 1b–1f): the search bar of the header; ⌘K/Ctrl+K opens it, arrows and Enter pick, Esc closes. N = new tenant.
export function CommandPalette() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const dialog = useRef<HTMLDialogElement>(null)
  const [text, setText] = useState('')
  const [active, setActive] = useState(0)
  const term = useDebouncedValue(text.trim(), 200)
  const { data, isFetching } = useGlobalSearch(term)
  const entries = paletteEntries(term.length >= 2 ? data : undefined, text, t)

  const open = () => {
    setText('')
    setActive(0)
    dialog.current?.showModal()
  }
  const go = (entry: PaletteEntry | undefined) => {
    if (!entry) return
    dialog.current?.close()
    navigate(entry.to)
  }

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        open()
      } else if (e.key === 'n' && !e.metaKey && !e.ctrlKey && !e.altKey && !isTyping(e.target) && !dialog.current?.open) {
        navigate(routes.tenantNew)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [navigate])

  const onInputKey = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') setActive((i) => Math.min(i + 1, entries.length - 1))
    else if (e.key === 'ArrowUp') setActive((i) => Math.max(i - 1, 0))
    else if (e.key === 'Enter') go(entries[active])
    else return
    e.preventDefault()
  }

  return (
    <>
      <button type="button" onClick={open} className="input input-sm w-full max-w-md cursor-text justify-between text-base-content/50" aria-label={t('search.open')}>
        <span className="flex items-center gap-2">
          <FontAwesomeIcon icon={faMagnifyingGlass} />
          {t('search.placeholder')}
        </span>
        <Kbd>⌘K</Kbd>
      </button>
      <dialog ref={dialog} className="modal modal-top" aria-label={t('search.title')}>
        <div className="modal-box mx-auto mt-16 max-w-xl p-0">
          <label className="flex items-center gap-3 border-b border-base-300 px-4 py-3">
            <FontAwesomeIcon icon={faMagnifyingGlass} className="text-base-content/50" />
            <input
              autoFocus
              className="w-full bg-transparent outline-none"
              placeholder={t('search.placeholder')}
              aria-label={t('search.placeholder')}
              role="combobox"
              aria-expanded
              aria-controls="palette-results"
              aria-activedescendant={entries[active] ? `palette-${active}` : undefined}
              value={text}
              onChange={(e) => {
                setText(e.target.value)
                setActive(0)
              }}
              onKeyDown={onInputKey}
            />
            {isFetching && <span className="loading loading-spinner loading-xs" />}
          </label>
          <ul id="palette-results" role="listbox" className="max-h-96 overflow-y-auto py-1">
            {entries.map((entry, i) => (
              <li key={`${entry.group}-${entry.to}`} id={`palette-${i}`} role="option" aria-selected={i === active}>
                {(i === 0 || entries[i - 1].group !== entry.group) && (
                  <div className="eyebrow px-4 pt-2 pb-1 text-base-content/50">{t(`search.groups.${entry.group}`)}</div>
                )}
                <button
                  type="button"
                  className={`flex w-full items-center gap-3 px-4 py-2 text-left ${i === active ? 'bg-primary/10' : 'hover:bg-base-200/60'}`}
                  onMouseEnter={() => setActive(i)}
                  onClick={() => go(entry)}
                >
                  <FontAwesomeIcon icon={icons[entry.group]} className="w-4 text-base-content/50" />
                  <span className="min-w-0 flex-1">
                    <span className="block truncate font-medium">{entry.label}</span>
                    {entry.detail && <span className="block truncate text-[12px] text-base-content/55">{entry.detail}</span>}
                  </span>
                  {entry.shortcut && <Kbd>{entry.shortcut}</Kbd>}
                </button>
              </li>
            ))}
            {data?.usersUnavailable && term.length >= 2 && <li className="px-4 py-2 text-[12px] text-warning">{t('search.usersUnavailable')}</li>}
          </ul>
          <div className="flex gap-3 border-t border-base-300 px-4 py-2 text-[11px] text-base-content/55">
            <span>
              <Kbd>↑</Kbd> <Kbd>↓</Kbd> {t('search.move')}
            </span>
            <span>
              <Kbd>↵</Kbd> {t('search.openItem')}
            </span>
            <span>
              <Kbd>Esc</Kbd> {t('search.close')}
            </span>
          </div>
        </div>
        <form method="dialog" className="modal-backdrop">
          <button type="submit">{t('search.close')}</button>
        </form>
      </dialog>
    </>
  )
}
