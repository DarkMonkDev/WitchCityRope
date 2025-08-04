#!/bin/bash

# Fix all NavigationManager.NavigateTo calls to include forceLoad: true
# This prevents the render mode issue where URL updates but page doesn't refresh

echo "🔧 Fixing all navigation calls to include forceLoad: true..."

# Fix Admin pages
sed -i 's/Navigation\.NavigateTo($"\/admin\/members\/{member\.Id}")/Navigation.NavigateTo($"\/admin\/members\/{member.Id}", forceLoad: true)/g' src/WitchCityRope.Web/Pages/Admin/Members/Index.razor.cs

# Fix main navigation in layouts
sed -i 's/Navigation\.NavigateTo("\/");/Navigation.NavigateTo("\/", forceLoad: true);/g' src/WitchCityRope.Web/Shared/Components/Navigation/MainNav.razor
sed -i 's/Navigation\.NavigateTo("\/");/Navigation.NavigateTo("\/", forceLoad: true);/g' src/WitchCityRope.Web/Shared/Layouts/MainLayout.razor
sed -i 's/Navigation\.NavigateTo("\/");/Navigation.NavigateTo("\/", forceLoad: true);/g' src/WitchCityRope.Web/Shared/Layouts/AdminLayout.razor

# Fix authentication pages
sed -i 's/Navigation\.NavigateTo("\/login")/Navigation.NavigateTo("\/login", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/ManageProfile.razor
sed -i 's/Navigation\.NavigateTo("\/login")/Navigation.NavigateTo("\/login", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/ManageEmail.razor
sed -i 's/Navigation\.NavigateTo("\/login")/Navigation.NavigateTo("\/login", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/ChangePassword.razor
sed -i 's/Navigation\.NavigateTo("\/login")/Navigation.NavigateTo("\/login", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/DeletePersonalData.razor
sed -i 's/Navigation\.NavigateTo("\/login")/Navigation.NavigateTo("\/login", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/LoginWith2fa.razor

# Fix member pages
sed -i 's/Navigation\.NavigateTo("\/member\/dashboard")/Navigation.NavigateTo("\/member\/dashboard", forceLoad: true)/g' src/WitchCityRope.Web/Features/Auth/Pages/TwoFactorSetup.razor
sed -i 's/Navigation\.NavigateTo("\/member\/dashboard")/Navigation.NavigateTo("\/member\/dashboard", forceLoad: true)/g' src/WitchCityRope.Web/Features/Vetting/Pages/VettingApplication.razor

# Fix vetting pages
sed -i 's/Navigation\.NavigateTo("\/how-to-join")/Navigation.NavigateTo("\/how-to-join", forceLoad: true)/g' src/WitchCityRope.Web/Features/Vetting/Pages/VettingStatus.razor
sed -i 's/Navigation\.NavigateTo("\/vetting\/apply")/Navigation.NavigateTo("\/vetting\/apply", forceLoad: true)/g' src/WitchCityRope.Web/Features/Vetting/Pages/VettingStatus.razor

# Fix admin dashboard
sed -i 's/Navigation\.NavigateTo("\/admin\/events\/new")/Navigation.NavigateTo("\/admin\/events\/new", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor
sed -i 's/Navigation\.NavigateTo("\/admin\/members")/Navigation.NavigateTo("\/admin\/members", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor
sed -i 's/Navigation\.NavigateTo("\/admin\/reports")/Navigation.NavigateTo("\/admin\/reports", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor
sed -i 's/Navigation\.NavigateTo("\/admin\/settings")/Navigation.NavigateTo("\/admin\/settings", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor

# Fix event management
sed -i 's/Navigation\.NavigateTo($"\/events\/{eventId}")/Navigation.NavigateTo($"\/events\/{eventId}", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/EventManagement.razor
sed -i 's/Navigation\.NavigateTo($"\/admin\/events\/{eventId}\/attendees")/Navigation.NavigateTo($"\/admin\/events\/{eventId}\/attendees", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/EventManagement.razor

# Fix member tickets
sed -i 's/Navigation\.NavigateTo("\/events")/Navigation.NavigateTo("\/events", forceLoad: true)/g' src/WitchCityRope.Web/Features/Members/Pages/Tickets.razor
sed -i 's/Navigation\.NavigateTo($"\/events\/{eventId}")/Navigation.NavigateTo($"\/events\/{eventId}", forceLoad: true)/g' src/WitchCityRope.Web/Features/Members/Pages/Tickets.razor

# Fix profile pages
sed -i 's/Navigation\.NavigateTo("\/profile")/Navigation.NavigateTo("\/profile", forceLoad: true)/g' src/WitchCityRope.Web/Features/Members/Pages/MemberProfile.razor
sed -i 's/Navigation\.NavigateTo("\/dashboard")/Navigation.NavigateTo("\/dashboard", forceLoad: true)/g' src/WitchCityRope.Web/Features/Members/Pages/Profile.razor

# Fix user management
sed -i 's/Navigation\.NavigateTo($"\/admin\/users\/{userId}")/Navigation.NavigateTo($"\/admin\/users\/{userId}", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/UserManagement.razor
sed -i 's/Navigation\.NavigateTo($"\/admin\/users\/{userId}")/Navigation.NavigateTo($"\/admin\/users\/{userId}", forceLoad: true)/g' src/WitchCityRope.Web/Features/Admin/Pages/UserManagementStandardized.razor

# Fix how to join page
sed -i 's/Navigation\.NavigateTo("\/vetting\/apply")/Navigation.NavigateTo("\/vetting\/apply", forceLoad: true)/g' src/WitchCityRope.Web/Features/Public/Pages/HowToJoin.razor
sed -i 's/Navigation\.NavigateTo("\/events")/Navigation.NavigateTo("\/events", forceLoad: true)/g' src/WitchCityRope.Web/Features/Public/Pages/HowToJoin.razor

# Fix vetting applications
sed -i 's/Navigation\.NavigateTo("\/vetting\/submitted")/Navigation.NavigateTo("\/vetting\/submitted", forceLoad: true)/g' src/WitchCityRope.Web/Features/Vetting/Pages/VettingApplicationStandardized.razor

echo "✅ Navigation fixes applied!"
echo "🔄 Please restart the application to see changes"