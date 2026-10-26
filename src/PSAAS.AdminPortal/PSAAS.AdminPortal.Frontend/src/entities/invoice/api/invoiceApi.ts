import { keepPreviousData, useMutation, useQuery, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiGet, apiPost, type PagedResult } from '@/shared/api'

export type InvoiceStatus = 'Issued' | 'Overdue' | 'Paid' | 'Cancelled'
export type InvoiceKind = 'Invoice' | 'CreditNote'

// Mirrors PSAAS.Application/Contracts/Billing/BillingModels.cs (UC23, UC24).
export interface Invoice {
  id: string
  number: string
  kind: InvoiceKind
  tenantId: string
  tenantName: string
  tenantVatNumber: string
  tenantCertifiedEmail: string
  tenantSdiCode: string | null
  issuedAt: string
  dueAt: string
  periodFrom: string | null
  periodTo: string | null
  lines: { description: string; quantity: number; unitPrice: number; amount: number }[]
  net: number
  vatRate: number
  vat: number
  total: number
  status: InvoiceStatus
  paidAt: string | null
  paymentReference: string | null
  remindersSent: number
  lastReminderAt: string | null
  relatedInvoiceId: string | null
  relatedNumber: string | null
  /** Cancellation reason, or the note of a credit note. */
  cancelReason: string | null
}

export interface InvoicesQuery {
  tenantId?: string
  status?: InvoiceStatus
  search?: string
  page: number
  pageSize: number
}

export interface BillingSummary {
  toCollect: number
  overdue: number
  overdueCount: number
  paidThisMonth: number
  issuedThisMonth: number
}

export interface BillingRunResult {
  issued: number
  expired: number
  reminders: number
  suspended: number
}

export const useInvoices = (query: InvoicesQuery, enabled = true) =>
  useQuery({
    queryKey: ['invoices', 'list', query],
    queryFn: ({ signal }) => {
      const params = new URLSearchParams({ page: String(query.page), pageSize: String(query.pageSize) })
      if (query.tenantId) params.set('tenantId', query.tenantId)
      if (query.status) params.set('status', query.status)
      if (query.search) params.set('search', query.search)
      return apiGet<PagedResult<Invoice>>(`/api/invoices?${params}`, signal)
    },
    placeholderData: keepPreviousData,
    enabled,
  })

export const useInvoice = (id: string | null) =>
  useQuery({
    queryKey: ['invoices', 'detail', id],
    queryFn: ({ signal }) => apiGet<Invoice>(`/api/invoices/${encodeURIComponent(id!)}`, signal),
    enabled: Boolean(id),
  })

export const useBillingSummary = () =>
  useQuery({ queryKey: ['invoices', 'summary'], queryFn: ({ signal }) => apiGet<BillingSummary>('/api/invoices/summary', signal) })

// Billing moves invoices, tenant statuses (suspension) and subscriptions (renewals): refresh them all.
function onBillingChanged(queryClient: QueryClient) {
  for (const key of [['invoices'], ['tenants'], ['subscriptions'], ['audit']]) void queryClient.invalidateQueries({ queryKey: key })
}

export function useRegisterPayment(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: { paidAt: string; reference: string }) => apiPost<Invoice>(`/api/invoices/${encodeURIComponent(id)}/payments`, body),
    onSuccess: () => onBillingChanged(queryClient),
  })
}

export function useCancelInvoice(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (reason: string) => apiPost<Invoice>(`/api/invoices/${encodeURIComponent(id)}/cancel`, { reason }),
    onSuccess: () => onBillingChanged(queryClient),
  })
}

export function useRunBilling() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiPost<BillingRunResult>('/api/billing/run', undefined),
    onSuccess: () => onBillingChanged(queryClient),
  })
}
