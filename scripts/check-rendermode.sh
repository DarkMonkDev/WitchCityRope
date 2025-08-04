#!/bin/bash

# Script to check for missing @rendermode directives in Blazor pages

echo "Checking for Blazor pages missing @rendermode directive..."
echo "=========================================================="
echo ""

# Find all .razor files that have @page directive but no @rendermode
echo "Pages with @page but missing @rendermode:"
echo ""

# Function to check if a file needs @rendermode
check_file() {
    local file=$1
    
    # Skip if file doesn't have @page directive
    if ! grep -q "^@page" "$file"; then
        return
    fi
    
    # Skip if file already has @rendermode
    if grep -q "@rendermode" "$file"; then
        return
    fi
    
    # Check if file has interactive elements
    local has_onclick=$(grep -c "@onclick" "$file")
    local has_form=$(grep -c "<form\|<EditForm" "$file")
    local has_bind=$(grep -c "@bind" "$file")
    local has_submit=$(grep -c "OnSubmit\|OnValidSubmit" "$file")
    
    local total=$((has_onclick + has_form + has_bind + has_submit))
    
    if [ $total -gt 0 ]; then
        echo "❌ $file"
        echo "   Interactive elements found: onclick=$has_onclick, forms=$has_form, bind=$has_bind, submit=$has_submit"
        echo ""
    else
        echo "⚠️  $file (no interactive elements detected, but may still need @rendermode)"
        echo ""
    fi
}

# Find all .razor files in src directory
while IFS= read -r file; do
    check_file "$file"
done < <(find ./src -name "*.razor" -type f ! -path "*/bin/*" ! -path "*/obj/*" ! -path "*/node_modules/*")

echo ""
echo "=========================================================="
echo "RECOMMENDATION:"
echo ""
echo "Add this line after @page directive in interactive pages:"
echo '@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())'
echo ""
echo "For static pages (no user interaction), @rendermode is optional."
echo ""