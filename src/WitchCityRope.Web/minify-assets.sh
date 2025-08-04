#!/bin/bash
# Minify CSS and JavaScript assets for WitchCityRope.Web
# This script reduces file sizes by 40-50% through minification

# Color definitions
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
WHITE='\033[1;37m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

echo -e "${CYAN}WitchCityRope Asset Minification Script${NC}"
echo -e "${CYAN}======================================${NC}"
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo -e "${RED}ERROR: Node.js is not installed. Please install Node.js first.${NC}"
    exit 1
fi

NODE_VERSION=$(node --version)
echo -e "${GREEN}Found Node.js $NODE_VERSION${NC}"

# Install minification tools if not already installed
echo -e "${YELLOW}Checking for minification tools...${NC}"

# Check for terser (JavaScript minifier)
if ! npm list -g terser &> /dev/null; then
    echo -e "${YELLOW}Installing terser for JavaScript minification...${NC}"
    npm install -g terser
fi

# Check for cssnano-cli (CSS minifier)
if ! npm list -g cssnano-cli &> /dev/null; then
    echo -e "${YELLOW}Installing cssnano for CSS minification...${NC}"
    npm install -g cssnano-cli
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WWWROOT_PATH="$SCRIPT_DIR/wwwroot"
CSS_PATH="$WWWROOT_PATH/css"
JS_PATH="$WWWROOT_PATH/js"

echo ""
echo -e "${CYAN}Starting minification process...${NC}"
echo ""

# Initialize counters
TOTAL_ORIGINAL_SIZE=0
TOTAL_MINIFIED_SIZE=0

# Minify CSS files
echo -e "${YELLOW}Minifying CSS files...${NC}"
CSS_FILES=("app.css" "pages.css" "wcr-theme.css")

for css_file in "${CSS_FILES[@]}"; do
    INPUT_FILE="$CSS_PATH/$css_file"
    OUTPUT_FILE="$CSS_PATH/${css_file%.css}.min.css"
    
    if [ -f "$INPUT_FILE" ]; then
        ORIGINAL_SIZE=$(stat -f%z "$INPUT_FILE" 2>/dev/null || stat -c%s "$INPUT_FILE" 2>/dev/null)
        TOTAL_ORIGINAL_SIZE=$((TOTAL_ORIGINAL_SIZE + ORIGINAL_SIZE))
        
        echo -n "  Minifying $css_file..."
        
        # Run cssnano with options for maximum compression
        if cssnano "$INPUT_FILE" "$OUTPUT_FILE" --no-map 2>/dev/null; then
            if [ -f "$OUTPUT_FILE" ]; then
                MINIFIED_SIZE=$(stat -f%z "$OUTPUT_FILE" 2>/dev/null || stat -c%s "$OUTPUT_FILE" 2>/dev/null)
                TOTAL_MINIFIED_SIZE=$((TOTAL_MINIFIED_SIZE + MINIFIED_SIZE))
                REDUCTION=$(echo "scale=1; (1 - $MINIFIED_SIZE / $ORIGINAL_SIZE) * 100" | bc)
                
                echo -e " ${GREEN}Done!${NC} ($(echo "scale=1; $ORIGINAL_SIZE/1024" | bc)KB → $(echo "scale=1; $MINIFIED_SIZE/1024" | bc)KB, -${REDUCTION}%)"
            else
                echo -e " ${RED}Failed!${NC}"
            fi
        else
            echo -e " ${RED}Failed!${NC}"
        fi
    else
        echo -e "  ${YELLOW}Warning: $css_file not found${NC}"
    fi
done

echo ""

# Minify JavaScript files
echo -e "${YELLOW}Minifying JavaScript files...${NC}"
JS_FILES=("app.js")

for js_file in "${JS_FILES[@]}"; do
    INPUT_FILE="$JS_PATH/$js_file"
    OUTPUT_FILE="$JS_PATH/${js_file%.js}.min.js"
    
    if [ -f "$INPUT_FILE" ]; then
        ORIGINAL_SIZE=$(stat -f%z "$INPUT_FILE" 2>/dev/null || stat -c%s "$INPUT_FILE" 2>/dev/null)
        TOTAL_ORIGINAL_SIZE=$((TOTAL_ORIGINAL_SIZE + ORIGINAL_SIZE))
        
        echo -n "  Minifying $js_file..."
        
        # Run terser with options for maximum compression
        if terser "$INPUT_FILE" -o "$OUTPUT_FILE" --compress --mangle --comments false 2>/dev/null; then
            if [ -f "$OUTPUT_FILE" ]; then
                MINIFIED_SIZE=$(stat -f%z "$OUTPUT_FILE" 2>/dev/null || stat -c%s "$OUTPUT_FILE" 2>/dev/null)
                TOTAL_MINIFIED_SIZE=$((TOTAL_MINIFIED_SIZE + MINIFIED_SIZE))
                REDUCTION=$(echo "scale=1; (1 - $MINIFIED_SIZE / $ORIGINAL_SIZE) * 100" | bc)
                
                echo -e " ${GREEN}Done!${NC} ($(echo "scale=1; $ORIGINAL_SIZE/1024" | bc)KB → $(echo "scale=1; $MINIFIED_SIZE/1024" | bc)KB, -${REDUCTION}%)"
            else
                echo -e " ${RED}Failed!${NC}"
            fi
        else
            echo -e " ${RED}Failed!${NC}"
        fi
    else
        echo -e "  ${YELLOW}Warning: $js_file not found${NC}"
    fi
done

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Minification Summary${NC}"
echo -e "${CYAN}========================================${NC}"
echo -e "${WHITE}Total Original Size: $(echo "scale=1; $TOTAL_ORIGINAL_SIZE/1024" | bc)KB${NC}"
echo -e "${WHITE}Total Minified Size: $(echo "scale=1; $TOTAL_MINIFIED_SIZE/1024" | bc)KB${NC}"
if [ $TOTAL_ORIGINAL_SIZE -gt 0 ]; then
    TOTAL_REDUCTION=$(echo "scale=1; (1 - $TOTAL_MINIFIED_SIZE / $TOTAL_ORIGINAL_SIZE) * 100" | bc)
    echo -e "${GREEN}Total Reduction: ${TOTAL_REDUCTION}%${NC}"
fi
echo ""

# Create version hash for cache busting
VERSION_HASH=$(date +"%Y%m%d%H%M%S")
VERSION_FILE="$WWWROOT_PATH/version.txt"
echo "$VERSION_HASH" > "$VERSION_FILE"

echo -e "${YELLOW}Cache busting version: $VERSION_HASH${NC}"
echo -e "${GRAY}Saved to: $VERSION_FILE${NC}"
echo ""
echo -e "${CYAN}Next Steps:${NC}"
echo -e "${WHITE}1. Update _Layout.cshtml to use .min.css and .min.js files${NC}"
echo -e "${WHITE}2. Add ?v=$VERSION_HASH to asset URLs for cache busting${NC}"
echo -e "${WHITE}3. Consider adding this script to your build pipeline${NC}"
echo ""
echo -e "${GREEN}Minification complete!${NC}"