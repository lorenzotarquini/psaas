import { createBrowserRouter, Navigate } from 'react-router'
import { DashboardPage } from '@/pages/dashboard'
import { NetWorthPage } from '@/pages/net-worth'
import { PortfolioPage } from '@/pages/portfolio'
import { SetupPage } from '@/pages/setup'
import { TransactionsPage } from '@/pages/transactions'
import { AppLayout } from '../layout/AppLayout'

export const routes = [
  {
    Component: AppLayout,
    children: [
      { index: true, Component: DashboardPage },
      { path: 'setup', Component: SetupPage },
      { path: 'transactions', Component: TransactionsPage },
      { path: 'portfolio', Component: PortfolioPage },
      { path: 'net-worth', Component: NetWorthPage },
      { path: '*', element: <Navigate to="/" replace /> },
    ],
  },
]

// A factory, not a module constant: the router must read the URL after Keycloak has cleaned it up.
export const createAppRouter = () => createBrowserRouter(routes)
