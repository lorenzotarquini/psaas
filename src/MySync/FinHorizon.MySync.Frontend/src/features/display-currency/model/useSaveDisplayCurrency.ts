import { useMutation, useQueryClient } from '@tanstack/react-query'
import { preferencesQueryKey, updatePreferences } from '@/entities/currency'
import { invalidateLedger } from '@/shared/api'

/** Saves the display currency and recomputes everything shown in it (UC13). */
export function useSaveDisplayCurrency() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (displayCurrency: string) => updatePreferences({ displayCurrency }),
    onSuccess: async (saved) => {
      queryClient.setQueryData(preferencesQueryKey, saved)
      await invalidateLedger(queryClient)
    },
  })
}
