import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faRightLeft } from '@fortawesome/free-solid-svg-icons'
import { AccountKindIcon, type Account } from '@/entities/account'
import { AccountForm } from '@/features/account-form'
import { TransferDialog } from '@/features/transfer-money'
import { inScope, type PortfolioScope } from '@/shared/lib/portfolio-scope'
import { AddButton, Amount, Modal } from '@/shared/ui'

/** UC4: the accounts with today's balance; "New account" while the limit allows it (UC2). */
interface AccountsCardProps {
  accounts: Account[]
  max: number
  /** UC12: the groups to show; the count and the limit still include the hidden ones. */
  scope?: PortfolioScope
}

export function AccountsCard({ accounts, max, scope = { stable: true, investments: true } }: AccountsCardProps) {
  const { t } = useTranslation()
  const [creating, setCreating] = useState(false)
  const [transferring, setTransferring] = useState(false)
  const shown = accounts.filter((account) => inScope(account.group, scope))
  const hidden = (group: Account['group']) => accounts.filter((account) => account.group === group && !inScope(group, scope)).length

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="accounts-title">
      <header className="flex items-center justify-between">
        <h2 id="accounts-title" className="surface-title">
          {t('accounts.title')} <span className="muted text-xs font-normal">{t('accounts.count', { count: accounts.length, max })}</span>
        </h2>
        <div className="flex items-center gap-1">
          <AddButton label={t('accounts.new')} limitReached={accounts.length >= max} limitMessage={t('accounts.limitReached')} onClick={() => setCreating(true)} />
          {/* UC9: moving money needs two accounts. */}
          {accounts.length >= 2 && (
            <span className="tooltip tooltip-left" data-tip={t('transfer.title')}>
              <button type="button" className="btn btn-ghost btn-sm btn-circle text-primary" aria-label={t('transfer.title')} onClick={() => setTransferring(true)}>
                <FontAwesomeIcon icon={faRightLeft} />
              </button>
            </span>
          )}
        </div>
      </header>
      <ul className="flex flex-col divide-y divide-base-300">
        {shown.map((account) => (
          <li key={account.id} className="flex items-center gap-3 py-2.5">
            <AccountKindIcon kind={account.kind} />
            <div className="min-w-0 flex-1">
              <div className="truncate font-medium">{account.name}</div>
              <div className="muted truncate text-xs">
                {t(`accounts.kinds.${account.kind}`)}
                {account.tag && ` · ${account.tag}`}
              </div>
            </div>
            <Amount value={account.balance} highlightNegative className="font-semibold" />
          </li>
        ))}
      </ul>
      {hidden('Stable') > 0 && <p className="muted text-xs">{t('accounts.stableHidden', { count: hidden('Stable') })}</p>}
      {hidden('Investments') > 0 && <p className="muted text-xs">{t('accounts.investmentsHidden', { count: hidden('Investments') })}</p>}
      <TransferDialog open={transferring} onClose={() => setTransferring(false)} />
      <Modal open={creating} title={t('accounts.new')} onClose={() => setCreating(false)}>
        <AccountForm onSaved={() => setCreating(false)} />
      </Modal>
    </section>
  )
}
