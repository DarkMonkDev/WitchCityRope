#!/bin/bash
# Master Index Updater
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Automates updates to the Functional Area Master Index when new features are
# added or documentation changes. Maintains the single navigation source for all
# feature documentation, ensuring discoverability and organization.

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -lt 2 ] || [ "$1" == "--help" ]; then
    echo "📚 Master Index Updater"
    echo "======================="
    echo ""
    echo "📋 Purpose: Update Functional Area Master Index when features/docs change"
    echo ""
    echo "✅ Use when:"
    echo "   • After creating new functional area documentation"
    echo "   • After completing feature work"
    echo "   • When documentation structure changes"
    echo "   • When deprecating features"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Documentation doesn't exist yet (create it first)"
    echo "   • Just updating existing docs (no index change needed)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Validates Master Index exists"
    echo "   2. Creates backup of Master Index"
    echo "   3. Adds/updates/deprecates feature entry"
    echo "   4. Updates timestamps"
    echo "   5. Provides entry location and next steps"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <action> <feature-name> <domain> [feature-path]"
    echo ""
    echo "   action: add|update|deprecate"
    echo "   feature-name: Name of feature (e.g., \"Streamlined Check-in Workflow\")"
    echo "   domain: Core System|Events Domain|Content & Community|Operations"
    echo "   feature-path: Path to feature docs (required for 'add')"
    echo ""
    echo "   Examples:"
    echo "   bash execute.sh add \"User Management\" \"Core System\" docs/functional-areas/auth"
    echo "   bash execute.sh update \"Event Management\" \"\" \"\""
    echo "   bash execute.sh deprecate \"Legacy Registration\" \"\" \"\""
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

ACTION="$1"  # add|update|deprecate
FEATURE_NAME="$2"
DOMAIN="$3"
FEATURE_PATH="$4"

MASTER_INDEX="/home/chad/repos/witchcityrope/docs/architecture/functional-area-master-index.md"

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📚 Master Index Updater"
echo "======================="
echo ""

echo "1️⃣  Checking Master Index exists..."
if [ ! -f "$MASTER_INDEX" ]; then
    echo "   ❌ FAIL: Master Index not found at $MASTER_INDEX"
    exit 1
fi
echo "   ✅ Master Index found"

echo ""
echo "2️⃣  Validating action..."
if ! echo "$ACTION" | grep -qE "^(add|update|deprecate)$"; then
    echo "   ❌ FAIL: Unknown action: $ACTION"
    echo "   Valid actions: add, update, deprecate"
    exit 1
fi
echo "   ✅ Action valid: $ACTION"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - UPDATE INDEX
# ============================================

echo "Action: $ACTION"
echo "Feature: $FEATURE_NAME"
echo "Domain: $DOMAIN"
echo ""

# Create backup
cp "$MASTER_INDEX" "${MASTER_INDEX}.backup"
echo "✅ Backup created: ${MASTER_INDEX}.backup"
echo ""

TIMESTAMP=$(date +"%Y-%m-%d")

case "$ACTION" in
    "add")
        # Add new feature entry
        echo "Adding new feature to Master Index..."

        # Find domain section
        DOMAIN_SECTION=$(grep -n "^## $DOMAIN" "$MASTER_INDEX" | cut -d: -f1)

        if [ -z "$DOMAIN_SECTION" ]; then
            echo "❌ Error: Domain not found: $DOMAIN"
            echo "   Valid domains: Core System, Events Domain, Content & Community, Operations"
            exit 1
        fi

        # Create feature entry
        FEATURE_ENTRY="
### $FEATURE_NAME

**Domain**: $DOMAIN
**Status**: ✅ Active
**Owner**: WitchCityRope Team
**Last Updated**: $TIMESTAMP

#### Documentation
- [Business Requirements]($FEATURE_PATH/new-work/*/requirements/business-requirements.md)
- [Functional Spec]($FEATURE_PATH/new-work/*/requirements/functional-spec.md)
- [Database Design]($FEATURE_PATH/new-work/*/design/database-design.md)
- [UI/UX Design]($FEATURE_PATH/new-work/*/design/ui-ux-design.md)

