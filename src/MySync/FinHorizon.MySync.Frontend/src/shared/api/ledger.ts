import type { QueryClient } from '@tanstack/react-query'

/**
 * Every query that depends on the user's ledger starts with this key: balances, residuals and the trend
 * are computed from the movements, so any write refreshes them all at once.
 */
export const ledgerKey = 'ledger' as const

export const invalidateLedger = (queryClient: QueryClient) => queryClient.invalidateQueries({ queryKey: [ledgerKey] })
