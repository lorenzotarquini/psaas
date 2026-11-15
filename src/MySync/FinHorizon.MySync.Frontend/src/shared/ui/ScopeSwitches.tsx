import { useTranslation } from 'react-i18next'
import type { PortfolioScope } from '@/shared/lib/portfolio-scope'

/** UC12: one switch per optional group of the portfolio. */
export function ScopeSwitches({ scope, onChange }: { scope: PortfolioScope; onChange: (scope: PortfolioScope) => void }) {
  const { t } = useTranslation()
  return (
    <>
      <label className="label cursor-pointer gap-2 text-sm">
        <input
          type="checkbox"
          className="toggle toggle-primary toggle-sm"
          checked={scope.stable}
          onChange={(event) => onChange({ ...scope, stable: event.target.checked })}
        />
        {t('scope.stable')}
      </label>
      <label className="label cursor-pointer gap-2 text-sm">
        <input
          type="checkbox"
          className="toggle toggle-primary toggle-sm"
          checked={scope.investments}
          onChange={(event) => onChange({ ...scope, investments: event.target.checked })}
        />
        {t('scope.investments')}
      </label>
    </>
  )
}
