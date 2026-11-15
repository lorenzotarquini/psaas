import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPenToSquare } from '@fortawesome/free-regular-svg-icons'
import { AccountKindIcon, useAccounts, type Account } from '@/entities/account'
import { useDebts, type Debt } from '@/entities/debt'
import { AccountForm } from '@/features/account-form'
import { DebtForm } from '@/features/debt-form'
import { useFormat } from '@/shared/lib/format'
import { AddButton, Amount, EmptyState, LoadError, Loading, Modal } from '@/shared/ui'

/** What is open: a new account or debt, or the one being edited. */
type Editing = { type: 'account'; account?: Account } | { type: 'debt'; debt?: Debt } | null

/** UC10: every account and debt of the user, to add and to edit. */
export function PortfolioPage() {
  const { t } = useTranslation()
  const format = useFormat()
  const accounts = useAccounts()
  const debts = useDebts()
  const [editing, setEditing] = useState<Editing>(null)

  if (accounts.isPending || debts.isPending) return <Loading />
  if (accounts.isError || debts.isError) return <LoadError />

  const accountName = (id: string) => accounts.data.items.find((account) => account.id === id)?.name ?? ''
  const close = () => setEditing(null)

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold">{t('portfolio.title')}</h1>
        <p className="muted">{t('portfolio.subtitle')}</p>
      </header>

      <section className="surface flex flex-col gap-3 p-5" aria-labelledby="portfolio-accounts">
        <header className="flex items-center justify-between">
          <h2 id="portfolio-accounts" className="surface-title">
            {t('accounts.title')} <span className="muted text-xs font-normal">{t('accounts.count', { count: accounts.data.items.length, max: accounts.data.max })}</span>
          </h2>
          <AddButton
            label={t('accounts.new')}
            limitReached={accounts.data.items.length >= accounts.data.max}
            limitMessage={t('accounts.limitReached')}
            onClick={() => setEditing({ type: 'account' })}
          />
        </header>
        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>{t('accounts.fields.name')}</th>
                <th className="hidden md:table-cell">{t('portfolio.openedOn')}</th>
                <th className="text-right">{t('accounts.fields.initialBalance')}</th>
                <th className="text-right">{t('portfolio.balance')}</th>
                <th>
                  <span className="sr-only">{t('actions.title')}</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {accounts.data.items.map((account) => (
                <tr key={account.id}>
                  <td>
                    <div className="flex items-center gap-3">
                      <AccountKindIcon kind={account.kind} />
                      <div className="min-w-0">
                        <div className="truncate font-medium">{account.name}</div>
                        <div className="muted truncate text-xs">
                          {t(`accounts.kinds.${account.kind}`)}
                          {account.tag && ` · ${account.tag}`}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td className="hidden whitespace-nowrap md:table-cell">{format.date(account.openedOn)}</td>
                  <td className="text-right">
                    <Amount value={account.initialBalance} currency={account.currency} highlightNegative />
                  </td>
                  <td className="text-right font-semibold">
                    <Amount value={account.balance} currency={account.currency} highlightNegative />
                  </td>
                  <td className="text-right">
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm btn-square"
                      aria-label={t('portfolio.editAccount', { name: account.name })}
                      onClick={() => setEditing({ type: 'account', account })}
                    >
                      <FontAwesomeIcon icon={faPenToSquare} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* UC3-R5: with a limit of 0 debts are switched off. */}
      {debts.data.max > 0 && (
        <section className="surface flex flex-col gap-3 p-5" aria-labelledby="portfolio-debts">
          <header className="flex items-center justify-between">
            <h2 id="portfolio-debts" className="surface-title">
              {t('debts.title')} <span className="muted text-xs font-normal">{t('debts.count', { count: debts.data.items.length, max: debts.data.max })}</span>
            </h2>
            <AddButton
              label={t('debts.new')}
              limitReached={debts.data.items.length >= debts.data.max}
              limitMessage={t('debts.limitReached')}
              onClick={() => setEditing({ type: 'debt' })}
            />
          </header>
          {debts.data.items.length === 0 ? (
            <EmptyState>{t('debts.empty')}</EmptyState>
          ) : (
            <div className="overflow-x-auto">
              <table className="table">
                <thead>
                  <tr>
                    <th>{t('debts.fields.name')}</th>
                    <th className="hidden md:table-cell">{t('debts.fields.account')}</th>
                    <th className="text-right">{t('debts.fields.initialResidual')}</th>
                    <th className="text-right">{t('portfolio.residual')}</th>
                    <th>
                      <span className="sr-only">{t('actions.title')}</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {debts.data.items.map((debt) => (
                    <tr key={debt.id}>
                      <td>
                        <div className="font-medium">{debt.name}</div>
                        <div className="muted text-xs">
                          {t(`debts.kinds.${debt.kind}`)}
                          {debt.tag && ` · ${debt.tag}`}
                        </div>
                      </td>
                      <td className="hidden md:table-cell">{accountName(debt.accountId)}</td>
                      <td className="text-right">
                        <Amount value={debt.initialResidual} currency={debt.currency} />
                      </td>
                      <td className="text-right font-semibold">
                        {debt.paidOff ? <span className="badge badge-success badge-soft badge-sm">{t('debts.paidOff')}</span> : <Amount value={debt.residual} currency={debt.currency} />}
                      </td>
                      <td className="text-right">
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm btn-square"
                          aria-label={t('portfolio.editDebt', { name: debt.name })}
                          onClick={() => setEditing({ type: 'debt', debt })}
                        >
                          <FontAwesomeIcon icon={faPenToSquare} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      <Modal open={editing?.type === 'account'} title={t(editing?.type === 'account' && editing.account ? 'portfolio.editAccountTitle' : 'accounts.new')} onClose={close}>
        {editing?.type === 'account' && <AccountForm key={editing.account?.id ?? 'new'} account={editing.account} onSaved={close} />}
      </Modal>
      <Modal open={editing?.type === 'debt'} title={t(editing?.type === 'debt' && editing.debt ? 'portfolio.editDebtTitle' : 'debts.new')} onClose={close}>
        {editing?.type === 'debt' && <DebtForm key={editing.debt?.id ?? 'new'} debt={editing.debt} onSaved={close} />}
      </Modal>
    </div>
  )
}
