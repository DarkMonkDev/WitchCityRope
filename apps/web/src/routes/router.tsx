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
import { EventsManagementApiDemo } from '../pages/admin/EventsManagementApiDemo';
// import { EventsManagementApiDemoMinimal } from '../pages/admin/EventsManagementApiDemo.minimal';
import { EventsManagementApiDemoTest } from '../pages/admin/EventsManagementApiDemo.test';
// import { EventsManagementApiDemoDebug } from '../pages/admin/EventsManagementApiDemo.debug';
import { EventFormTestPage } from '../pages/EventFormTestPage';
import TestTinyMCE from '../pages/TestTinyMCE';
import { NavigationTestPage } from '../pages/NavigationTestPage';
import { RootLayout } from '../components/layout/RootLayout';
import { RootErrorBoundary } from '../components/errors/RootErrorBoundary';
import { authLoader } from './loaders/authLoader';

// Events system pages
import { EventsListPage } from '../pages/events/EventsListPage';
import { EventDetailPage } from '../pages/events/EventDetailPage';
import { AdminEventsPage } from '../pages/admin/AdminEventsPage';
// import { AdminEventDetailsPage } from '../pages/admin/AdminEventDetailsPage';
import { AdminDashboardPage } from '../pages/admin/AdminDashboardPage';
import { AdminSafetyPage } from '../pages/admin/AdminSafetyPage';
import { TestPage } from '../pages/TestPage';
import { VettingTestPage } from '../pages/VettingTestPage';

// Safety system pages - temporarily disabled due to TypeScript errors
// import { SafetyReportPage } from '../pages/safety/SafetyReportPage';
import { SafetyStatusPage } from '../pages/safety/SafetyStatusPage';

// CheckIn system pages - temporarily disabled due to TypeScript errors
// import { CheckInPage } from '../pages/checkin/CheckInPage';
// import { CheckInDashboardPage } from '../pages/checkin/CheckInDashboardPage';

// Vetting system pages - temporarily disabled due to TypeScript errors
// import { VettingApplicationPage, VettingStatusPage, ReviewerDashboardPage } from '../features/vetting';

// Payment system pages
import { EventPaymentPage } from '../features/payments';
import { PaymentTestPage } from '../pages/PaymentTestPage';
import { PaymentSuccessPage } from '../pages/payments/PaymentSuccessPage';
import { PaymentCancelPage } from '../pages/payments/PaymentCancelPage';

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
        path: "test",
        element: <TestPage />
      },
      {
        path: "vetting-test",
        element: <VettingTestPage />
      },
      { 
        path: "login", 
        element: <LoginPage /> 
      },
      { 
        path: "register", 
        element: <RegisterPage /> 
      },
      
      // Events system routes
      {
        path: "events",
        element: <EventsListPage />
      },
      {
        path: "events/:id",
        element: <EventDetailPage />
      },
      
      // CheckIn system routes (protected) - temporarily disabled due to TypeScript errors
      // {
      //   path: "events/:eventId/checkin",
      //   element: <CheckInPage />,
      //   loader: authLoader
      // },
      // {
      //   path: "events/:eventId/checkin/dashboard",
      //   element: <CheckInDashboardPage />,
      //   loader: authLoader
      // },
      
      // Safety system routes (public) - temporarily disabled due to TypeScript errors
      // {
      //   path: "safety/report",
      //   element: <SafetyReportPage />
      // },
      {
        path: "safety/status",
        element: <SafetyStatusPage />
      },
      
      // Vetting system routes - temporarily disabled due to TypeScript errors
      // {
      //   path: "vetting/apply",
      //   element: <VettingApplicationPage />
      // },
      // {
      //   path: "vetting/status", 
      //   element: <VettingStatusPage />
      // },
      // {
      //   path: "vetting/reviewer",
      //   element: <ReviewerDashboardPage />,
      //   loader: authLoader
      // },
      
      // Payment system routes (protected)
      {
        path: "events/:eventId/payment/:registrationId",
        element: <EventPaymentPage />,
        loader: authLoader
      },
      {
        path: "payment/success",
        element: <PaymentSuccessPage />
      },
      {
        path: "payment/cancel", 
        element: <PaymentCancelPage />
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
        path: "test-tinymce", 
        element: <TestTinyMCE /> 
      },
      { 
        path: "navigation-test", 
        element: <NavigationTestPage /> 
      },
      { 
        path: "payment-test", 
        element: <PaymentTestPage /> 
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
      },
      
      // Admin routes - authentication and admin role required
      {
        path: "admin",
        element: <AdminDashboardPage />,
        loader: authLoader
      },
      {
        path: "admin/events",
        element: <AdminEventsPage />,
        loader: authLoader
      },
      // {
      //   path: "admin/events/:id",
      //   element: <AdminEventDetailsPage />,
      //   loader: authLoader
      // },
      {
        path: "admin/safety",
        element: <AdminSafetyPage />,
        loader: authLoader
      },
      
      // Demo routes - no authentication required for demos
      {
        path: "admin/event-session-matrix-demo",
        element: <EventSessionMatrixDemo />
      },
      {
        path: "admin/events-management-api-demo",
        element: <EventsManagementApiDemo />
      },
      {
        path: "event-form-test",
        element: <EventFormTestPage />
      }
    ]
  },
  // Test route OUTSIDE of RootLayout to bypass Navigation/UtilityBar
  {
    path: "/test-no-layout",
    element: <EventsManagementApiDemoTest />
  }
]);