import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/shared/api'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Client errors (400, 403, 404…) will not change by retrying; network and 5xx may.
      retry: (failureCount, error) => !(error instanceof ApiError && error.status < 500) && failureCount < 2,
    },
  },
})
