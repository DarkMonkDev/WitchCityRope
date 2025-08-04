#!/bin/bash

# Fix all interactive pages missing proper render modes
# This prevents the navigation issue where URL updates but page doesn't refresh

echo "🔧 Adding render modes to interactive pages missing them..."

# HowToJoin page - has @onclick buttons
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Public/Pages/HowToJoin.razor; then
    echo "Adding render mode to HowToJoin.razor"
    sed -i '2a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Public/Pages/HowToJoin.razor
fi

# Admin Dashboard - has @onclick buttons
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor; then
    echo "Adding render mode to Dashboard.razor"
    sed -i '9a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/Dashboard.razor
fi

# Admin UserManagement - has @bind and @onclick
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/UserManagement.razor; then
    echo "Adding render mode to UserManagement.razor"
    sed -i '7a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/UserManagement.razor
fi

# Admin EventManagement - has interactive elements
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/EventManagement.razor; then
    echo "Adding render mode to EventManagement.razor"
    sed -i '10a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/EventManagement.razor
fi

# VettingStatus - has @onclick buttons
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Vetting/Pages/VettingStatus.razor; then
    echo "Adding render mode to VettingStatus.razor"
    sed -i '5a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Vetting/Pages/VettingStatus.razor
fi

# VettingApplication - has form elements
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Vetting/Pages/VettingApplication.razor; then
    echo "Adding render mode to VettingApplication.razor"
    sed -i '7a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Vetting/Pages/VettingApplication.razor
fi

# TwoFactorSetup - has interactive forms
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Auth/Pages/TwoFactorSetup.razor; then
    echo "Adding render mode to TwoFactorSetup.razor"
    sed -i '6a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Auth/Pages/TwoFactorSetup.razor
fi

# MyTickets - has interactive elements
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Members/Pages/MyTickets.razor; then
    echo "Adding render mode to MyTickets.razor"
    sed -i '6a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Members/Pages/MyTickets.razor
fi

# TwoFactorAuth - has interactive forms
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Auth/Pages/TwoFactorAuth.razor; then
    echo "Adding render mode to TwoFactorAuth.razor"
    sed -i '6a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Auth/Pages/TwoFactorAuth.razor
fi

# VettingQueue - has admin controls
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/VettingQueue.razor; then
    echo "Adding render mode to VettingQueue.razor"
    sed -i '7a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/VettingQueue.razor
fi

# FinancialReports - has filters and controls
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/FinancialReports.razor; then
    echo "Adding render mode to FinancialReports.razor"
    sed -i '8a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/FinancialReports.razor
fi

# IncidentManagement - has admin controls
if ! grep -q "@rendermode" src/WitchCityRope.Web/Features/Admin/Pages/IncidentManagement.razor; then
    echo "Adding render mode to IncidentManagement.razor"
    sed -i '8a @rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' src/WitchCityRope.Web/Features/Admin/Pages/IncidentManagement.razor
fi

echo "✅ Render mode fixes applied!"
echo "🔄 Please restart the application to see changes"