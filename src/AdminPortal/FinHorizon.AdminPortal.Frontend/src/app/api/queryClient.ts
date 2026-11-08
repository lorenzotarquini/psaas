import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Only a network failure or a 503 (service temporarily down) may change by retrying; 4xx and 500 will not.
      retry: (failureCount, error) => !(error instanceof ApiError && error.status !== 503) && failureCount < 2,
    },
  },
})
