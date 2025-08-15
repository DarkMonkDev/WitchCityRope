# Register Page Approach Decision

## Date: January 11, 2025
## Decision: Keep as Separate Page

## Analysis

### Original Design (Tabbed Approach)
The original wireframe (auth-login-register-visual.html) shows:
- Single card with tabs for "Sign In" and "Create Account"
- Tab navigation changes the form content within the same card
- Unified experience with seamless switching

### Current Implementation (Separate Pages)
- Login page has tabs, but "Create Account" tab is a link to separate page
- Register has its own dedicated page at `/Identity/Account/Register`
- Each page has its own URL and can be bookmarked

## Recommendation: Keep Separate Pages

### Reasons

#### 1. **Better User Experience**
- **Clear Intent**: Separate URLs clearly indicate user intent (login vs register)
- **Bookmarkable**: Users can bookmark registration page directly
- **Back Button**: Browser back button works naturally
- **Deep Linking**: Can link directly to registration from emails/campaigns
- **Less Confusion**: No accidental form switches while typing

#### 2. **Technical Benefits**
- **Simpler State Management**: Each page manages its own state
- **Better SEO**: Separate pages with distinct meta tags
- **Cleaner Code**: Single responsibility - one page, one purpose
- **Easier Testing**: Can test each flow independently
- **Progressive Enhancement**: Can optimize each page separately

#### 3. **Security & Validation**
- **Different Requirements**: Registration has more fields and validations
- **CAPTCHA Integration**: Easier to add CAPTCHA only to registration
- **Rate Limiting**: Can apply different rate limits to each endpoint
- **Analytics**: Better tracking of conversion funnels

#### 4. **Modern UX Patterns**
- Most modern apps use separate pages (GitHub, Google, Microsoft)
- Users expect registration to be a dedicated experience
- Tabbed auth forms are becoming less common

### UX Improvements for Login Page

Since we're keeping separate pages, we need to update the Login page UX:

1. **Replace Tab with Prominent Link**
   - Remove inactive "Create Account" tab
   - Add clear "Create Account" button/link
   - Position it prominently but secondary to login

2. **Visual Hierarchy**
   ```
   [Login Form]
   
   ─────── OR ───────
   
   New to Witch City Rope?
   [Create Account] (button)
   ```

3. **Consistent Branding**
   - Both pages use same header design
   - Same color scheme and styling
   - Smooth visual transition between pages

## Implementation Plan

### Phase 1: Update Login Page UX
1. Remove tab navigation
2. Add "New to Witch City Rope?" section
3. Style "Create Account" as secondary CTA
4. Ensure smooth navigation to Register page

### Phase 2: Create Register.razor Component
1. Copy Login.razor as starting template
2. Update to use registration model
3. Add all registration fields
4. Implement scene name validation
5. Add age confirmation checkbox
6. Add terms acceptance checkbox

### Phase 3: Integration
1. Update navigation links
2. Test full flow
3. Add Puppeteer tests
4. Document patterns

## Conclusion

While the original design used tabs, separate pages provide better UX, technical benefits, and align with modern patterns. The key is ensuring smooth navigation between pages and maintaining visual consistency.