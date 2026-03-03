import { createBrowserRouter } from 'react-router-dom'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'
import { VerifyEmailPage } from '../pages/VerifyEmailPage'
import { ForgotPasswordPage } from '../pages/ForgotPasswordPage'
import { ResetPasswordPage } from '../pages/ResetPasswordPage'
import { MyEventsPage } from '../pages/dashboard/MyEventsPage'
import { ProfileSettingsPage } from '../pages/dashboard/ProfileSettingsPage'
import { FormComponentsTest } from '../pages/FormComponentsTest'
import MantineFormTest from '../pages/MantineFormTest'
// TODO: Fix TypeScript errors in API validation pages before uncommenting
// import ApiValidation from '../pages/ApiValidation';
import ApiValidationV2Simple from '../pages/ApiValidationV2Simple'
import ApiConnectionTest from '../pages/ApiConnectionTest'
import { EventSessionMatrixDemo } from '../pages/admin/EventSessionMatrixDemo'
import { EventsManagementApiDemo } from '../pages/admin/EventsManagementApiDemo'
// import { EventsManagementApiDemoMinimal } from '../pages/admin/EventsManagementApiDemo.minimal';
// import { EventsManagementApiDemoDebug } from '../pages/admin/EventsManagementApiDemo.debug';
import { EventFormTestPage } from '../pages/EventFormTestPage'
import { NavigationTestPage } from '../pages/NavigationTestPage'
import { RootLayout } from '../components/layout/RootLayout'
import { RootErrorBoundary } from '../components/errors/RootErrorBoundary'
import { LowercaseUrlRedirect } from './guards/LowercaseUrlRedirect'
import { authLoader } from './loaders/authLoader'
import { adminLoader } from './loaders/adminLoader'
import { UnauthorizedPage } from '../pages/UnauthorizedPage'
import { NotFoundPage } from '../pages/NotFoundPage'

// Events system pages
import { EventsListPage } from '../pages/events/EventsListPage'
import { EventDetailPage } from '../pages/events/EventDetailPage'
import { AdminEventsPage } from '../pages/admin/AdminEventsPage'
import { AdminEventDetailsPage } from '../pages/admin/AdminEventDetailsPage'
import { NewEventPage } from '../pages/admin/NewEventPage'
import { AdminDashboardPage } from '../pages/admin/AdminDashboardPage'
import { AdminIncidentDashboard } from '../pages/admin/safety/AdminIncidentDashboard'
import { AdminIncidentDetailPage } from '../pages/admin/safety/AdminIncidentDetailPage'
import { AdminVettingPage } from '../pages/admin/AdminVettingPage'
import { AdminVettingApplicationDetailPage } from '../pages/admin/AdminVettingApplicationDetailPage'
import { AdminMembersPage } from '../pages/admin/AdminMembersPage'
import { AdminMemberDetailsPage } from '../pages/admin/AdminMemberDetailsPage'
import { AdminSettingsPage } from '../pages/admin/AdminSettingsPage'
import { EmailTemplatesAdminPage } from '../pages/admin/EmailTemplatesAdminPage'
import { AdminPaymentsPage } from '../pages/admin/AdminPaymentsPage'
import { TestPage } from '../pages/TestPage'
import { VettingTestPage } from '../pages/VettingTestPage'
import { TestNotifications } from '../pages/TestNotifications'

// Safety system pages
import { SafetyReportPage } from '../pages/safety/SafetyReportPage'
import { SafetyStatusPage } from '../pages/safety/SafetyStatusPage'
import { MyReportsPage } from '../pages/MyReportsPage'
import { MyReportDetailView } from '../pages/MyReportDetailView'

// CheckIn system pages
import { CheckInPage } from '../pages/checkin/CheckInPage';
import { CheckInDashboardPage } from '../pages/checkin/CheckInDashboardPage';

// Vetting system pages
import { VettingApplicationPage } from '../features/vetting/pages/VettingApplicationPage'

// CMS system pages
import { CmsDynamicPage } from '../features/cms/pages/CmsDynamicPage'
import { CmsRevisionListPage } from '../features/cms/pages/CmsRevisionListPage'
import { CmsRevisionDetailPage } from '../features/cms/pages/CmsRevisionDetailPage'

// Payment system pages
import { EventPaymentPage } from '../features/payments'
import { PaymentTestPage } from '../pages/PaymentTestPage'

/**
 * React Router v7 configuration following validated patterns
 * Reference: /docs/functional-areas/routing-validation/requirements/functional-specification.md
 */
