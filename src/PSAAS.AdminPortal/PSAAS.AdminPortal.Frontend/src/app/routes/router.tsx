import { createBrowserRouter } from 'react-router'
import { AuditPage } from '@/pages/audit'
import { HomePage } from '@/pages/home'
import { NotFoundPage } from '@/pages/not-found'
import { ProfilesPage } from '@/pages/profiles'
import { SettingsPage } from '@/pages/settings'
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
        { path: routes.audit, Component: AuditPage },
        { path: routes.settings, Component: SettingsPage },
        { path: '*', Component: NotFoundPage },
      ],
    },
  ])
