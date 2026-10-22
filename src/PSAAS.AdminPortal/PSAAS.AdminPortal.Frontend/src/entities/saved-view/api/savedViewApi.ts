import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiDelete, apiGet, apiPost } from '@/shared/api'

export type SavedViewArea = 'tenants' | 'users'

/** UC22: query is the list parameters as in the page URL. */
export interface SavedView {
  id: string
  area: SavedViewArea
  name: string
  query: string
  createdAt: string
}

const key = (area: SavedViewArea) => ['saved-views', area] as const

export const useSavedViews = (area: SavedViewArea) =>
  useQuery({ queryKey: key(area), queryFn: ({ signal }) => apiGet<SavedView[]>(`/api/saved-views?area=${area}`, signal) })

export function useSaveView(area: SavedViewArea) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ name, query }: { name: string; query: string }) => apiPost<SavedView>('/api/saved-views', { area, name, query }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(area) }),
  })
}

export function useDeleteView(area: SavedViewArea) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiDelete(`/api/saved-views/${encodeURIComponent(id)}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(area) }),
  })
}
