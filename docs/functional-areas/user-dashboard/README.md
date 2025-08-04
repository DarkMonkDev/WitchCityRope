# User Dashboard Enhancement

## Overview

This folder contains all documentation for implementing the user dashboard feature for WitchCityRope. The dashboard serves as the primary landing page for authenticated members, providing personalized content and quick access to key features.

## Documentation Structure

1. **[Implementation Plan](implementation-plan.md)** - Comprehensive roadmap including requirements analysis, phases, and success criteria
2. **[Technical Design](technical-design.md)** - Detailed technical specifications, component architecture, and code examples
3. **[Testing Strategy](testing-strategy.md)** - Complete testing approach including unit, integration, E2E, and visual tests
4. **[MCP Visual Testing Guide](mcp-visual-testing-guide.md)** - Step-by-step guide for using MCP tools to test the dashboard

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Node.js 18+ (for MCP tools)
- Docker Desktop (for PostgreSQL)
- Visual Studio 2022 or VS Code
- Chrome browser (for testing)

### Development Setup

1. **Create feature branch**:
   ```bash
   git checkout -b feature/user-dashboard
   ```

2. **Start required services**:
   ```bash
   # Start PostgreSQL
   docker-compose up -d postgres
   
   # Start API
   cd src/WitchCityRope.Api
   dotnet run
   
   # Start Web (new terminal)
   cd src/WitchCityRope.Web
   dotnet run
   ```

3. **Access application**:
   - Web: https://localhost:8281
   - API: https://localhost:8181
   - Dashboard: https://localhost:8281/member/dashboard

### Test Accounts

| Role | Email | Password | Features |
|------|-------|----------|----------|
| Admin | admin@witchcityrope.com | Test123! | Full access + Admin tools |
| Member | member@witchcityrope.com | Test123! | Standard member features |
| Unvetted | guest@witchcityrope.com | Test123! | Limited access + vetting prompt |

## Implementation Checklist

### Phase 1: Core Structure ⬜
- [ ] Create Dashboard.razor page component
- [ ] Implement Dashboard.razor.cs code-behind
- [ ] Add authentication attribute
- [ ] Create loading skeleton component
- [ ] Set up basic responsive layout
- [ ] Configure routing

### Phase 2: Components ⬜
- [ ] DashboardHeader (welcome message)
- [ ] UpcomingEvents component
- [ ] MembershipStatus card
- [ ] QuickLinks grid
- [ ] AdminQuickAccess (admin only)

### Phase 3: Services ⬜
- [ ] Create IDashboardService interface
- [ ] Implement DashboardService
- [ ] Add caching logic
- [ ] Wire up dependency injection
- [ ] Create view models/DTOs

### Phase 4: API Integration ⬜
- [ ] Create DashboardController
- [ ] Implement dashboard endpoints
- [ ] Add authorization checks
- [ ] Implement data aggregation
- [ ] Add response caching

### Phase 5: Testing ⬜
- [ ] Unit tests (95% coverage)
- [ ] Integration tests
- [ ] E2E tests
- [ ] Visual regression tests
- [ ] Performance tests
- [ ] Accessibility audit

### Phase 6: Polish ⬜
- [ ] Animations and transitions
- [ ] Error states
- [ ] Empty states
- [ ] Loading states
- [ ] Mobile optimizations
- [ ] Dark mode support (if applicable)

## Key Features

### 1. Personalized Welcome
- Displays user's scene name
- Contextual greeting based on time of day
- Quick action buttons

### 2. Event Management
- Shows next 3 upcoming registered events
- Registration status badges
- Quick access to tickets
- Empty state with CTA


### 4. Membership Status
- Visual verification badge
- Key statistics
- Vetting progress
- Member since date

### 5. Quick Links
- Common actions grid
- Icon-based navigation
- Hover effects
- Tooltips

### 6. Role-Based Content
- Admin tools section
- Teacher resources
- Progressive disclosure
- Permission-based visibility

