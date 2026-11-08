import type { TFunction } from 'i18next'
import type { SearchResult } from '@/entities/overview'
import { routes, tenantPath } from '@/shared/routes'

export interface PaletteEntry {
  group: 'tenants' | 'users' | 'actions'
  label: string
  detail?: string
  to: string
  shortcut?: string
}

/** Typing in a field: single-letter shortcuts must not fire (UC30-R2). */
export const isTyping = (target: EventTarget | null) =>
  target instanceof HTMLElement && (target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName))

/** Results first (tenants, then users), then the quick actions whose name matches the text. */
export function paletteEntries(result: SearchResult | undefined, text: string, t: TFunction): PaletteEntry[] {
  const needle = text.trim().toLowerCase()
  const actions = [
    { group: 'actions' as const, label: t('tenantForm.createTitle'), to: routes.tenantNew, shortcut: 'N' },
    { group: 'actions' as const, label: t('users.create'), to: routes.userNew },
    { group: 'actions' as const, label: t('nav.tenants'), to: routes.tenants },
    { group: 'actions' as const, label: t('nav.users'), to: routes.users },
    { group: 'actions' as const, label: t('nav.billing'), to: routes.billing },
    { group: 'actions' as const, label: t('nav.communications'), to: routes.communications },
    { group: 'actions' as const, label: t('nav.audit'), to: routes.audit },
  ] satisfies PaletteEntry[]
  const matching = actions.filter((a) => !needle || a.label.toLowerCase().includes(needle))
  return [
    ...(result?.tenants ?? []).map((tenant) => ({
      group: 'tenants' as const,
      label: tenant.companyName,
      detail: `${tenant.completeVatNumber} · ${tenant.certifiedEmail.toLowerCase()}`,
      to: tenantPath(tenant.id),
    })),
    ...(result?.users ?? []).map((user) => ({
      group: 'users' as const,
      label: `${user.firstName} ${user.lastName}`,
      detail: user.email,
      to: `${routes.users}?${new URLSearchParams({ search: user.email, user: user.id })}`,
    })),
    ...matching,
  ]
}
