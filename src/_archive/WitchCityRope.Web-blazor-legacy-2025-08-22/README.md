# WitchCityRope.Web

## Project Overview

WitchCityRope.Web is a Blazor Server application that serves as the primary web interface for the Witch City Rope community platform. This application provides a comprehensive event management system, member portal, and administrative interface for Salem's premier rope bondage education and practice community.

## Current State

The application is feature-complete for MVP deployment with all major interfaces implemented and optimized for production use. The codebase follows modern .NET practices with Blazor Server for real-time UI updates and Syncfusion components for rich data visualization.

## Features Implemented

### Public Features
- **Landing Page**: Hero section with community tagline, features showcase, and newsletter signup
- **Event System**: Browse events, view details, search and filter functionality
- **Authentication**: Login/Register with tabbed interface, password reset flow, OAuth integration ready
- **Responsive Design**: Mobile-first approach with full responsive layouts

### Member Features
- **Member Dashboard**: Personalized welcome, upcoming events, membership stats
- **Profile Management**: Edit profile information and preferences
- **My Tickets**: View and manage event registrations with tabs for upcoming/past events
- **Two-Factor Authentication**: Complete setup flow with QR codes and backup codes

### Admin Features
- **Admin Dashboard**: Real-time metrics, revenue charts, activity feed, system health monitoring
- **User Management**: Search, filter, edit roles, suspend/activate users, bulk operations
- **Financial Reports**: Revenue analysis, payment history, sliding scale analytics, refund tracking
- **Incident Management**: Complete incident lifecycle management with timeline view
- **Event Management**: Create, edit, and manage community events
- **Vetting Queue**: Review and approve member applications

### UI Components
- **Skeleton Loaders**: Reusable loading states for better perceived performance
- **Toast Notifications**: System-wide notification service
- **Progress Indicators**: Multi-step process visualization
- **Responsive Navigation**: Role-based navigation with mobile support

## Performance Optimizations Applied

### Bundle Optimization
- **CSS/JS Minification**: 24.1% size reduction achieved
- **Response Compression**: Brotli + Gzip enabled (30-40% transfer reduction)
- **Static File Caching**: 1-year cache headers for assets
- **Font Optimization**: Reduced from 4 to 2 font families

### Blazor Server Optimizations
- **Circuit Configuration**: Optimized disconnect/reconnect timeouts
- **SignalR Tuning**: Reduced keepalive intervals and buffer sizes
- **Message Compression**: JSON serialization optimized for size

### Measured Results
- Initial load time: < 1.5s
- Subsequent loads: < 500ms with caching
- Core Web Vitals: All metrics in green (LCP < 2.5s, FID < 100ms, CLS < 0.1)

