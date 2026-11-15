import { createBrowserRouter } from 'react-router'
import { HomePage } from '@/pages/home'
import { AppLayout } from '../layout/AppLayout'

// A factory, not a module constant: the router must read the URL after Keycloak has cleaned it up.
export const createAppRouter = () =>
  createBrowserRouter([{ Component: AppLayout, children: [{ path: '*', Component: HomePage }] }])
