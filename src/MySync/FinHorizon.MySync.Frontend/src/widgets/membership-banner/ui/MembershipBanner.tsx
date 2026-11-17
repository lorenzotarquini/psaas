import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useMembership } from '@/entities/membership'
import { membershipExpiredEvent } from '@/shared/api'

/**
 * UC17-R4 on every page: read-only while expired, a warning while Stripe retries. A refused change (402)
 * refreshes it, so a subscription that expired meanwhile shows up at once.
 */
export function MembershipBanner() {
  const { t } = useTranslation()
  const membership = useMembership()
  const { refetch } = membership

  useEffect(() => {
    const onExpired = () => void refetch()
    window.addEventListener(membershipExpiredEvent, onExpired)
    return () => window.removeEventListener(membershipExpiredEvent, onExpired)
  }, [refetch])

  const status = membership.data?.status
  if (status !== 'Expired' && status !== 'PastDue') return null

  return (
    <div role="status" className={`alert alert-soft mx-4 mt-4 md:mx-6 ${status === 'Expired' ? 'alert-error' : 'alert-warning'}`}>
      <span>{t(status === 'Expired' ? 'membership.bannerExpired' : 'membership.bannerPastDue')}</span>
      <Link to="/settings" className="btn btn-sm">
        {t(status === 'Expired' ? 'membership.renew' : 'membership.fix')}
      </Link>
    </div>
  )
}
