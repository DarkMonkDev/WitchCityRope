# UI Deployment Checklist for WitchCityRope.Web

## Pre-Deployment Verification

### 1. UI Components Verification

#### Core Components
- [ ] **Navigation Components**
  - [ ] MainNav.razor renders correctly
  - [ ] Navigation links work properly
  - [ ] Responsive hamburger menu functions
  - [ ] Active page highlighting works

- [ ] **Layout Components**
  - [ ] MainLayout.razor loads without errors
  - [ ] PublicLayout.razor displays correctly
  - [ ] AdminLayout.razor (protected routes) works
  - [ ] Layout switching functions properly

- [ ] **Authentication Components**
  - [ ] Login.razor form submission works
  - [ ] TwoFactorModal.razor displays correctly
  - [ ] TwoFactorAuth.razor validation works
  - [ ] PasswordReset.razor flow completes
  - [ ] Logout functionality works

- [ ] **Event Components**
  - [ ] EventCard.razor displays all fields
  - [ ] EventList.razor pagination works
  - [ ] EventDetail.razor loads event data
  - [ ] Event images load properly
  - [ ] Price formatting displays correctly

- [ ] **Member Components**
  - [ ] Dashboard.razor loads user data
  - [ ] Profile.razor edit functionality works
  - [ ] MyTickets.razor displays purchases
  - [ ] Protected routes redirect properly

- [ ] **Admin Components**
  - [ ] EventManagement.razor CRUD operations
  - [ ] VettingQueue.razor displays queue
  - [ ] Admin-only routes are protected

- [ ] **Shared UI Components**
  - [ ] LoadingSpinner.razor displays
  - [ ] ToastContainer.razor notifications work
  - [ ] Error states display properly
  - [ ] Empty states show correctly

### 2. Browser Compatibility Checks

#### Desktop Browsers
- [ ] **Chrome (Latest)**
  - [ ] Layout renders correctly
  - [ ] JavaScript functions work
  - [ ] CSS animations smooth
  - [ ] Forms submit properly

- [ ] **Firefox (Latest)**
  - [ ] Layout renders correctly
  - [ ] JavaScript functions work
  - [ ] CSS animations smooth
  - [ ] Forms submit properly

- [ ] **Safari (Latest)**
  - [ ] Layout renders correctly
  - [ ] JavaScript functions work
  - [ ] CSS animations smooth
  - [ ] Forms submit properly

- [ ] **Edge (Latest)**
  - [ ] Layout renders correctly
  - [ ] JavaScript functions work
  - [ ] CSS animations smooth
  - [ ] Forms submit properly

#### Mobile Browsers
- [ ] **Chrome Mobile**
  - [ ] Touch interactions work
  - [ ] Viewport scaling correct
  - [ ] Forms are usable

- [ ] **Safari iOS**
  - [ ] Touch interactions work
  - [ ] Viewport scaling correct
  - [ ] Forms are usable

### 3. Mobile Responsiveness Verification

#### Breakpoints Testing
- [ ] **Mobile (320px - 767px)**
  - [ ] Navigation collapses to hamburger
  - [ ] Cards stack vertically
  - [ ] Forms fit within viewport
  - [ ] Modals are scrollable
  - [ ] Text is readable

- [ ] **Tablet (768px - 1023px)**
  - [ ] Layout adjusts appropriately
  - [ ] Grid layouts work
  - [ ] Navigation displays correctly
  - [ ] Images scale properly

- [ ] **Desktop (1024px+)**
  - [ ] Full layout displays
  - [ ] Multi-column layouts work
  - [ ] Hover states function
  - [ ] Maximum width constraints apply

#### Device-Specific Testing
- [ ] **iPhone SE (375px)**
- [ ] **iPhone 12/13 (390px)**
- [ ] **iPad (768px)**
- [ ] **iPad Pro (1024px)**
- [ ] **Android Phone (Various)**
- [ ] **Android Tablet (Various)**

### 4. Performance Benchmarks

#### Initial Load Performance
- [ ] **Time to First Byte (TTFB)**
  - Target: < 600ms
  - Actual: _______

- [ ] **First Contentful Paint (FCP)**
  - Target: < 1.8s
  - Actual: _______

- [ ] **Largest Contentful Paint (LCP)**
  - Target: < 2.5s
  - Actual: _______

- [ ] **Time to Interactive (TTI)**
  - Target: < 3.8s
  - Actual: _______

#### Runtime Performance
- [ ] **JavaScript Bundle Size**
  - Target: < 200KB (gzipped)
  - Actual: _______

- [ ] **CSS Bundle Size**
  - Target: < 50KB (gzipped)
  - Actual: _______

- [ ] **Image Optimization**
  - All images optimized
  - Lazy loading implemented
  - WebP format used where supported

#### Blazor-Specific Metrics
- [ ] **SignalR Connection Time**
  - Target: < 1s
  - Actual: _______

- [ ] **Component Render Time**
  - Target: < 50ms per component
  - Actual: _______

### 5. Security Checks for UI Elements

