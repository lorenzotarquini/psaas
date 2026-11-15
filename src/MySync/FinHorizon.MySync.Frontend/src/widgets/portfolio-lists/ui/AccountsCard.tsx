import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlus } from '@fortawesome/free-solid-svg-icons'
import { AccountKindIcon, type Account } from '@/entities/account'
import { CreateAccountForm } from '@/features/create-account'
import { Amount, Modal } from '@/shared/ui'

/** UC4: the accounts with today's balance; "New account" while the limit allows it (UC2). */
export function AccountsCard({ accounts, max }: { accounts: Account[]; max: number }) {
  const { t } = useTranslation()
  const [creating, setCreating] = useState(false)

  return (
    <section className="surface flex flex-col gap-3 p-5" aria-labelledby="accounts-title">
      <header className="flex items-center justify-between">
        <h2 id="accounts-title" className="surface-title">
          {t('accounts.title')} <span className="muted text-xs font-normal">{t('accounts.count', { count: accounts.length, max })}</span>
        </h2>
        {accounts.length < max && (
          <button type="button" className="btn btn-ghost btn-sm text-primary" onClick={() => setCreating(true)}>
            <FontAwesomeIcon icon={faPlus} /> {t('accounts.new')}
          </button>
        )}
      </header>
      <ul className="flex flex-col divide-y divide-base-300">
        {accounts.map((account) => (
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
      <Modal open={creating} title={t('accounts.new')} onClose={() => setCreating(false)}>
        <CreateAccountForm onCreated={() => setCreating(false)} />
      </Modal>
    </section>
  )
}
