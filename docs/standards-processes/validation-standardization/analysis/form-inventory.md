# Form Inventory - WitchCityRope Application

## Summary Statistics
- **Total Forms**: 17
- **jQuery Validation**: 8 forms (47%)
- **Blazor Validation**: 6 forms (35%)
- **No Validation**: 3 forms (18%)

## Forms by Validation Type

### jQuery Validation (ASP.NET Core Identity)
1. `/Areas/Identity/Pages/Account/Login.cshtml` - User login
2. `/Areas/Identity/Pages/Account/Register.cshtml` - New user registration
3. `/Areas/Identity/Pages/Account/ForgotPassword.cshtml` - Password reset request
4. `/Areas/Identity/Pages/Account/ResetPassword.cshtml` - Set new password
5. `/Areas/Identity/Pages/Account/Manage/ChangePassword.cshtml` - Change password
6. `/Areas/Identity/Pages/Account/Manage/Email.cshtml` - Update email
7. `/Areas/Identity/Pages/Account/Manage/Index.cshtml` - Update profile
8. `/Areas/Identity/Pages/Account/Logout.cshtml` - Sign out (no validation)

### Blazor Validation (DataAnnotationsValidator)
1. `/Features/Admin/Pages/EventEdit.razor` - Create/edit events
2. `/Features/Members/Components/EventRegistrationModal.razor` - Event registration/RSVP
3. `/Features/Vetting/Pages/VettingApplication.razor` - Community membership application
4. `/Features/Members/Pages/Profile.razor` - Member profile editing
5. `/Features/Admin/Pages/UserManagement.razor` - Admin user management
6. `/Features/Admin/Pages/IncidentManagement.razor` - Incident reporting

### No Validation
1. `/Pages/Shared/_LoginPartial.cshtml` - Navigation only
2. `MainLayout.razor` - Layout with search
3. `PublicLayout.razor` - Layout structure

## Key Findings
1. **Clear Technology Split**: Identity pages use traditional ASP.NET Core with jQuery, while all feature pages use Blazor
2. **Inconsistent Experience**: Users experience different validation behaviors when moving between Identity pages and the main application
3. **Syncfusion Integration**: Blazor forms heavily use Syncfusion components, which have their own validation integration
4. **Complex Forms**: Some forms (EventEdit, VettingApplication) have multi-step or tabbed interfaces with complex validation requirements