#### Input Validation
- [ ] **Form Inputs**
  - [ ] XSS prevention on all text inputs
  - [ ] SQL injection prevention
  - [ ] Input length validation
  - [ ] Special character handling

- [ ] **File Uploads**
  - [ ] File type validation
  - [ ] File size limits enforced
  - [ ] Malicious file detection

#### Authentication Security
- [ ] **Login Security**
  - [ ] Password field masked
  - [ ] No passwords in URLs
  - [ ] Session timeout works
  - [ ] CSRF tokens present

- [ ] **API Security**
  - [ ] JWT tokens stored securely
  - [ ] API calls use HTTPS
  - [ ] Unauthorized access handled
  - [ ] Token refresh works

#### Content Security
- [ ] **Content Security Policy**
  - [ ] CSP headers configured
  - [ ] Inline scripts minimized
  - [ ] External resources whitelisted

- [ ] **Data Exposure**
  - [ ] No sensitive data in HTML
  - [ ] Console logs removed
  - [ ] Debug info disabled

### 6. Accessibility Verification

#### WCAG 2.1 Compliance
- [ ] **Level A**
  - [ ] Images have alt text
  - [ ] Forms have labels
  - [ ] Page has proper headings
  - [ ] Links have descriptive text

- [ ] **Level AA**
  - [ ] Color contrast ratios meet standards
  - [ ] Text is resizable to 200%
  - [ ] Focus indicators visible
  - [ ] Error messages clear

#### Keyboard Navigation
- [ ] **Tab Order**
  - [ ] Logical tab sequence
  - [ ] Skip links present
  - [ ] Modal trap focus works
  - [ ] Escape key closes modals

#### Screen Reader Testing
- [ ] **NVDA/JAWS Testing**
  - [ ] Page structure announced
  - [ ] Form fields announced
  - [ ] Dynamic content updates announced
  - [ ] Error messages announced

## Deployment Process

### 1. Pre-Deployment Steps
```bash
# Build verification
dotnet build --configuration Release

# Run tests
dotnet test

# Bundle optimization
dotnet publish -c Release -o ./publish
```

### 2. Asset Preparation
- [ ] **CSS Minification**
  ```bash
  # Minify CSS files
  # app.css -> app.min.css
  # wcr-theme.css -> wcr-theme.min.css
  # pages.css -> pages.min.css
  ```

- [ ] **JavaScript Minification**
  ```bash
  # Minify JS files
  # app.js -> app.min.js
  ```

- [ ] **Image Optimization**
  - [ ] Compress all images
  - [ ] Generate WebP versions
  - [ ] Create responsive sizes

### 3. Configuration Updates
- [ ] Update appsettings.json for production
- [ ] Set proper API endpoints
- [ ] Configure logging levels
- [ ] Set connection strings

### 4. Deployment Execution
- [ ] Deploy to staging environment
- [ ] Run smoke tests
- [ ] Verify all components load
- [ ] Test critical user flows

## Rollback Procedures

### 1. Immediate Rollback Triggers
- [ ] **Critical Issues**
  - Site doesn't load
  - Authentication broken
  - Payment processing fails
  - Data corruption detected

### 2. Rollback Steps
```bash
# 1. Switch load balancer to previous version
# 2. Restore previous deployment
docker service update --image witchcityrope:previous web

# 3. Verify rollback successful
curl https://witchcityrope.com/health

# 4. Clear CDN cache if needed
# 5. Notify team of rollback
```

### 3. Post-Rollback Actions
- [ ] Document issue encountered
- [ ] Create hotfix branch
- [ ] Test fix in staging
- [ ] Plan re-deployment

## Post-Deployment Verification

### 1. Production Smoke Tests
- [ ] Homepage loads
- [ ] Login functionality works
- [ ] Event listing displays
- [ ] Member dashboard accessible
- [ ] Payment processing works

### 2. Monitoring Setup
- [ ] **Application Monitoring**
  - [ ] Error rates normal
  - [ ] Response times acceptable
  - [ ] Memory usage stable
  - [ ] CPU usage normal

- [ ] **User Monitoring**
  - [ ] Real User Monitoring (RUM) active
  - [ ] JavaScript errors tracked
  - [ ] Page load times monitored
  - [ ] User flows tracked

### 3. Performance Validation
- [ ] Run Lighthouse audit
- [ ] Check Core Web Vitals
- [ ] Verify CDN caching
- [ ] Test under load

## Sign-Off Checklist

### Technical Sign-Off
- [ ] Development Team Lead: _______________ Date: _______
- [ ] QA Lead: _______________ Date: _______
- [ ] Security Review: _______________ Date: _______
- [ ] DevOps Lead: _______________ Date: _______

### Business Sign-Off
- [ ] Product Owner: _______________ Date: _______
- [ ] Stakeholder Review: _______________ Date: _______

## Notes and Issues
<!-- Document any issues encountered during deployment -->

---

**Deployment Date**: _____________
**Deployed By**: _____________
**Version**: _____________