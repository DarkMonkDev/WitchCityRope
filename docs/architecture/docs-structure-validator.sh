#!/bin/bash
# CRITICAL: Documentation Structure Validator
# MANDATORY: Run this on every session start
# PURPOSE: Prevent /docs/docs/ disasters and structure violations

set -e

DOCS_ROOT="/home/chad/repos/witchcityrope-react/docs"
ERRORS_FOUND=0

echo "🔍 CRITICAL: Validating documentation structure..."

# 1. Check for multiple archive folders (CRITICAL VIOLATION)
echo "📁 Checking for duplicate archive folders..."
ARCHIVE_FOLDERS=$(find "$DOCS_ROOT" -maxdepth 1 -name "*archive*" -type d | wc -l)
if [ "$ARCHIVE_FOLDERS" -gt 1 ]; then
    echo "🚨 CRITICAL ERROR: Multiple archive folders detected!"
    find "$DOCS_ROOT" -maxdepth 1 -name "*archive*" -type d
    ERRORS_FOUND=1
fi

# 2. Check for /docs/docs/ folder (CATASTROPHIC VIOLATION)
if [ -d "$DOCS_ROOT/docs" ]; then
    echo "🚨 CATASTROPHIC ERROR: /docs/docs/ folder detected!"
    echo "This is the worst-case scenario - immediate fix required!"
    ERRORS_FOUND=1
fi

# 3. Check for CANONICAL LOCATION VIOLATIONS (CRITICAL)
echo "📄 Checking for canonical location violations..."
# ZERO FILES ALLOWED IN /docs/ ROOT (As of 2025-08-22)
APPROVED_DOCS_ROOT_FILES=()
# 00-START-HERE.md was ARCHIVED to /docs/_archive/00-start-here-legacy-2025-08-22/

# FORBIDDEN IN /docs/ ROOT - MUST BE IN PROJECT ROOT /
FORBIDDEN_IN_DOCS=(
    "ARCHITECTURE.md"
    "CLAUDE.md"
    "PROGRESS.md"
    "ROADMAP.md"
    "README.md"
    "SECURITY.md"
    "QUICK_START.md"
)

VIOLATION_FILES=()
# Check for forbidden files in /docs/ root
for file in "$DOCS_ROOT"/*.md; do
    if [ -f "$file" ]; then
        filename=$(basename "$file")
        is_approved=false
        for approved in "${APPROVED_DOCS_ROOT_FILES[@]}"; do
            if [ "$filename" = "$approved" ]; then
                is_approved=true
                break
            fi
        done
        
        # Check if it's a forbidden file
        is_forbidden=false
        for forbidden in "${FORBIDDEN_IN_DOCS[@]}"; do
            if [ "$filename" = "$forbidden" ]; then
                is_forbidden=true
                echo "🚨 CANONICAL LOCATION VIOLATION: $filename found in /docs/ but should be in project root /"
                ERRORS_FOUND=1
                break
            fi
        done
        
        if [ "$is_approved" = false ] && [ "$is_forbidden" = false ]; then
            VIOLATION_FILES+=("$filename")
        fi
    fi
done

if [ ${#VIOLATION_FILES[@]} -gt 0 ]; then
    echo "🚨 ERROR: Unauthorized files in /docs/ root:"
    printf '  - %s\n' "${VIOLATION_FILES[@]}"
    echo "Only 00-START-HERE.md is allowed in /docs/ root!"
    ERRORS_FOUND=1
fi

# 3.1 Check that required files ARE in project root
echo "📄 Checking canonical files are in project root..."
PROJECT_ROOT="/home/chad/repos/witchcityrope-react"
MISSING_FROM_ROOT=()
for required_file in "${FORBIDDEN_IN_DOCS[@]}"; do
    if [ "$required_file" != "QUICK_START.md" ]; then  # QUICK_START should be in guides-setup
        if [ ! -f "$PROJECT_ROOT/$required_file" ]; then
            MISSING_FROM_ROOT+=("$required_file")
        fi
    fi
done

if [ ${#MISSING_FROM_ROOT[@]} -gt 0 ]; then
    echo "🚨 ERROR: Required files missing from project root:"
    printf '  - %s\n' "${MISSING_FROM_ROOT[@]}"
    echo "These files must exist in project root / not in /docs/!"
    ERRORS_FOUND=1
fi

# 4. Check for functional area duplicates
echo "🔄 Checking for functional area duplicates..."
FUNCTIONAL_AREAS=("security" "user-guide" "authentication" "database" "deployment")
for area in "${FUNCTIONAL_AREAS[@]}"; do
    root_path="$DOCS_ROOT/$area"
    functional_path="$DOCS_ROOT/functional-areas/$area"
    guides_path="$DOCS_ROOT/guides-setup/$area"
    
    count=0
    [ -d "$root_path" ] && count=$((count + 1))
    [ -d "$functional_path" ] && count=$((count + 1))
    [ -d "$guides_path" ] && count=$((count + 1))
    
    if [ $count -gt 1 ]; then
        echo "🚨 ERROR: Duplicate $area folders detected!"
        [ -d "$root_path" ] && echo "  - $root_path"
        [ -d "$functional_path" ] && echo "  - $functional_path"  
        [ -d "$guides_path" ] && echo "  - $guides_path"
        ERRORS_FOUND=1
    fi
done

# 5. Check approved folder structure
echo "📂 Validating approved folder structure..."
APPROVED_FOLDERS=(
    "architecture"
    "_archive"
    "design"
    "functional-areas"
    "guides-setup"
    "history"
    "lessons-learned"
    "standards-processes"
)

for folder in "$DOCS_ROOT"/*/; do
    if [ -d "$folder" ]; then
        folder_name=$(basename "$folder")
        is_approved=false
        for approved in "${APPROVED_FOLDERS[@]}"; do
            if [ "$folder_name" = "$approved" ]; then
                is_approved=true
                break
            fi
        done
        if [ "$is_approved" = false ]; then
            echo "🚨 ERROR: Unauthorized folder in docs root: $folder_name"
            ERRORS_FOUND=1
        fi
    fi
