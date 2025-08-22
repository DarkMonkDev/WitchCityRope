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

# 3. Check for root directory pollution
echo "📄 Checking for root directory pollution..."
APPROVED_ROOT_FILES=(
    "00-START-HERE.md"
    "ARCHITECTURE.md"
    "CLAUDE.md"
    "PROGRESS.md"
    "QUICK_START.md"
    "ROADMAP.md"
)

POLLUTING_FILES=()
for file in "$DOCS_ROOT"/*.md; do
    if [ -f "$file" ]; then
        filename=$(basename "$file")
        is_approved=false
        for approved in "${APPROVED_ROOT_FILES[@]}"; do
            if [ "$filename" = "$approved" ]; then
                is_approved=true
                break
            fi
        done
        if [ "$is_approved" = false ]; then
            POLLUTING_FILES+=("$filename")
        fi
    fi
done

if [ ${#POLLUTING_FILES[@]} -gt 0 ]; then
    echo "🚨 ERROR: Root directory pollution detected!"
    printf '%s\n' "${POLLUTING_FILES[@]}"
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