import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { Debt } from '@/entities/debt'
import { DebtForm } from '@/features/debt-form'
import { inScope, type PortfolioScope } from '@/shared/lib/portfolio-scope'
import { AddButton, Amount, EmptyState, Modal } from '@/shared/ui'

/** UC4: debts by residual, paid-off ones last; "New debt" while the limit allows it (UC3). */
interface DebtsCardProps {
  debts: Debt[]
  max: number
  /** UC12: the groups to show; the count and the limit still include the hidden ones. */
  scope?: PortfolioScope
}

export function DebtsCard({ debts, max, scope = { stable: true, investments: true } }: DebtsCardProps) {
  const { t } = useTranslation()
  const [creating, setCreating] = useState(false)
  const shown = debts.filter((debt) => inScope(debt.group, scope))

  // UC3-R5: with a limit of 0 debts are switched off.
  if (max === 0) return null

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="debts-title">
      <header className="flex items-center justify-between">
        <h2 id="debts-title" className="surface-title">
          {t('debts.title')} <span className="muted text-xs font-normal">{t('debts.count', { count: debts.length, max })}</span>
        </h2>
        <AddButton label={t('debts.new')} limitReached={debts.length >= max} limitMessage={t('debts.limitReached')} onClick={() => setCreating(true)} />
      </header>
      {shown.length === 0 ? (
        <EmptyState>{t('debts.empty')}</EmptyState>
      ) : (
        <ul className="flex flex-col gap-3">
          {shown.map((debt) => {
            const paid = debt.initialResidual - debt.residual
            return (
              <li key={debt.id} className={debt.paidOff ? 'opacity-60' : ''}>
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium">
                    {debt.name} <span className="muted text-xs">· {t(`debts.kinds.${debt.kind}`)}</span>
                  </span>
                  {debt.paidOff ? (
                    <span className="badge badge-success badge-soft badge-sm">{t('debts.paidOff')}</span>
                  ) : (
                    <Amount value={debt.residual} className="font-semibold" />
                  )}
                </div>
                <progress
                  className="progress progress-primary h-2 w-full"
                  value={paid}
                  max={debt.initialResidual}
                  aria-label={t('debts.progress', { name: debt.name })}
                />
              </li>
            )
          })}
        </ul>
      )}
      {shown.length < debts.length && <p className="muted text-xs">{t('debts.stableHidden', { count: debts.length - shown.length })}</p>}
      <Modal open={creating} title={t('debts.new')} onClose={() => setCreating(false)}>
        <DebtForm onSaved={() => setCreating(false)} />
      </Modal>
    </section>
  )
}
