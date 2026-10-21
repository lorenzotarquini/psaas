import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '@/features/switch-language'
import { ThemeSwitcher } from '@/features/switch-theme'
import { keycloak, logout } from '@/shared/auth'

export function Header() {
  const { t } = useTranslation()

  return (
    <header className="navbar bg-base-100 shadow-sm gap-2">
      <span className="flex-1 text-xl font-semibold px-2">{t('appName')}</span>
      <LanguageSwitcher />
      <ThemeSwitcher />
      <span className="text-sm">{keycloak.tokenParsed?.preferred_username}</span>
      <button type="button" className="btn btn-sm btn-ghost" onClick={() => void logout()}>
        {t('logout')}
      </button>
    </header>
  )
}
