import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import type { Debt } from '@/entities/debt'
import { CreateDebtForm } from '@/features/create-debt'
import { Amount, EmptyState, Modal } from '@/shared/ui'

/** UC4: debts by residual, paid-off ones last; "New debt" while the limit allows it (UC3). */
export function DebtsCard({ debts, max }: { debts: Debt[]; max: number }) {
  const { t } = useTranslation()
  const [creating, setCreating] = useState(false)

  // UC3-R5: with a limit of 0 debts are switched off.
  if (max === 0) return null

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="debts-title">
      <header className="flex items-center justify-between">
        <h2 id="debts-title" className="surface-title">
          {t('debts.title')} <span className="muted text-xs font-normal">{t('debts.count', { count: debts.length, max })}</span>
        </h2>
        {debts.length < max && (
          <button type="button" className="btn btn-ghost btn-sm text-primary" onClick={() => setCreating(true)}>
            <FontAwesomeIcon icon={faPlus} /> {t('debts.new')}
          </button>
        )}
      </header>
      {debts.length === 0 ? (
        <EmptyState>{t('debts.empty')}</EmptyState>
      ) : (
        <ul className="flex flex-col gap-3">
          {debts.map((debt) => {
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
      <Modal open={creating} title={t('debts.new')} onClose={() => setCreating(false)}>
        <CreateDebtForm onCreated={() => setCreating(false)} />
      </Modal>
    </section>
  )
}
