import { faCircle } from '@fortawesome/free-regular-svg-icons'
import { faCircleCheck, faCircleInfo, faCircleXmark } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import type { Check } from '../model/onboarding'

const icons = { ok: faCircleCheck, error: faCircleXmark, pending: faCircle } as const
const colors = { ok: 'text-success', error: 'text-error', pending: 'text-base-content/30' } as const

// Right column of mock 1e: the rules of saving, checked as the administrator types.
export function ChecksPanel({ checks }: { checks: Check[] }) {
  const { t } = useTranslation()

  return (
    <aside className="flex h-full flex-col gap-4">
      <h2 className="panel-title">{t('onboarding.checks.title')}</h2>
      <ul className="flex flex-col gap-3.5" aria-live="polite">
        {checks.map((check) => (
          <li key={check.key} className="grid grid-cols-[18px_1fr] gap-2">
            <FontAwesomeIcon icon={icons[check.state]} className={`mt-0.5 ${colors[check.state]}`} />
            <div>
              <div className="font-semibold">{t(`onboarding.checks.${check.key}`)}</div>
              <div className="text-[12px] text-base-content/60">{t(check.detail.key, check.detail.values)}</div>
            </div>
          </li>
        ))}
      </ul>
      <div className="mt-auto flex gap-2 rounded-box bg-info/8 p-3 text-[12px] text-info">
        <FontAwesomeIcon icon={faCircleInfo} className="mt-0.5" />
        <p>{t('onboarding.draftInfo')}</p>
      </div>
    </aside>
  )
}
