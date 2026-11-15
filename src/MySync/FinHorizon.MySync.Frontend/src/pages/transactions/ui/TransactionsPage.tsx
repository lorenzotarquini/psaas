import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { useAccounts } from '@/entities/account'
import { useCategories } from '@/entities/category'
import { useDebts } from '@/entities/debt'
import { transactionKinds, useTransactions, type TransactionFilter, type TransactionKind } from '@/entities/transaction'
import { ApiError } from '@/shared/api'
import { Amount, EmptyState, LoadError, Loading } from '@/shared/ui'
import { TransactionTable } from '@/widgets/transaction-table'

const pageSize = 25

/** UC8 "Consultare": every movement, filtered and paged; filters and page live in the URL (R5). */
export function TransactionsPage() {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const filter: TransactionFilter = {
    from: params.get('from') ?? undefined,
    to: params.get('to') ?? undefined,
    kind: (params.get('kind') as TransactionKind | null) ?? undefined,
    accountId: params.get('accountId') ?? undefined,
    categoryId: params.get('categoryId') ?? undefined,
    debtId: params.get('debtId') ?? undefined,
    page: Number(params.get('page') ?? 1),
    pageSize,
  }
  const transactions = useTransactions(filter)
  const accounts = useAccounts()
  const debts = useDebts()
  // A transfer has no category (UC9): the category filter is for expenses and incomes only.
  const categoryKind = filter.kind === 'Transfer' ? undefined : filter.kind
  const categories = useCategories(categoryKind)
  const page = transactions.data
  const pages = page ? Math.max(1, Math.ceil(page.totalCount / pageSize)) : 1
  const invalidPeriod = transactions.error instanceof ApiError && transactions.error.fieldErrors.period !== undefined
  const filtered = [...params.keys()].some((key) => key !== 'page')

  function set(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    // A new filter starts again from the first page; changing kind resets the category (categories are per kind).
    if (key !== 'page') next.delete('page')
    if (key === 'kind') next.delete('categoryId')
    setParams(next)
  }

  return (
    <div className="flex flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold">{t('transactions.title')}</h1>
        <p className="muted">{t('transactions.subtitle')}</p>
      </header>

      <section className="surface grid gap-3 p-5 sm:grid-cols-2 lg:grid-cols-6" aria-label={t('transactions.filters')}>
        <label className="flex flex-col gap-1 text-sm">
          {t('transactions.from')}
          <input type="date" className="input input-sm w-full" value={filter.from ?? ''} onChange={(event) => set('from', event.target.value)} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          {t('transactions.to')}
          <input type="date" className="input input-sm w-full" value={filter.to ?? ''} onChange={(event) => set('to', event.target.value)} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          {t('transactions.fields.kind')}
          <select className="select select-sm w-full" value={filter.kind ?? ''} onChange={(event) => set('kind', event.target.value)}>
            <option value="">{t('transactions.allKinds')}</option>
            {transactionKinds.map((kind) => (
              <option key={kind} value={kind}>
                {t(`transactions.kinds.${kind}`)}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm">
          {t('transactions.fields.account')}
          <select className="select select-sm w-full" value={filter.accountId ?? ''} onChange={(event) => set('accountId', event.target.value)}>
            <option value="">{t('transactions.allAccounts')}</option>
            {accounts.data?.items.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name}
              </option>
            ))}
          </select>
        </label>
        <label className={`flex flex-col gap-1 text-sm ${filter.kind === 'Transfer' ? 'invisible' : ''}`}>
          {t('transactions.fields.category')}
          <select className="select select-sm w-full" value={filter.categoryId ?? ''} onChange={(event) => set('categoryId', event.target.value)}>
            <option value="">{t('transactions.allCategories')}</option>
            {categories.data?.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm">
          {t('transactions.fields.debt')}
          <select className="select select-sm w-full" value={filter.debtId ?? ''} onChange={(event) => set('debtId', event.target.value)}>
            <option value="">{t('transactions.allDebts')}</option>
            {debts.data?.items.map((debt) => (
              <option key={debt.id} value={debt.id}>
                {debt.name}
              </option>
            ))}
          </select>
        </label>
      </section>

      <section className="surface flex flex-col gap-3 p-5" aria-labelledby="list-title">
        <header className="flex flex-wrap items-center justify-between gap-2">
          <h2 id="list-title" className="surface-title">
            {t('transactions.count', { count: page?.totalCount ?? 0 })}
          </h2>
          {page && (
            <div className="flex gap-4 text-sm">
              <span>
                {t('transactions.expenseTotal')}: <Amount value={-page.expenseTotal} signed />
              </span>
              <span>
                {t('transactions.incomeTotal')}: <Amount value={page.incomeTotal} signed />
              </span>
            </div>
          )}
        </header>
        {invalidPeriod ? (
          <p role="alert" className="alert alert-warning alert-soft">
            {t('transactions.invalidPeriod')}
          </p>
        ) : transactions.isPending ? (
          <Loading />
        ) : transactions.isError ? (
          <LoadError />
        ) : page && page.items.length === 0 ? (
          <EmptyState>
            {filtered ? t('transactions.noMatch') : t('dashboard.noTransactions')}{' '}
            {filtered && (
              <button type="button" className="link link-primary" onClick={() => setParams(new URLSearchParams())}>
                {t('transactions.clearFilters')}
              </button>
            )}
          </EmptyState>
        ) : (
          page && <TransactionTable items={page.items} />
        )}
        {pages > 1 && (
          <nav className="join self-center" aria-label={t('transactions.pages')}>
            <button type="button" className="btn join-item btn-sm" disabled={filter.page === 1} onClick={() => set('page', String((filter.page ?? 1) - 1))}>
              «
            </button>
            <span className="btn join-item btn-sm pointer-events-none">{t('transactions.pageOf', { page: filter.page, pages })}</span>
            <button type="button" className="btn join-item btn-sm" disabled={filter.page === pages} onClick={() => set('page', String((filter.page ?? 1) + 1))}>
              »
            </button>
          </nav>
        )}
      </section>
    </div>
  )
}
