import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '@/features/switch-language'
import { ThemeSwitcher } from '@/features/switch-theme'

// The same switchers as the header, for whoever looks for them under "Settings".
export function SettingsPage() {
  const { t } = useTranslation()

  return (
    <div className="card bg-base-100 shadow-sm max-w-xl">
      <div className="card-body">
        <h1 className="card-title">{t('profile.settings')}</h1>
        <dl className="divide-y divide-base-200">
          <div className="flex items-center justify-between py-2">
            <dt className="font-medium">{t('language')}</dt>
            <dd>
              <LanguageSwitcher />
            </dd>
          </div>
          <div className="flex items-center justify-between py-2">
            <dt className="font-medium">{t('theme')}</dt>
            <dd>
              <ThemeSwitcher />
            </dd>
          </div>
        </dl>
      </div>
    </div>
  )
}