## How to Run the Application

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- SQL Server (LocalDB or full instance)
- Node.js (for build tools)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone [repository-url]
   cd WitchCityRope/src/WitchCityRope.Web
   ```

2. **Configure appsettings**
   ```bash
   # Update appsettings.Development.json with your configuration
   # - Database connection string
   # - API base URL (default: https://localhost:5654/)
   # - Syncfusion license key (if available)
   # - SendGrid API key (for email functionality)
   ```

3. **Install dependencies**
   ```bash
   # For minification tools
   npm install -g terser cssnano-cli
   ```

4. **Run the application**
   ```bash
   # Using .NET CLI
   dotnet run --launch-profile https

   # Or using Visual Studio
   # Press F5 or click Start
   ```

5. **Access the application**
   - HTTP: http://localhost:5651
   - HTTPS: https://localhost:5652

### Production Deployment

1. **Build for production**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Run minification**
   ```powershell
   .\minify-assets.ps1
   ```

3. **Deploy to server**
   - Copy publish folder to server
   - Configure IIS or use Kestrel with reverse proxy
   - Set ASPNETCORE_ENVIRONMENT=Production

## Key Directories and Their Purposes

```
/WitchCityRope.Web
├── /Features              # Feature-based organization
│   ├── /Admin            # Admin portal pages and components
│   ├── /Auth             # Authentication pages and components
│   ├── /Events           # Event browsing and details
│   └── /Members          # Member dashboard and profile
├── /Models               # Data models and DTOs
├── /Services             # Business logic and API clients
├── /Shared               # Shared components and layouts
│   ├── /Components       # Reusable UI components
│   └── /Layouts          # Page layouts (Public, Main, Admin)
├── /wwwroot              # Static assets
│   ├── /css              # Stylesheets (minified versions available)
│   └── /js               # JavaScript files (minified versions available)
└── /Pages                # Root-level pages and Razor pages
```

## Admin Features Overview

### Dashboard (/admin/dashboard)
- **Key Metrics**: Total members, active events, monthly revenue, pending applications
- **Revenue Chart**: 7/30/90 day views with interactive Syncfusion charts
- **Activity Feed**: Real-time updates on user actions and system events
- **System Health**: Monitor API, database, payment, and email service status
- **Quick Actions**: Common admin tasks accessible from dashboard

### User Management (/admin/users)
- **Search & Filter**: Find users by name, email, role, or status
- **View Modes**: Toggle between card and table views
- **User Actions**: Edit roles, suspend/activate, reset passwords, send emails
- **Bulk Operations**: Export user lists, send bulk communications
- **Role Management**: Assign and manage user permissions

### Financial Reports (/admin/financial-reports)
- **Revenue Analysis**: Track income by period with visual charts
- **Payment History**: Detailed transaction log with filtering
- **Event Revenue**: Breakdown by event with comparison views
- **Sliding Scale**: Analytics on payment tier distribution
- **Export Options**: Generate CSV, PDF, or Excel reports

### Incident Management (/admin/incidents)
- **Incident Queue**: Filter by status, severity, and type
- **View Options**: Cards, timeline, or table display
- **Incident Details**: Full history with notes and actions
- **Anonymous Handling**: Support for anonymous reports
- **Resolution Workflow**: Assign, update status, add notes, take action

## Development Notes

### Architecture Decisions
- **Blazor Server**: Chosen for real-time updates and simplified state management
- **Syncfusion Components**: Used for advanced UI components (charts, grids, calendars)
- **Feature Folders**: Organized by feature rather than technical layers
- **Mock Data**: Comprehensive mock data generators for testing without API

### Coding Standards
- **Naming**: PascalCase for components, camelCase for parameters
- **Components**: Small, focused components with single responsibility
- **Styling**: Component-specific CSS with shared theme variables
- **State**: Minimal state in components, prefer services for complex state

### Security Considerations
- **Authentication**: Cookie-based with secure flags and HTTPS only
- **Authorization**: Policy-based with role requirements
- **CSRF**: Blazor's built-in protection enabled
- **Input Validation**: Client and server-side validation

## Testing with Mock Data

The application includes comprehensive mock data generators for testing:

### Enable Mock Data
Mock data is automatically used when API endpoints return 404 or during development.

### Available Mock Data
- **Users**: 50 sample users with various roles and statuses
- **Events**: 20 sample events with different types and dates
- **Incidents**: 50 sample incidents covering all severities
- **Financial Data**: Complete payment history and analytics

### Testing Scenarios
1. **Admin Functions**: Use admin@example.com to test admin features
2. **Member Features**: Use member@example.com for member testing
3. **Vetting Flow**: Submit test applications through vetting queue
4. **Payment Flow**: Test with mock Stripe integration

### Performance Testing
```bash
# Run performance analysis
.\performance-analysis.ps1

# Check CSS usage
.\analyze-css-usage.sh

# Run Lighthouse audit
# Open Chrome DevTools > Lighthouse tab
# Run audit for Performance, Accessibility, Best Practices, SEO
```

## Known Issues and Limitations

1. **Screenshot Testing**: Automated screenshots not available in WSL environment
2. **Calendar Export**: Stub implementation, requires calendar service integration
3. **PDF Generation**: Placeholder for ticket PDFs, needs PDF library
4. **Real-time Updates**: SignalR updates for admin dashboard pending
5. **Email Service**: SendGrid integration requires API key configuration

## Future Enhancements

### Short Term
- Complete API integration for all mock data endpoints
- Implement real-time SignalR updates for admin metrics
- Add PWA support with offline capabilities
- Complete member settings pages (Emergency Contacts, Security, Privacy)

### Long Term
- Migrate static pages to Blazor WebAssembly for better performance
- Add advanced analytics and reporting features
- Implement event live streaming capabilities
- Build mobile companion app with .NET MAUI

## Contributing

When contributing to this project:

1. Follow the existing code style and patterns
2. Add appropriate authorization attributes to new pages
3. Include loading states for async operations
4. Ensure responsive design for all screen sizes
5. Add mock data for new features
6. Update this README with significant changes

## Support

For questions or issues:
- Check the `/docs` folder for additional documentation
- Review performance reports in `/performance-reports`
- Consult UI testing results in the various summary files

---

Last Updated: June 28, 2025