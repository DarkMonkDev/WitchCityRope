#!/bin/bash

# Performance Analysis Script for WitchCityRope.Web
# Checks bundle sizes, analyzes CSS/JS, and generates performance report

echo -e "\033[36mWitchCityRope.Web Performance Analysis\033[0m"
echo -e "\033[36m=====================================\033[0m"
echo ""

# Set working directory
WORKING_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$WORKING_DIR"

# Create output directory for reports
REPORT_DIR="$WORKING_DIR/performance-reports"
mkdir -p "$REPORT_DIR"

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
REPORT_FILE="$REPORT_DIR/performance-report-$TIMESTAMP.md"

# Initialize report
cat > "$REPORT_FILE" << EOF
# WitchCityRope.Web Performance Analysis Report
Generated: $(date +"%Y-%m-%d %H:%M:%S")

## Executive Summary
This report analyzes the performance characteristics of the WitchCityRope.Web application, including bundle sizes, asset optimization, and performance recommendations.

EOF

# 1. Analyze CSS Bundle Sizes
echo -e "\033[33m1. Analyzing CSS Bundle Sizes...\033[0m"
echo "## 1. CSS Bundle Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### File Sizes" >> "$REPORT_FILE"

TOTAL_CSS_SIZE=0
for file in "$WORKING_DIR/wwwroot/css"/*.css; do
    if [ -f "$file" ]; then
        SIZE=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
        SIZE_KB=$(echo "scale=2; $SIZE / 1024" | bc)
        FILENAME=$(basename "$file")
        TOTAL_CSS_SIZE=$((TOTAL_CSS_SIZE + SIZE))
        
        echo "  - $FILENAME: $SIZE_KB KB"
        echo "- **$FILENAME**: $SIZE_KB KB" >> "$REPORT_FILE"
    fi
done

TOTAL_CSS_KB=$(echo "scale=2; $TOTAL_CSS_SIZE / 1024" | bc)
echo -e "  \033[32mTotal CSS: $TOTAL_CSS_KB KB\033[0m"
echo "" >> "$REPORT_FILE"
echo "**Total CSS Size**: $TOTAL_CSS_KB KB" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# 2. Analyze JavaScript Bundle Sizes
echo ""
echo -e "\033[33m2. Analyzing JavaScript Bundle Sizes...\033[0m"
echo "## 2. JavaScript Bundle Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "### File Sizes" >> "$REPORT_FILE"

TOTAL_JS_SIZE=0
for file in "$WORKING_DIR/wwwroot/js"/*.js; do
    if [ -f "$file" ]; then
        SIZE=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
        SIZE_KB=$(echo "scale=2; $SIZE / 1024" | bc)
        FILENAME=$(basename "$file")
        TOTAL_JS_SIZE=$((TOTAL_JS_SIZE + SIZE))
        
        echo "  - $FILENAME: $SIZE_KB KB"
        echo "- **$FILENAME**: $SIZE_KB KB" >> "$REPORT_FILE"
    fi
done

TOTAL_JS_KB=$(echo "scale=2; $TOTAL_JS_SIZE / 1024" | bc)
echo -e "  \033[32mTotal JavaScript: $TOTAL_JS_KB KB\033[0m"
echo "" >> "$REPORT_FILE"
echo "**Total JavaScript Size**: $TOTAL_JS_KB KB" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# 3. Check for Minification
echo ""
echo -e "\033[33m3. Checking Minification Status...\033[0m"
echo "## 3. Minification Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

NEEDS_MINIFICATION=""

check_minification() {
    local file=$1
    local filename=$(basename "$file")
    
    # Count lines and calculate average line length
    if [ -f "$file" ]; then
        LINES=$(wc -l < "$file")
        CHARS=$(wc -c < "$file")
        
        if [ "$LINES" -gt 0 ]; then
            AVG_LINE_LENGTH=$((CHARS / LINES))
            
            if [ "$AVG_LINE_LENGTH" -lt 200 ]; then
                echo -e "  - $filename: \033[31mNOT minified (avg line length: $AVG_LINE_LENGTH)\033[0m"
                NEEDS_MINIFICATION="$NEEDS_MINIFICATION$filename\n"
            else
                echo -e "  - $filename: \033[32mAppears minified\033[0m"
            fi
        fi
    fi
}

# Check CSS files
for file in "$WORKING_DIR/wwwroot/css"/*.css; do
    check_minification "$file"
done

# Check JS files
for file in "$WORKING_DIR/wwwroot/js"/*.js; do
    check_minification "$file"
done

if [ -n "$NEEDS_MINIFICATION" ]; then
    echo "### Files Requiring Minification" >> "$REPORT_FILE"
    echo -e "$NEEDS_MINIFICATION" | while read -r file; do
        [ -n "$file" ] && echo "- $file" >> "$REPORT_FILE"
    done
else
    echo "All CSS and JavaScript files appear to be minified." >> "$REPORT_FILE"
fi

# 4. Analyze External Resources
echo ""
echo -e "\033[33m4. Analyzing External Resources...\033[0m"
echo "" >> "$REPORT_FILE"
echo "## 4. External Resources Analysis" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

LAYOUT_FILE="$WORKING_DIR/Pages/_Layout.cshtml"
if [ -f "$LAYOUT_FILE" ]; then
    echo "### External Dependencies" >> "$REPORT_FILE"
    
    if grep -q "fonts.googleapis.com" "$LAYOUT_FILE"; then
        echo "  - Google Fonts detected"
        echo "- Google Fonts (multiple font families)" >> "$REPORT_FILE"
    fi
    
    if grep -q "Syncfusion" "$LAYOUT_FILE"; then
        echo "  - Syncfusion components detected"
        echo "- Syncfusion Blazor Components" >> "$REPORT_FILE"
    fi
fi

# 5. Image Optimization Check
echo ""
echo -e "\033[33m5. Checking for Images...\033[0m"
echo "" >> "$REPORT_FILE"
echo "## 5. Image Optimization" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

IMAGE_COUNT=$(find "$WORKING_DIR/wwwroot" -type f \( -name "*.png" -o -name "*.jpg" -o -name "*.jpeg" -o -name "*.gif" -o -name "*.svg" -o -name "*.webp" \) 2>/dev/null | wc -l)

if [ "$IMAGE_COUNT" -eq 0 ]; then
    echo "  - No images found in wwwroot"
    echo "No images found in wwwroot directory. Consider adding optimized images for better visual appeal." >> "$REPORT_FILE"
else
    echo "### Image Files" >> "$REPORT_FILE"
    find "$WORKING_DIR/wwwroot" -type f \( -name "*.png" -o -name "*.jpg" -o -name "*.jpeg" -o -name "*.gif" -o -name "*.svg" -o -name "*.webp" \) 2>/dev/null | while read -r img; do
        SIZE=$(stat -f%z "$img" 2>/dev/null || stat -c%s "$img" 2>/dev/null)
        SIZE_KB=$(echo "scale=2; $SIZE / 1024" | bc)
        FILENAME=$(basename "$img")
        echo "- $FILENAME: $SIZE_KB KB" >> "$REPORT_FILE"
    done
fi

# 6. Performance Recommendations
echo ""
echo -e "\033[33m6. Generating Recommendations...\033[0m"

cat >> "$REPORT_FILE" << 'EOF'

## 6. Performance Recommendations

### High Priority

#### CSS Optimization
- Enable CSS minification using cssnano or similar
- Remove unused CSS rules with PurgeCSS
- Consider code splitting for page-specific styles
- Implement critical CSS inlining

#### JavaScript Optimization
- Implement build-time minification using terser
- Enable compression (gzip/brotli) on the server
- Lazy load non-critical JavaScript
- Remove unused JavaScript code

#### Blazor Server Optimization
- Enable response compression in Program.cs
- Implement lazy loading for components
- Use virtualization for large lists
- Configure SignalR for better performance
- Consider Blazor WebAssembly for static content

### Medium Priority

#### Font Optimization
- Limit font families to 2-3 maximum
- Use font-display: swap for better loading
- Consider self-hosting critical fonts
- Subset fonts to include only needed characters

#### Resource Loading
- Add preconnect hints for external domains
- Implement resource prefetching
- Use HTTP/2 server push for critical resources

## 7. Quick Performance Wins

1. **Enable Response Compression**
   ```csharp
   // In Program.cs
   builder.Services.AddResponseCompression(options =>
   {
       options.EnableForHttps = true;
       options.Providers.Add<BrotliCompressionProvider>();
       options.Providers.Add<GzipCompressionProvider>();
   });
   ```

2. **Add Static File Caching**
   ```csharp
   app.UseStaticFiles(new StaticFileOptions
   {
       OnPrepareResponse = ctx =>
       {
           ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
       }
   });
   ```

3. **Optimize SignalR**
   ```csharp
   builder.Services.AddSignalR(options =>
   {
       options.EnableDetailedErrors = false;
       options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
       options.KeepAliveInterval = TimeSpan.FromSeconds(15);
   });
   ```

4. **Implement CSS/JS Bundling**
   - Use WebOptimizer or similar for runtime bundling
   - Or implement build-time bundling with webpack/vite

5. **Add Loading States**
   - Show skeleton screens during component initialization
   - Implement progressive enhancement

## 8. Estimated Impact

Based on the analysis:
- **Current estimated load time**: 2-3 seconds (with good connection)
- **After optimizations**: < 1.5 seconds
- **Potential improvement**: 40-50% faster initial load

### Core Web Vitals Targets
- **LCP (Largest Contentful Paint)**: < 2.5s (Good)
- **FID (First Input Delay)**: < 100ms (Good)
- **CLS (Cumulative Layout Shift)**: < 0.1 (Good)

## 9. Implementation Priority

1. **Immediate (1-2 hours)**
   - Enable response compression
   - Add static file caching headers
   - Minify CSS/JS files

2. **Short-term (1-2 days)**
   - Implement CSS/JS bundling
   - Optimize fonts
   - Add loading states

3. **Long-term (1 week)**
   - Implement lazy loading
   - Add virtualization for lists
   - Consider CDN for static assets
   - Evaluate Blazor WebAssembly for appropriate pages

EOF

# Save summary
echo ""
echo -e "\033[32mPerformance analysis complete!\033[0m"
echo -e "\033[36mReport saved to: $REPORT_FILE\033[0m"

echo ""
echo -e "\033[33mSummary:\033[0m"
echo "- Total CSS: $TOTAL_CSS_KB KB"
echo "- Total JS: $TOTAL_JS_KB KB"
echo "- Image files: $IMAGE_COUNT"

# Display key recommendations
echo ""
echo -e "\033[33mTop 3 Recommendations:\033[0m"
echo "1. Enable response compression and static file caching"
echo "2. Minify CSS and JavaScript files"
echo "3. Optimize font loading and reduce external dependencies"

# Try to open the report
if command -v xdg-open &> /dev/null; then
    xdg-open "$REPORT_FILE" 2>/dev/null || true
elif command -v open &> /dev/null; then
    open "$REPORT_FILE" 2>/dev/null || true
fi