// Wraps all routes in LowercaseUrlRedirect to normalize URLs to lowercase,
// preventing 404s when users type mixed-case URLs (e.g., /Admin/Events → /admin/events)
export const router = createBrowserRouter([
  {
    element: <LowercaseUrlRedirect />,
    children: [
  {
    path: '/',
    element: <RootLayout />,
    errorElement: <RootErrorBoundary />,
    children: [
      // Public routes - no authentication required
      {
        index: true,
        element: <HomePage />,
      },
      {
        path: 'test',
        element: <TestPage />,
      },
      {
        path: 'vetting-test',
        element: <VettingTestPage />,
      },
      {
        path: 'test-notifications',
        element: <TestNotifications />,
      },
      {
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'register',
        element: <RegisterPage />,
      },
      {
        path: 'verify-email',
        element: <VerifyEmailPage />,
      },
      {
        path: 'forgot-password',
        element: <ForgotPasswordPage />,
      },
      {
        path: 'reset-password',
        element: <ResetPasswordPage />,
      },
      {
        path: 'unauthorized',
        element: <UnauthorizedPage />,
      },

      // Events system routes
      {
        path: 'events',
        element: <EventsListPage />,
      },
      {
        path: 'events/:id',
        element: <EventDetailPage />,
      },

      // Safety system routes (public)
      {
        path: 'safety/report',
        element: <SafetyReportPage />
      },
      {
        path: 'safety/status',
        element: <SafetyStatusPage />,
      },

      // Vetting system routes (public access for viewing form)
      {
        path: 'join',
        element: <VettingApplicationPage />,
      },
      {
        path: 'vetting/apply',
        element: <VettingApplicationPage />,
      },

      // Payment system routes (protected)
      // New checkout route - supports both single and dual parameter patterns
      {
        path: 'checkout/:eventId',
        element: <EventPaymentPage />,
        loader: authLoader,
      },
      {
        path: 'checkout/:eventId/:registrationId',
        element: <EventPaymentPage />,
        loader: authLoader,
      },
      // Keep alternative test route for development
      {
        path: 'events/:eventId/payment/:registrationId',
        element: <EventPaymentPage />,
        loader: authLoader,
      },
      // Test/Development routes (public for now)
      {
        path: 'form-test',
        element: <FormComponentsTest />,
      },
      {
        path: 'mantine-forms',
        element: <MantineFormTest />,
      },
      // TODO: Fix TypeScript errors before uncommenting
      // {
      //   path: "api-validation",
      //   element: <ApiValidation />
      // },
      {
        path: 'api-validation-v2-simple',
        element: <ApiValidationV2Simple />,
      },
      {
        path: 'api-connection-test',
        element: <ApiConnectionTest />,
      },
      {
        path: 'navigation-test',
        element: <NavigationTestPage />,
      },
      {
        path: 'payment-test',
        element: <PaymentTestPage />,
      },

      // Demo routes - no authentication required for demos
      // IMPORTANT: These MUST come BEFORE protected admin routes to avoid route matching issues
      {
        path: 'admin/event-session-matrix-demo',
        element: <EventSessionMatrixDemo />,
      },
      {
        path: 'admin/events-management-api-demo',
        element: <EventsManagementApiDemo />,
      },
      {
        path: 'event-form-test',
        element: <EventFormTestPage />,
      },

      // Protected routes - authentication required
      // All dashboard routes use DashboardLayout in each page component
      {
        path: 'dashboard',
        element: <MyEventsPage />,
        loader: authLoader,
      },
      {
        path: 'dashboard/profile-settings',
        element: <ProfileSettingsPage />,
        loader: authLoader,
      },

      // My Reports routes - authentication required (identified users only)
      {
        path: 'my-reports',
        element: <MyReportsPage />,
        loader: authLoader,
      },
      {
        path: 'my-reports/:id',
        element: <MyReportDetailView />,
        loader: authLoader,
      },

      // Admin routes - authentication and admin role required
      // SECURITY: All admin routes use adminLoader which validates "Administrator" role
      {
        path: 'admin',
        element: <AdminDashboardPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/events',
        element: <AdminEventsPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/events/new',
        element: <NewEventPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/events/:id',
        element: <AdminEventDetailsPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/safety/incidents',
        element: <AdminIncidentDashboard />,
        loader: adminLoader,
      },
      {
        path: 'admin/safety/incidents/:id',
        element: <AdminIncidentDetailPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/vetting',
        element: <AdminVettingPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/vetting/applications/:applicationId',
        element: <AdminVettingApplicationDetailPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/members',
        element: <AdminMembersPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/members/:id',
        element: <AdminMemberDetailsPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/settings',
        element: <AdminSettingsPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/email-templates',
        element: <EmailTemplatesAdminPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/analytics/payments',
        element: <AdminPaymentsPage />,
        loader: adminLoader,
      },
      // CMS admin routes
      {
        path: 'admin/cms/revisions',
        element: <CmsRevisionListPage />,
        loader: adminLoader,
      },
      {
        path: 'admin/cms/revisions/:pageId',
        element: <CmsRevisionDetailPage />,
        loader: adminLoader,
      },

      // Dynamic CMS route - must be SECOND TO LAST to avoid matching static routes
      // Matches any single-level path that doesn't match routes above
      // Examples: /resources, /about-us, /terms-of-service, /faq, etc.
      // Note: CmsDynamicPage handles its own 404 when a slug doesn't exist in the CMS
      {
        path: ':slug',
        element: <CmsDynamicPage />,
      },

      // 404 catch-all route - MUST BE ABSOLUTELY LAST
      // Catches multi-segment unknown URLs (e.g., /foo/bar/baz) that don't match
      // any explicit route or the single-segment CMS :slug route above.
      // Single-segment unknown URLs (e.g., /asdfgh) are caught by :slug and
      // CmsDynamicPage renders NotFoundPage when the CMS API returns 404.
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
  // CheckIn system routes (KIOSK MODE - NO MAIN NAVIGATION)
  // Uses session tokens for access control - NO user login required
  // These routes are OUTSIDE RootLayout to provide clean kiosk experience
  {
    path: "events/:eventId/checkin",
    element: <CheckInPage />,
    errorElement: <RootErrorBoundary />
  },
  {
    path: "events/:eventId/checkin/dashboard",
    element: <CheckInDashboardPage />,
    errorElement: <RootErrorBoundary />
  },
    ],
  },
])
