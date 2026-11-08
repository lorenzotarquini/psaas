import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '@/features/switch-language'
import { ThemeSwitcher } from '@/features/switch-theme'
import { PageHeader } from '@/shared/ui/page-header'
import { ServicesStatus } from '@/widgets/services-status'

// Preferences of this administrator, kept in the browser (language and theme), and the connected systems (UC26).
export function SettingsPage() {
  const { t } = useTranslation()

  return (
    <>
      <PageHeader title={t('profile.settings')} subtitle={t('settings.subtitle')} />
      <section className="panel max-w-xl">
        <h2 className="panel-title border-b border-base-300 px-4 py-3">{t('settings.preferences')}</h2>
        <dl className="divide-y divide-base-300">
          <div className="flex items-center justify-between px-4 py-2.5">
            <dt>
              <div className="font-medium">{t('language')}</div>
              <div className="text-[12px] text-base-content/60">{t('settings.languageHint')}</div>
            </dt>
            <dd>
              <LanguageSwitcher />
            </dd>
          </div>
          <div className="flex items-center justify-between px-4 py-2.5">
            <dt>
              <div className="font-medium">{t('theme')}</div>
              <div className="text-[12px] text-base-content/60">{t('settings.themeHint')}</div>
            </dt>
            <dd>
              <ThemeSwitcher />
            </dd>
          </div>
        </dl>
      </section>
      <div className="max-w-xl">
        <ServicesStatus />
      </div>
    </>
  )
}