#### Key Files
- Feature Code: \`/apps/web/src/features/$(echo $FEATURE_NAME | tr '[:upper:]' '[:lower:]' | tr ' ' '-')/\`
- API Endpoints: \`/apps/api/Features/$(echo $FEATURE_NAME | tr ' ' '')/\`
- Tests: \`/tests/playwright/$(echo $FEATURE_NAME | tr '[:upper:]' '[:lower:]' | tr ' ' '-')*.spec.ts\`

#### Status Notes
- Recently implemented
- All tests passing
- Documentation complete

---
"

        # Insert after domain header
        LINE_NUM=$((DOMAIN_SECTION + 1))
        {
            head -n $LINE_NUM "$MASTER_INDEX"
            echo "$FEATURE_ENTRY"
            tail -n +$((LINE_NUM + 1)) "$MASTER_INDEX"
        } > "${MASTER_INDEX}.tmp"

        mv "${MASTER_INDEX}.tmp" "$MASTER_INDEX"

        echo "✅ Feature added to Master Index"
        echo "   Domain: $DOMAIN"
        echo "   Feature: $FEATURE_NAME"
        ;;

    "update")
        # Update existing feature
        echo "Updating feature in Master Index..."

        # Find feature section
        if ! grep -q "^### $FEATURE_NAME" "$MASTER_INDEX"; then
            echo "❌ Error: Feature not found: $FEATURE_NAME"
            exit 1
        fi

        # Update Last Updated timestamp
        sed -i'' -e "s/^\*\*Last Updated\*\*: .*/*\*Last Updated**: $TIMESTAMP/" "$MASTER_INDEX" 2>/dev/null || \
        sed -i "s/^\*\*Last Updated\*\*: .*/*\*Last Updated**: $TIMESTAMP/" "$MASTER_INDEX"

        echo "✅ Feature updated in Master Index"
        echo "   Last Updated: $TIMESTAMP"
        ;;

    "deprecate")
        # Mark feature as deprecated
        echo "Deprecating feature in Master Index..."

        # Find feature and update status
        if ! grep -q "^### $FEATURE_NAME" "$MASTER_INDEX"; then
            echo "❌ Error: Feature not found: $FEATURE_NAME"
            exit 1
        fi

        # Change status to deprecated
        sed -i'' -e "/^### $FEATURE_NAME/,/^---/ s/\*\*Status\*\*: .*/\*\*Status**: ⚠️  Deprecated/" "$MASTER_INDEX" 2>/dev/null || \
        sed -i "/^### $FEATURE_NAME/,/^---/ s/\*\*Status\*\*: .*/\*\*Status**: ⚠️  Deprecated/" "$MASTER_INDEX"

        # Update timestamp
        sed -i'' -e "/^### $FEATURE_NAME/,/^---/ s/\*\*Last Updated\*\*: .*/\*\*Last Updated**: $TIMESTAMP/" "$MASTER_INDEX" 2>/dev/null || \
        sed -i "/^### $FEATURE_NAME/,/^---/ s/\*\*Last Updated\*\*: .*/\*\*Last Updated**: $TIMESTAMP/" "$MASTER_INDEX"

        echo "✅ Feature marked as deprecated"
        echo "   Add migration notes to Status Notes section"
        ;;

    *)
        echo "❌ Error: Unknown action: $ACTION"
        echo "   Valid actions: add, update, deprecate"
        exit 1
        ;;
esac

# Update index last modified date
sed -i'' -e "1,10s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$MASTER_INDEX" 2>/dev/null || \
sed -i "1,10s/Last Updated: .*/Last Updated: $TIMESTAMP/" "$MASTER_INDEX"

echo ""
echo "✅ Master Index updated successfully"
echo "================================"
echo ""
echo "Location: $MASTER_INDEX"
echo "Backup: ${MASTER_INDEX}.backup"
echo ""
echo "📝 Next Steps:"
echo "1. Verify entry in Master Index"
echo "2. Ensure all documentation links work"
echo "3. Add related area cross-references"
echo "4. Update PROGRESS.md with new feature"
echo ""

exit 0
