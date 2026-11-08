import { createBrowserRouter } from 'react-router'
import { AuditPage } from '@/pages/audit'
import { CommunicationsPage } from '@/pages/communications'
import { BillingPage } from '@/pages/billing'
import { HomePage } from '@/pages/home'
import { ModulesPage } from '@/pages/modules'
import { NotFoundPage } from '@/pages/not-found'
import { PlansPage } from '@/pages/plans'
import { ProfilesPage } from '@/pages/profiles'
import { SettingsPage } from '@/pages/settings'
import { SubscriptionsPage } from '@/pages/subscriptions'
import { TenantDetailPage } from '@/pages/tenant-detail'
import { OnboardingPage } from '@/pages/tenant-onboarding'
import { TenantsListPage } from '@/pages/tenants-list'
import { UserCreatePage } from '@/pages/user-create'
import { UsersPage } from '@/pages/users'
import { routes } from '@/shared/routes'
import { AppLayout } from '../layout/AppLayout'

// A factory, not a module constant: the router must read the URL after Keycloak has cleaned it up.
export const createAppRouter = () =>
  createBrowserRouter([
    {
      Component: AppLayout,
      children: [
        { path: routes.home, Component: HomePage },
        { path: routes.tenants, Component: TenantsListPage },
        { path: routes.tenantNew, Component: OnboardingPage },
        { path: routes.tenantOnboarding, Component: OnboardingPage },
        { path: routes.tenantDetail, Component: TenantDetailPage },
        { path: routes.users, Component: UsersPage },
        { path: routes.userNew, Component: UserCreatePage },
        { path: routes.profiles, Component: ProfilesPage },
        { path: routes.modules, Component: ModulesPage },
        { path: routes.plans, Component: PlansPage },
        { path: routes.subscriptions, Component: SubscriptionsPage },
        { path: routes.billing, Component: BillingPage },
        { path: routes.communications, Component: CommunicationsPage },
        { path: routes.audit, Component: AuditPage },
        { path: routes.settings, Component: SettingsPage },
        { path: '*', Component: NotFoundPage },
      ],
    },
  ])