## Technical Stack

- **Frontend**: Blazor Server (.NET 9)
- **UI Components**: Syncfusion Blazor
- **Styling**: CSS3 with CSS Variables
- **Icons**: Font Awesome 6
- **Testing**: bUnit, xUnit, Playwright
- **API**: ASP.NET Core Web API
- **Database**: PostgreSQL 16

## Design System

### Colors
```css
--wcr-color-burgundy: #880124;
--wcr-color-plum: #614B79;
--wcr-color-amber: #FFBF00;
--wcr-color-ivory: #FFF8F0;
--wcr-color-cream: #FAF6F2;
--wcr-color-midnight: #1A1A2E;
```

### Typography
- **Display**: Bodoni Moda (serif)
- **Headings**: Montserrat (sans-serif)
- **Body**: Source Sans 3 (sans-serif)

### Spacing
- Base unit: 8px
- xs: 8px, sm: 16px, md: 24px, lg: 32px, xl: 48px

### Breakpoints
- Mobile: < 768px
- Tablet: 768px - 1023px
- Desktop: 1024px+
- Large: 1440px+

## Performance Targets

| Metric | Target | Maximum |
|--------|--------|---------|
| First Contentful Paint | 1.2s | 1.5s |
| Time to Interactive | 2.5s | 3.0s |
| Lighthouse Score | 95+ | 90+ |
| Bundle Size | <200KB | <300KB |

## Testing Commands

```bash
# Run all dashboard tests
dotnet test --filter "FullyQualifiedName~Dashboard"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "Dashboard"

# Run specific test type
dotnet test --filter "Category=Dashboard&Category=Unit"

# Watch mode
dotnet watch test --filter "Dashboard"

# E2E tests
pwsh tests/WitchCityRope.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
dotnet test tests/WitchCityRope.E2E.Tests --filter "Feature=Dashboard"
```

## MCP Visual Testing

### Quick Test
```javascript
// Capture dashboard screenshot
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  fullPage: true,
  filename: "dashboard-test.png"
});

// Run accessibility audit
await mcp_browser_tools.audit({
  url: "https://localhost:8281/member/dashboard",
  type: "accessibility"
});
```

### Full Test Suite
See [MCP Visual Testing Guide](mcp-visual-testing-guide.md) for complete testing procedures.

## Deployment

### Environment Variables
```bash
# Required
ConnectionStrings__DefaultConnection=...
JwtSettings__SecretKey=...
Syncfusion__LicenseKey=...

# Optional
Email__SendGrid__ApiKey=...
ApplicationInsights__InstrumentationKey=...
```

### Feature Flags
```json
{
  "Features": {
    "Dashboard": {
      "Enabled": true,
      "NewLayout": false
    }
  }
}
```

## Monitoring

### Key Metrics
- Page load time
- API response time
- Error rate
- User engagement
- Feature adoption

### Alerts
- Dashboard load time > 3s
- API errors > 1%
- Failed logins > 10/min
- Memory usage > 80%

## Support

### Documentation
- [Blazor Docs](https://docs.microsoft.com/aspnet/core/blazor/)
- [Syncfusion Blazor](https://blazor.syncfusion.com/)
- [bUnit Testing](https://bunit.dev/)

### Internal Resources
- Architecture Decision Records: `/docs/architecture/decisions/`
- API Documentation: `/docs/api/`
- Design System: `/docs/design/design-system.md`

## Next Steps

1. **Review** this documentation with the team
2. **Approve** the technical design
3. **Create** feature branch
4. **Implement** Phase 1 (Core Structure)
5. **Test** continuously during development
6. **Deploy** to staging for QA
7. **Release** with feature flag

## Updates Log

| Date | Author | Changes |
|------|--------|---------|
| 2024-01-07 | Claude | Initial documentation created |

---

For questions or clarifications, please refer to the detailed documentation files in this folder or contact the development team.