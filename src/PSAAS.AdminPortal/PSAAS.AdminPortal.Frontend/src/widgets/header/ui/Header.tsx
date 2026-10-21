import { faBars } from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '@/features/switch-language'
import { ThemeSwitcher } from '@/features/switch-theme'
import { ProfileMenu } from './ProfileMenu'

export function Header({ drawerId }: { drawerId: string }) {
  const { t } = useTranslation()

  return (
    <header className="navbar bg-base-100 shadow-sm gap-1">
      {/* Mobile only: on desktop the sidebar has its own toggle. */}
      <label htmlFor={drawerId} aria-label={t('menu')} className="btn btn-square btn-ghost lg:hidden">
        <FontAwesomeIcon icon={faBars} />
      </label>
      <div className="flex-1" />
      <LanguageSwitcher />
      <ThemeSwitcher />
      <ProfileMenu />
    </header>
  )
}
