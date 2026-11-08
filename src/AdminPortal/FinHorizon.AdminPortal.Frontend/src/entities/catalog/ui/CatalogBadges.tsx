import {
  faBoxesStacked,
  faCube,
  faFileInvoiceDollar,
  faIndustry,
  faPuzzlePiece,
  faTruck,
  faUserTie,
  faWrench,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { useTranslation } from 'react-i18next'
import { StatusBadge, type StatusTone } from '@/shared/ui/status-badge'
import type { ModuleStatus, ProvisioningStatus, SubscriptionStatus } from '../api/catalogApi'

const moduleTones: Record<ModuleStatus, StatusTone> = { Available: 'success', Beta: 'info', Deprecated: 'neutral' }
const provisioningTones: Record<ProvisioningStatus, StatusTone> = { Active: 'success', Provisioning: 'warning', Failed: 'error' }
const subscriptionTones: Record<SubscriptionStatus, StatusTone> = { Active: 'success', Trial: 'info', Expired: 'error', Cancelled: 'neutral' }

export function ModuleStatusBadge({ status }: { status: ModuleStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={moduleTones[status]}>{t(`catalog.moduleStatus.${status}`)}</StatusBadge>
}

export function ProvisioningBadge({ status }: { status: ProvisioningStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={provisioningTones[status]}>{t(`catalog.provisioning.${status}`)}</StatusBadge>
}

export function SubscriptionStatusBadge({ status }: { status: SubscriptionStatus }) {
  const { t } = useTranslation()
  return <StatusBadge tone={subscriptionTones[status]}>{t(`catalog.subscriptionStatus.${status}`)}</StatusBadge>
}

// Manifest icons are names (UC15); unknown ones fall back to the puzzle piece of the catalog.
const icons: Record<string, IconDefinition> = {
  cube: faCube,
  warehouse: faBoxesStacked,
  boxes: faBoxesStacked,
  hr: faUserTie,
  user: faUserTie,
  industry: faIndustry,
  production: faIndustry,
  maintenance: faWrench,
  wrench: faWrench,
  logistics: faTruck,
  truck: faTruck,
  invoice: faFileInvoiceDollar,
}

export function ModuleIcon({ icon }: { icon: string | null }) {
  return (
    <span className="grid size-7 shrink-0 place-items-center rounded-md bg-base-200 text-[12px] text-base-content/70">
      <FontAwesomeIcon icon={(icon && icons[icon]) || faPuzzlePiece} />
    </span>
  )
}
