#!/bin/bash

# Blazor Architecture Validation Script
# This script checks for common architectural mistakes in Blazor components

echo "🔍 Validating Blazor Architecture..."
echo "================================="

ERRORS=0

# Check for CSS escaping issues
echo "1. Checking CSS escaping..."
CSS_ISSUES=$(grep -r "@media\|@keyframes" src/WitchCityRope.Web/**/*.razor 2>/dev/null | grep -v "@@media\|@@keyframes" || true)
if [ ! -z "$CSS_ISSUES" ]; then
    echo "❌ ERROR: Found CSS rules with single @ (should be @@):"
    echo "$CSS_ISSUES"
    echo "   Fix: Replace @media with @@media and @keyframes with @@keyframes"
    ((ERRORS++))
else
    echo "✅ CSS escaping looks good"
fi

# Check for Razor Pages patterns in Blazor components
echo "2. Checking for Razor Pages patterns..."
RAZOR_PAGES_ISSUES=$(grep -r "@model\|@{" src/WitchCityRope.Web/**/*.razor 2>/dev/null || true)
if [ ! -z "$RAZOR_PAGES_ISSUES" ]; then
    echo "❌ ERROR: Found Razor Pages patterns in Blazor components:"
    echo "$RAZOR_PAGES_ISSUES"
    echo "   Fix: Remove @model and @{} blocks, use @code{} instead"
    ((ERRORS++))
else
    echo "✅ No Razor Pages patterns found"
fi

# Check for missing [Authorize] attributes on member pages
echo "3. Checking member pages for [Authorize] attributes..."
MEMBER_PAGES=$(find src/WitchCityRope.Web/Features/Members -name "*.razor" 2>/dev/null || true)
if [ ! -z "$MEMBER_PAGES" ]; then
    for page in $MEMBER_PAGES; do
        if ! grep -q "@attribute \[Authorize\]" "$page"; then
            echo "⚠️  WARNING: $page missing [Authorize] attribute"
        fi
    done
fi

# Check for missing render modes on interactive pages
echo "4. Checking for render modes on interactive pages..."
INTERACTIVE_PATTERNS="EditForm\|@onclick\|@onchange\|@bind"
INTERACTIVE_PAGES=$(grep -l "$INTERACTIVE_PATTERNS" src/WitchCityRope.Web/**/*.razor 2>/dev/null || true)
if [ ! -z "$INTERACTIVE_PAGES" ]; then
    for page in $INTERACTIVE_PAGES; do
        if ! grep -q "@rendermode" "$page"; then
            echo "⚠️  WARNING: $page has interactive elements but no render mode"
        fi
    done
fi

# Check for .cshtml files in Features directory (should be .razor)
echo "5. Checking for .cshtml files in Features directory..."
CSHTML_FILES=$(find src/WitchCityRope.Web/Features -name "*.cshtml" 2>/dev/null || true)
if [ ! -z "$CSHTML_FILES" ]; then
    echo "❌ ERROR: Found .cshtml files in Features directory:"
    echo "$CSHTML_FILES"
    echo "   Fix: These should be .razor files for Blazor components"
    ((ERRORS++))
else
    echo "✅ No .cshtml files found in Features directory"
fi

# Check for proper service injection patterns
echo "6. Checking service injection patterns..."
BAD_INJECTION=$(grep -r "GetService\|GetRequiredService" src/WitchCityRope.Web/**/*.razor 2>/dev/null || true)
if [ ! -z "$BAD_INJECTION" ]; then
    echo "⚠️  WARNING: Found manual service resolution (should use @inject):"
    echo "$BAD_INJECTION"
    echo "   Fix: Use @inject ServiceType ServiceName instead"
fi

# Check for manual authorization checks
echo "7. Checking for manual authorization checks..."
MANUAL_AUTH=$(grep -r "GetCurrentUserAsync\|IsAuthenticated" src/WitchCityRope.Web/**/*.razor 2>/dev/null | grep -v "@inject\|@using" || true)
if [ ! -z "$MANUAL_AUTH" ]; then
    echo "⚠️  WARNING: Found manual authorization checks:"
    echo "$MANUAL_AUTH"
    echo "   Fix: Use [Authorize] attribute and AuthorizeView components"
fi

# Summary
echo "================================="
if [ $ERRORS -eq 0 ]; then
    echo "✅ All architecture checks passed!"
else
    echo "❌ Found $ERRORS critical issues that need to be fixed"
    exit 1
fi

echo "🔍 Validation complete"