#!/bin/bash

# Fix newly created pages to follow Blazor Server architecture
echo "🔧 Fixing newly created pages..."

# Add interactive render mode to pages with interactive elements
INTERACTIVE_PAGES=(
    "src/WitchCityRope.Web/Features/Members/Pages/MemberProfile.razor"
    "src/WitchCityRope.Web/Features/Members/Pages/Tickets.razor"
    "src/WitchCityRope.Web/Features/Members/Pages/PresenterResources.razor"
    "src/WitchCityRope.Web/Features/Members/Pages/VettedEvents.razor"
)

for page in "${INTERACTIVE_PAGES[@]}"; do
    if [ -f "$page" ]; then
        echo "📝 Adding interactive render mode to $page"
        # Add render mode after @page directive
        sed -i '1a@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())' "$page"
    fi
done

# Fix CSS media queries in all new pages
echo "🎨 Fixing CSS media queries..."
find src/WitchCityRope.Web/Features/Members/Pages src/WitchCityRope.Web/Features/Public/Pages -name "*.razor" -type f | while read -r file; do
    echo "  Fixing $file"
    sed -i 's/@media/@@media/g' "$file"
    sed -i 's/@keyframes/@@keyframes/g' "$file"
done

# Fix string escaping in onclick handlers
echo "🔧 Fixing onclick handlers..."
find src/WitchCityRope.Web/Features/Members/Pages src/WitchCityRope.Web/Features/Public/Pages -name "*.razor" -type f | while read -r file; do
    echo "  Fixing onclick handlers in $file"
    # Fix patterns like @onclick="() => Method("param")" to @onclick='() => Method("param")'
    sed -i 's/@onclick="() => \([^(]*\)(\\"([^"]*)\\")"/@onclick='\''() => \1("\2")'\''/' "$file"
done

echo "✅ All pages fixed!"