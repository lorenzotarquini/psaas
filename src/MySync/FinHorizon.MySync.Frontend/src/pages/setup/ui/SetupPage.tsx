import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck } from '@fortawesome/free-solid-svg-icons'
import { useAccounts } from '@/entities/account'
import { CurrencySelect, usePreferences } from '@/entities/currency'
import { useDebts } from '@/entities/debt'
import { AccountForm } from '@/features/account-form'
import { DebtForm } from '@/features/debt-form'
import { useSaveDisplayCurrency } from '@/features/display-currency'
import { Amount, LoadError, Loading } from '@/shared/ui'

type Step = 'currency' | 'accounts' | 'debts'

/**
 * UC1: the guided setup of the first access. Every account and debt is saved as soon as it is created (R2),
 * so leaving half-way loses nothing; the setup is complete as soon as one account exists (R1).
 */
export function SetupPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const accounts = useAccounts()
  const debts = useDebts()
  const preferences = usePreferences()
  const saveCurrency = useSaveDisplayCurrency()
  // UC13: the first step is the currency net worth and trend are shown in.
  const [step, setStep] = useState<Step>('currency')
  const [currency, setCurrency] = useState<string>()
  const [needAccount, setNeedAccount] = useState(false)
  // UC1-1a: who already had accounts when the page opened goes straight to the dashboard.
  // Remembered once (React's "adjust state while rendering" pattern): creating the first account must not leave the page.
  const [startedEmpty, setStartedEmpty] = useState<boolean>()
  if (accounts.data && !accounts.isFetching && startedEmpty === undefined) setStartedEmpty(accounts.data.items.length === 0)

  if (accounts.isPending || debts.isPending || preferences.isPending || startedEmpty === undefined) return <Loading />
  if (accounts.isError || debts.isError || preferences.isError) return <LoadError />
  if (startedEmpty === false) return <Navigate to="/" replace />

  const accountList = accounts.data.items
  const debtList = debts.data.items
  const steps: Step[] = debts.data.max > 0 ? ['currency', 'accounts', 'debts'] : ['currency', 'accounts']
  const chosenCurrency = currency ?? preferences.data.displayCurrency

  function next() {
    if (step === 'currency') {
      saveCurrency.mutate(chosenCurrency, { onSuccess: () => setStep('accounts') })
      return
    }
    if (accountList.length === 0) {
      setNeedAccount(true)
      return
    }
    if (step === 'accounts' && steps.includes('debts')) setStep('debts')
    else void navigate('/', { replace: true })
  }

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-5">
      <header>
        <h1 className="text-2xl font-semibold">{t('setup.title')}</h1>
        <p className="muted">{t('setup.subtitle')}</p>
      </header>
      <ul className="steps w-full">
        {steps.map((item) => (
          <li key={item} className={`step ${steps.indexOf(item) <= steps.indexOf(step) ? 'step-primary' : ''}`} aria-current={item === step ? 'step' : undefined}>
            {t(`setup.steps.${item}`)}
          </li>
        ))}
      </ul>

      {step === 'currency' ? (
        <section className="surface flex flex-col gap-4 p-5" aria-labelledby="setup-currency">
          <h2 id="setup-currency" className="surface-title">
            {t('setup.currencyTitle')}
          </h2>
          <p className="muted">{t('setup.currencyHelp')}</p>
          <div className="flex flex-col gap-1">
            <label htmlFor="setup-currency-select" className="text-sm font-medium">
              {t('currency.display')}
            </label>
            <CurrencySelect
              id="setup-currency-select"
              type="Fiat"
              className="max-w-xs"
              value={chosenCurrency}
              onChange={(event) => setCurrency(event.target.value)}
            />
          </div>
          {saveCurrency.isError && (
            <p role="alert" className="alert alert-error alert-soft">
              {t('errors.saveFailed')}
            </p>
          )}
        </section>
      ) : step === 'accounts' ? (
        <section className="surface flex flex-col gap-4 p-5" aria-labelledby="setup-accounts">
          <h2 id="setup-accounts" className="surface-title">
            {t('setup.accountsTitle')}
          </h2>
          <p className="muted">{t('setup.accountsHelp', { count: accounts.data.max - accountList.length })}</p>
          {accountList.length > 0 && (
            <ul className="flex flex-col gap-1">
              {accountList.map((account) => (
                <li key={account.id} className="flex items-center gap-2">
                  <FontAwesomeIcon icon={faCheck} className="text-success" />
                  <span className="flex-1">{account.name}</span>
                  <Amount value={account.balance} currency={account.currency} highlightNegative />
                </li>
              ))}
            </ul>
          )}
          {needAccount && accountList.length === 0 && (
            <p role="alert" className="alert alert-warning alert-soft">
              {t('setup.needAccount')}
            </p>
          )}
          {accountList.length < accounts.data.max && <AccountForm onSaved={() => setNeedAccount(false)} />}
        </section>
      ) : (
        <section className="surface flex flex-col gap-4 p-5" aria-labelledby="setup-debts">
          <h2 id="setup-debts" className="surface-title">
            {t('setup.debtsTitle')}
          </h2>
          <p className="muted">{t('setup.debtsHelp')}</p>
          {debtList.length > 0 && (
            <ul className="flex flex-col gap-1">
              {debtList.map((debt) => (
                <li key={debt.id} className="flex items-center gap-2">
                  <FontAwesomeIcon icon={faCheck} className="text-success" />
                  <span className="flex-1">{debt.name}</span>
                  <Amount value={debt.residual} currency={debt.currency} />
                </li>
              ))}
            </ul>
          )}
          {debtList.length < debts.data.max && <DebtForm />}
        </section>
      )}

      <div className="flex justify-between gap-2">
        {step === 'currency' ? (
          <span />
        ) : (
          <button type="button" className="btn" onClick={() => setStep(steps[steps.indexOf(step) - 1] ?? 'currency')}>
            {t('actions.back')}
          </button>
        )}
        <button type="button" className="btn btn-primary" disabled={saveCurrency.isPending} onClick={next}>
          {step === steps[steps.length - 1] ? t('setup.finish') : t('actions.next')}
        </button>
      </div>
    </div>
  )
}
