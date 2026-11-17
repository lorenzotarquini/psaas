import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { categoriesQueryKey, setCategoryFixed, useCategories } from '@/entities/category'
import { invalidateLedger } from '@/shared/api'
import { EmptyState, LoadError, Loading } from '@/shared/ui'

/** UC15: the expense categories, each with its "fixed" switch; a change is saved at once and refreshes the suggestions. */
export function FixedExpenses() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const categories = useCategories('Expense')
  const mutation = useMutation({
    mutationFn: ({ id, fixed }: { id: string; fixed: boolean }) => setCategoryFixed(id, fixed),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: categoriesQueryKey })
      await invalidateLedger(queryClient)
    },
  })

  if (categories.isPending) return <Loading />
  if (categories.isError) return <LoadError />
  if (categories.data.length === 0) return <EmptyState>{t('projection.fixed.none')}</EmptyState>

  return (
    <div className="flex flex-col gap-2">
      <p className="muted text-sm">{t('projection.fixed.hint')}</p>
      <ul className="flex flex-col gap-1">
        {categories.data.map((category) => (
          <li key={category.id} className="flex items-center justify-between gap-3">
            <span>{category.name}</span>
            <label className="label cursor-pointer gap-2 text-sm">
              {t('projection.fixed.label')}
              <input
                type="checkbox"
                className="toggle toggle-sm toggle-primary"
                aria-label={`${t('projection.fixed.label')}: ${category.name}`}
                checked={category.fixed ?? false}
                disabled={mutation.isPending}
                onChange={(event) => mutation.mutate({ id: category.id, fixed: event.target.checked })}
              />
            </label>
          </li>
        ))}
      </ul>
    </div>
  )
}
