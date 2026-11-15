import { useDeferredValue, useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { categoriesQueryKey, createCategory, useCategories, type Category } from '../api/categories'
import type { TransactionKind } from '@/entities/transaction/@x/category'

interface CategoryPickerProps {
  id: string
  kind: TransactionKind
  value: Category | null
  onChange: (category: Category | null) => void
  invalid?: boolean
  describedBy?: string
}

/**
 * UC6: a combobox over the user's categories of the movement's kind. When nothing matches it offers
 * "Create «text»", creates the category on the spot and selects it; the rest of the form stays as it is.
 */
export function CategoryPicker({ id, kind, value, onChange, invalid, describedBy }: CategoryPickerProps) {
  const { t } = useTranslation()
  const listId = useId()
  const queryClient = useQueryClient()
  const [text, setText] = useState(value?.name ?? '')
  const [open, setOpen] = useState(false)
  const term = useDeferredValue(text.trim())
  const suggestions = useCategories(kind, term)
  const exact = suggestions.data?.some((category) => category.name.toLocaleLowerCase() === term.toLocaleLowerCase())

  const create = useMutation({
    mutationFn: () => createCategory(kind, term),
    onSuccess: async (category) => {
      choose(category)
      await queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
    },
  })

  function choose(category: Category) {
    onChange(category)
    setText(category.name)
    setOpen(false)
  }

  return (
    <div className="relative">
      <input
        id={id}
        role="combobox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-invalid={invalid}
        aria-describedby={describedBy}
        className="input w-full"
        autoComplete="off"
        value={text}
        placeholder={t('categories.placeholder')}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 150)}
        onChange={(event) => {
          setText(event.target.value)
          setOpen(true)
          if (value && event.target.value !== value.name) onChange(null)
        }}
      />
      {open && (
        <ul id={listId} role="listbox" className="menu absolute z-10 mt-1 max-h-60 w-full flex-nowrap overflow-y-auto rounded-box bg-base-100 p-1 shadow-lg">
          {suggestions.data?.map((category) => (
            <li key={category.id} role="option" aria-selected={value?.id === category.id}>
              <button type="button" onMouseDown={(event) => event.preventDefault()} onClick={() => choose(category)}>
                {category.name}
              </button>
            </li>
          ))}
          {term && !exact && (
            <li role="option" aria-selected={false}>
              <button
                type="button"
                className="text-primary"
                disabled={create.isPending}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => create.mutate()}
              >
                <FontAwesomeIcon icon={faPlus} />
                {t('categories.create', { name: term })}
              </button>
            </li>
          )}
          {!term && suggestions.data?.length === 0 && <li className="muted px-3 py-2">{t('categories.none')}</li>}
        </ul>
      )}
      {create.isError && (
        <p role="alert" className="label text-error">
          {t('errors.saveFailed')}
        </p>
      )}
    </div>
  )
}
