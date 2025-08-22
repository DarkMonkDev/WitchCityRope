#!/bin/bash

# CSS Usage Analysis Script for WitchCityRope.Web
# Analyzes CSS selectors and their usage in Razor components

echo -e "\033[36mCSS Usage Analysis for WitchCityRope.Web\033[0m"
echo -e "\033[36m========================================\033[0m"
echo ""

WORKING_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$WORKING_DIR"

# Create output directory
OUTPUT_DIR="$WORKING_DIR/performance-reports"
mkdir -p "$OUTPUT_DIR"

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
REPORT_FILE="$OUTPUT_DIR/css-usage-report-$TIMESTAMP.md"

# Initialize report
cat > "$REPORT_FILE" << EOF
# CSS Usage Analysis Report
Generated: $(date +"%Y-%m-%d %H:%M:%S")

## Summary
This report analyzes CSS selectors and their usage across Razor components to identify potentially unused styles.

EOF

# Extract CSS selectors from each file
echo -e "\033[33mExtracting CSS selectors...\033[0m"
echo "## CSS Files Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Function to extract CSS selectors
extract_selectors() {
    local css_file=$1
    local filename=$(basename "$css_file")
    
    echo "### $filename" >> "$REPORT_FILE"
    echo "" >> "$REPORT_FILE"
    
    # Extract class selectors
    echo "#### Class Selectors" >> "$REPORT_FILE"
    grep -oE '\.[a-zA-Z_-][a-zA-Z0-9_-]*' "$css_file" | sort | uniq | while read -r selector; do
        class_name=${selector:1}  # Remove the dot
        
        # Search for usage in Razor files
        usage_count=$(grep -r "class=\"[^\"]*$class_name" --include="*.razor" --include="*.cshtml" "$WORKING_DIR" 2>/dev/null | wc -l)
        usage_count2=$(grep -r "class=\"[^\"]*$class_name[[:space:]]" --include="*.razor" --include="*.cshtml" "$WORKING_DIR" 2>/dev/null | wc -l)
        usage_count3=$(grep -r "@class=\"[^\"]*$class_name" --include="*.razor" --include="*.cshtml" "$WORKING_DIR" 2>/dev/null | wc -l)
        total_usage=$((usage_count + usage_count2 + usage_count3))
        
        if [ "$total_usage" -eq 0 ]; then
            echo "- **$selector** - ❌ Unused" >> "$REPORT_FILE"
        else
            echo "- $selector - ✓ Used ($total_usage times)" >> "$REPORT_FILE"
        fi
    done
    
    echo "" >> "$REPORT_FILE"
    
    # Extract ID selectors
    echo "#### ID Selectors" >> "$REPORT_FILE"
    grep -oE '#[a-zA-Z_-][a-zA-Z0-9_-]*' "$css_file" | sort | uniq | while read -r selector; do
        id_name=${selector:1}  # Remove the hash
        
        # Search for usage in Razor files
        usage_count=$(grep -r "id=\"$id_name\"" --include="*.razor" --include="*.cshtml" "$WORKING_DIR" 2>/dev/null | wc -l)
        
        if [ "$usage_count" -eq 0 ]; then
            echo "- **$selector** - ❌ Unused" >> "$REPORT_FILE"
        else
            echo "- $selector - ✓ Used ($usage_count times)" >> "$REPORT_FILE"
        fi
    done
    
    echo "" >> "$REPORT_FILE"
}

# Analyze each CSS file
for css_file in "$WORKING_DIR/wwwroot/css"/*.css; do
    if [ -f "$css_file" ]; then
        echo "Analyzing $(basename "$css_file")..."
        extract_selectors "$css_file"
    fi
done

# Find common patterns
echo "" >> "$REPORT_FILE"
echo "## Common CSS Patterns" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo "### Bootstrap-style Classes" >> "$REPORT_FILE"
grep -hE '\.(btn|col|row|container|card|form|nav|modal|alert)-[a-zA-Z0-9-]+' "$WORKING_DIR/wwwroot/css"/*.css | sed 's/.*\(\.[a-zA-Z0-9-]*\).*/\1/' | sort | uniq -c | sort -nr | head -20 | while read -r count class; do
    echo "- $class (defined $count times)" >> "$REPORT_FILE"
done

echo "" >> "$REPORT_FILE"
echo "### Custom Component Classes" >> "$REPORT_FILE"
grep -hE '\.(wcr|witch|rope|event|member|auth)-[a-zA-Z0-9-]+' "$WORKING_DIR/wwwroot/css"/*.css | sed 's/.*\(\.[a-zA-Z0-9-]*\).*/\1/' | sort | uniq | while read -r class; do
    echo "- $class" >> "$REPORT_FILE"
done

# Analyze external CSS dependencies
echo "" >> "$REPORT_FILE"
echo "## External CSS Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo "### Syncfusion Theme Size" >> "$REPORT_FILE"
echo "Syncfusion theme is loaded from: _content/Syncfusion.Blazor.Themes/bootstrap5.css" >> "$REPORT_FILE"
echo "This is a large external dependency that may include many unused styles." >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# Recommendations
cat >> "$REPORT_FILE" << 'EOF'
## Recommendations

### 1. Remove Unused CSS
Based on the analysis, consider removing the following:
- Unused class selectors marked with ❌
- Unused ID selectors marked with ❌
- Duplicate or redundant styles

### 2. Optimize CSS Architecture
- **Component-scoped styles**: Use Blazor CSS isolation for component-specific styles
- **Utility-first approach**: Consider using utility classes for common patterns
- **CSS-in-JS**: For dynamic styles, consider CSS-in-JS solutions

### 3. Build-time Optimization
```bash
# Install PurgeCSS
npm install -D @fullhuman/postcss-purgecss

# Configure PurgeCSS to scan Razor files
content: [
  './Pages/**/*.{razor,cshtml}',
  './Shared/**/*.{razor,cshtml}',  
  './Features/**/*.{razor,cshtml}'
]
```

### 4. Syncfusion Optimization
- Use only required Syncfusion components
- Consider lazy loading Syncfusion styles
- Import component-specific CSS instead of full theme

### 5. CSS Splitting Strategy
- **Critical CSS**: Inline above-the-fold styles
- **Page-specific CSS**: Load only on relevant pages
- **Component CSS**: Use Blazor CSS isolation

## Estimated Savings
- Removing unused CSS: ~20-30% reduction
- Minification: ~40-50% reduction
- Combined with gzip: ~70-80% total reduction

EOF

echo ""
echo -e "\033[32mCSS usage analysis complete!\033[0m"
echo -e "\033[36mReport saved to: $REPORT_FILE\033[0m"

# Count unused selectors
UNUSED_COUNT=$(grep -c "❌ Unused" "$REPORT_FILE" 2>/dev/null || echo "0")
echo ""
echo -e "\033[33mSummary:\033[0m"
echo "- Unused selectors found: $UNUSED_COUNT"
echo "- Check the report for detailed analysis"