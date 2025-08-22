# Third-Party Services & Packages Summary

## External Services

### 1. **SendGrid** (Email Service)
- **Purpose**: Transactional and bulk email delivery
- **Features Used**:
  - Single email sending (welcome, password reset)
  - Bulk email campaigns (event announcements)
  - Dynamic email templates
  - Email analytics and tracking
- **Cost**: Free tier up to 100 emails/day, then ~$15/month for 50k emails
- **Why SendGrid**: Industry leader in deliverability, good API, templates

### 2. **Google OAuth 2.0** (Authentication)
- **Purpose**: Social login for members
- **Features Used**:
  - OAuth 2.0 authentication flow
  - Basic profile information (email, name)
- **Cost**: Free
- **Why Google**: Most users have Google accounts, trusted provider

### 3. **PayPal Checkout** (Payments)
- **Purpose**: Process event registrations and donations
- **Features Used**:
  - Checkout SDK
  - Payment capture
  - Refund processing
- **Cost**: 3.49% + $0.49 per transaction
- **Why PayPal**: Widely trusted, no monthly fees, good for small volume

### 4. **Google Fonts** (Typography)
- **Purpose**: Web fonts for branding
- **Fonts Used**:
  - Montserrat (headings)
  - Source Sans 3 (body)
  - Bodoni Moda (display)
  - Satisfy (accent)
- **Cost**: Free
- **Why Google Fonts**: Reliable CDN, no licensing issues

## NuGet Packages

### Core Framework
```xml
<!-- ASP.NET Core -->
<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.*" />
```

### UI Components
```xml
<!-- Syncfusion Blazor Components -->
<PackageReference Include="Syncfusion.Blazor" Version="24.1.*" />
```
- **License**: Commercial (already available)
- **Components Used**: DataGrid, DatePicker, Dropdown, Dialog, Toast

### Authentication & Security
```xml
<!-- OAuth and JWT -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.*" />

<!-- Password Hashing -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />
```

### Email & Communication
```xml
<!-- SendGrid SDK -->
<PackageReference Include="SendGrid" Version="9.28.*" />
```

### Payments
```xml
<!-- PayPal SDK -->
<PackageReference Include="PayPalCheckoutSdk" Version="1.0.*" />
```

### Validation
```xml
<!-- FluentValidation -->
<PackageReference Include="FluentValidation" Version="11.9.*" />
```

### Caching
```xml
<!-- Built into ASP.NET Core -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.0.*" />
```

### Logging
```xml
<!-- Structured Logging -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.*" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.*" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.*" />
```

### Testing
```xml
<!-- Unit Testing -->
<PackageReference Include="xUnit" Version="2.6.*" />
<PackageReference Include="Moq" Version="4.20.*" />
<PackageReference Include="FluentAssertions" Version="6.12.*" />

<!-- Blazor Component Testing -->
<PackageReference Include="bunit" Version="1.26.*" />

<!-- Integration Testing -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.*" />
```

## Infrastructure Dependencies

### Runtime
- **.NET 9.0 Runtime** (included in Docker image)
- **SQLite** (embedded, no separate installation)

### Development Tools
- **Docker Desktop** (for containerization)
- **Visual Studio 2022** or **VS Code**
- **.NET 9.0 SDK**

### Deployment
- **Docker** (single container)
- **Linux VPS** (Ubuntu/Debian)
- **Nginx** (reverse proxy)
- **Let's Encrypt** (SSL certificates)

## Cost Analysis

### Monthly Costs (Estimated for 600 members)
- **VPS Hosting**: $10-20 (DigitalOcean/Linode)
- **SendGrid**: $15 (for event announcements)
- **Domain**: $15/year ($1.25/month)
- **SSL**: Free (Let's Encrypt)
- **PayPal**: Transaction-based only
- **Total**: ~$30-40/month

### One-Time Costs
- **Syncfusion License**: Already available
- **Development Tools**: Free (VS Code) or covered (Visual Studio)

## Why These Choices?

### Simplicity First
- **SQLite**: No database server to manage
- **Memory Cache**: No Redis infrastructure
- **SendGrid**: Reliable email without managing SMTP
- **Blazor Server**: No separate API or SPA build process

### Cost Effective
- Most services have generous free tiers
- No monthly minimums
- Transaction-based pricing scales with usage
- Single server deployment

### Developer Friendly
- All services have excellent .NET SDKs
- Good documentation
- Active community support
- Easy local development

### Production Ready
- All services are enterprise-grade
- High availability and reliability
- Good security practices
- Compliance features (GDPR, etc.)

This stack provides everything needed for a community platform while keeping complexity and costs minimal.