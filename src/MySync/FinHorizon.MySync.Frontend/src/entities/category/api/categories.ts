import { useQuery } from '@tanstack/react-query'
import { apiGet, apiPost, toQuery } from '@/shared/api'
import type { TransactionKind } from '@/entities/transaction/@x/category'

// Mirrors CategoryView in FinHorizon.MySync.Application.
export interface Category {
  id: string
  kind: TransactionKind
  name: string
}

export const categoriesQueryKey = ['categories'] as const

export const useCategories = (kind: TransactionKind | undefined, term = '') =>
  useQuery({
    queryKey: [...categoriesQueryKey, kind ?? 'all', term],
    queryFn: ({ signal }) => apiGet<Category[]>(`/api/categories${toQuery({ kind, term })}`, signal),
  })

/** UC6-R4: idempotent, an existing category with the same name is returned as it is. */
export const createCategory = (kind: TransactionKind, name: string) => apiPost<Category>('/api/categories', { kind, name })