done

# 6. Check for agent shortcut patterns (common violations)
echo "🚨 Checking for common agent violation patterns..."
VIOLATION_PATTERNS=(
    "implementation.md"
    "requirements.md"
    "design.md"
    "status.md"
    "progress.md"
    "notes.md"
)

for pattern in "${VIOLATION_PATTERNS[@]}"; do
    if [ -f "$DOCS_ROOT/$pattern" ]; then
        echo "🚨 ERROR: Generic file in docs root: $pattern (should be in functional area)"
        ERRORS_FOUND=1
    fi
done

# 7. Check for temporary session work in docs root
echo "📁 Checking for temporary session work in docs root..."
if ls "$DOCS_ROOT"/2025-* >/dev/null 2>&1; then
    echo "🚨 ERROR: Temporary session files detected in docs root!"
    ls "$DOCS_ROOT"/2025-* 2>/dev/null
    echo "These should be in /session-work/YYYY-MM-DD/"
    ERRORS_FOUND=1
fi

# Results
if [ $ERRORS_FOUND -eq 0 ]; then
    echo "✅ SUCCESS: Documentation structure is CLEAN"
    echo "   - Single archive folder: ✅"
    echo "   - No /docs/docs/: ✅"
    echo "   - Clean root directory: ✅"
    echo "   - No duplicates: ✅"
    echo "   - Approved structure: ✅"
    echo "   - No agent shortcuts: ✅"
    echo "   - No temp files in root: ✅"
    echo ""
    echo "📋 ENFORCEMENT SYSTEM OPERATIONAL"
    echo "   Agents MUST follow structure rules"
    echo "   Zero tolerance for violations"
    echo "   Run after every file operation"
else
    echo ""
    echo "🚨🚨🚨 CRITICAL DOCUMENTATION STRUCTURE VIOLATIONS DETECTED! 🚨🚨🚨"
    echo "ENFORCEMENT SYSTEM TRIGGERED - IMMEDIATE ACTION REQUIRED!"
    echo ""
    echo "📢 VIOLATION RESPONSE PROTOCOL:"
    echo "   1. STOP all current work immediately"
    echo "   2. Contact librarian agent for emergency cleanup"
    echo "   3. Do NOT create any more files until fixed"
    echo "   4. Review agent training on structure rules"
    echo ""
    echo "🏆 ZERO TOLERANCE POLICY IN EFFECT"
    exit 1
fi