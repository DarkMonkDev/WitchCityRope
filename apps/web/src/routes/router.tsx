import { createBrowserRouter } from 'react-router-dom';
import { HomePage } from '../pages/HomePage';
import { LoginPage } from '../pages/LoginPage';
import { RegisterPage } from '../pages/RegisterPage';
import { DashboardPage } from '../pages/dashboard/DashboardPage';
import { EventsPage } from '../pages/dashboard/EventsPage';
import { ProfilePage } from '../pages/dashboard/ProfilePage';
import { SecurityPage } from '../pages/dashboard/SecurityPage';
import { MembershipPage } from '../pages/dashboard/MembershipPage';
import { FormComponentsTest } from '../pages/FormComponentsTest';
import MantineFormTest from '../pages/MantineFormTest';
// TODO: Fix TypeScript errors in API validation pages before uncommenting
// import ApiValidation from '../pages/ApiValidation';
import ApiValidationV2Simple from '../pages/ApiValidationV2Simple';
import ApiConnectionTest from '../pages/ApiConnectionTest';
import { TestMSWPage } from '../pages/TestMSWPage';
import { EventSessionMatrixDemo } from '../pages/admin/EventSessionMatrixDemo';
import { TinyMCETestPage } from '../pages/TinyMCETest';
import { EventsListPage } from '../pages/events/EventsListPage';
import { EventDetailPage } from '../pages/events/EventDetailPage';
import { RootLayout } from '../components/layout/RootLayout';
import { RootErrorBoundary } from '../components/errors/RootErrorBoundary';
import { authLoader } from './loaders/authLoader';

/**
 * React Router v7 configuration following validated patterns
 * Reference: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 */
export const router = createBrowserRouter([
  {
    path: "/",
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // Public routes - no authentication required
      { 
        index: true, 
        element: <HomePage /> 
      },
      { 
        path: "login", 
        element: <LoginPage /> 
      },
      { 
        path: "register", 
        element: <RegisterPage /> 
      },
      
      // Test/Development routes (public for now)
      { 
        path: "form-test", 
        element: <FormComponentsTest /> 
      },
      { 
        path: "mantine-forms", 
        element: <MantineFormTest /> 
      },
      // TODO: Fix TypeScript errors before uncommenting
      // { 
      //   path: "api-validation", 
      //   element: <ApiValidation /> 
      // },
      { 
        path: "api-validation-v2-simple", 
        element: <ApiValidationV2Simple /> 
      },
      { 
        path: "api-connection-test", 
        element: <ApiConnectionTest /> 
      },
      { 
        path: "test-msw", 
        element: <TestMSWPage /> 
      },
      {
        path: "admin/event-session-matrix-demo",
        element: <EventSessionMatrixDemo />
      },
      {
        path: "tinymce-test",
        element: <TinyMCETestPage />
      },
      
      // Public Events Pages
      {
        path: "events",
        element: <EventsListPage />
      },
      {
        path: "events/:id",
        element: <EventDetailPage />
      },
      
      // Protected routes - authentication required
      // All dashboard routes use DashboardLayout in each page component
      {
        path: "dashboard",
        element: <DashboardPage />,
        loader: authLoader
      },
      {
        path: "dashboard/events",
        element: <EventsPage />,
        loader: authLoader
      },
      {
        path: "dashboard/profile",
        element: <ProfilePage />,
        loader: authLoader
      },
      {
        path: "dashboard/security",
        element: <SecurityPage />,
        loader: authLoader
      },
      {
        path: "dashboard/membership",
        element: <MembershipPage />,
        loader: authLoader
      }
    ]
  }
]);