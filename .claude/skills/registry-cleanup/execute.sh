#!/bin/bash
# DigitalOcean Container Registry Cleanup
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script cleans up old container images based on retention policies:
# - Staging: Keep last 10 tags
# - Production: Keep last 30 tags
# - Always preserve :latest tag
#
# Usage:
#   bash execute.sh           # Dry-run mode (default)
#   bash execute.sh --confirm # Actually delete tags

set -e  # Exit on error

# ============================================
# CONFIGURATION
# ============================================

# Default to dry-run mode
DRY_RUN=true

# Parse arguments
if [ "$1" = "--confirm" ]; then
    DRY_RUN=false
fi

# API configuration
DIGITALOCEAN_API="https://api.digitalocean.com/v2"

# Registry names
WITCHCITYROPE_REGISTRY="witchcityrope"
ACCOUNTING_REGISTRY="accounting"

# Repository retention policies (number of tags to keep)
declare -A RETENTION
RETENTION["staging-api-witchcityrope"]=10
RETENTION["staging-web-witchcityrope"]=10
RETENTION["production-api-witchcityrope"]=30
RETENTION["production-web-witchcityrope"]=30
RETENTION["accounting-api"]=30
RETENTION["accounting-web"]=30

# Repository to registry mapping
declare -A REGISTRY_MAP
REGISTRY_MAP["staging-api-witchcityrope"]="$WITCHCITYROPE_REGISTRY"
REGISTRY_MAP["staging-web-witchcityrope"]="$WITCHCITYROPE_REGISTRY"
REGISTRY_MAP["production-api-witchcityrope"]="$WITCHCITYROPE_REGISTRY"
REGISTRY_MAP["production-web-witchcityrope"]="$WITCHCITYROPE_REGISTRY"
REGISTRY_MAP["accounting-api"]="$ACCOUNTING_REGISTRY"
REGISTRY_MAP["accounting-web"]="$ACCOUNTING_REGISTRY"

# ============================================
# DISPLAY HEADER
# ============================================

if [ "$DRY_RUN" = true ]; then
    echo "🧹 Registry Cleanup - DRY RUN MODE"
    echo "==================================="
    echo ""
    echo "ℹ️  No changes will be made - this is a preview only"
    echo ""
else
    echo "🧹 Registry Cleanup - CONFIRM MODE"
    echo "==================================="
    echo ""
    echo "⚠️  WARNING: This will DELETE old tags permanently!"
    echo ""
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🔍 Running prerequisite checks..."
echo ""

# Check 1: API token from user secrets
echo "1️⃣  Retrieving DigitalOcean API token..."
API_DIR="/home/chad/repos/witchcityrope/apps/api"

if [ ! -d "$API_DIR" ]; then
    echo "   ❌ FAIL: API directory not found: $API_DIR"
    exit 1
fi

TOKEN=$(cd "$API_DIR" && dotnet user-secrets list 2>/dev/null | grep "DigitalOcean:Token" | cut -d'=' -f2 | tr -d ' ')

if [ -z "$TOKEN" ]; then
    echo "   ❌ FAIL: DigitalOcean API token not found in user secrets"
    echo ""
    echo "💡 Set token in user secrets:"
    echo "   cd $API_DIR"
    echo "   dotnet user-secrets set \"DigitalOcean:Token\" \"YOUR_TOKEN_HERE\""
    exit 1
fi

echo "   ✅ API token retrieved"

# Check 2: Required tools
echo ""
echo "2️⃣  Checking required tools..."

if ! command -v curl &> /dev/null; then
    echo "   ❌ FAIL: curl not installed"
    exit 1
fi

if ! command -v jq &> /dev/null; then
    echo "   ❌ FAIL: jq not installed"
    echo ""
    echo "💡 Install jq:"
    echo "   sudo apt install jq"
    exit 1
fi

echo "   ✅ Required tools available"

# Check 3: Registry access
echo ""
echo "3️⃣  Testing registry access..."

RESPONSE=$(curl -s -w "%{http_code}" -o /dev/null \
    -H "Authorization: Bearer $TOKEN" \
    "$DIGITALOCEAN_API/registry")

if [ "$RESPONSE" != "200" ]; then
    echo "   ❌ FAIL: Cannot access DigitalOcean registry (HTTP $RESPONSE)"
    echo ""
    echo "💡 Check token validity:"
    echo "   - Token may be expired"
    echo "   - Token may lack registry permissions"
    echo "   - DigitalOcean API may be down"
    exit 1
fi

echo "   ✅ Registry access verified"
echo ""

# ============================================
# CLEANUP FUNCTIONS
# ============================================

# Function to get tags for a repository
get_tags() {
    local registry=$1
    local repository=$2

    # API endpoint with pagination support
    local url="$DIGITALOCEAN_API/registry/$registry/repositories/$repository/tags?per_page=200"

    # Fetch tags from API
    local response=$(curl -s -H "Authorization: Bearer $TOKEN" "$url")

    # Check for API errors
    if echo "$response" | jq -e '.message' > /dev/null 2>&1; then
        echo "ERROR: $(echo "$response" | jq -r '.message')" >&2
        return 1
    fi

    # Extract tags with creation dates, sort by date (newest first)
    echo "$response" | jq -r '.tags[] | "\(.updated_at)|\(.name)"' | sort -r
}

# Function to delete a tag
delete_tag() {
    local registry=$1
    local repository=$2
    local tag=$3

    local url="$DIGITALOCEAN_API/registry/$registry/repositories/$repository/tags/$tag"

    local response_code=$(curl -s -w "%{http_code}" -o /dev/null \
        -X DELETE \
        -H "Authorization: Bearer $TOKEN" \
        "$url")

    if [ "$response_code" = "204" ] || [ "$response_code" = "200" ]; then
        return 0
    else
        echo "   ⚠️  Delete failed (HTTP $response_code): $tag" >&2
        return 1
    fi
}

# Function to estimate image size (rough average)
estimate_size() {
    local count=$1
    # Average Docker image size: ~15-20 MB per tag (varies widely)
    # Use conservative estimate of 16 MB per tag
    local size_mb=$((count * 16))
    echo "$size_mb"
}

# ============================================
# PROCESS REPOSITORIES
# ============================================

TOTAL_REPOS=0
TOTAL_TAGS=0
TOTAL_TO_DELETE=0
TOTAL_DELETED=0

echo "📊 Processing Repositories"
echo "============================"
echo ""

for repo in "${!RETENTION[@]}"; do
    TOTAL_REPOS=$((TOTAL_REPOS + 1))

    retention=${RETENTION[$repo]}
    registry=${REGISTRY_MAP[$repo]}

    echo "Repository: $registry/$repo"
    echo "  Retention policy: Keep $retention most recent tags"

    # Get all tags sorted by date (newest first)
    tags=$(get_tags "$registry" "$repo")

    if [ $? -ne 0 ]; then
        echo "  ⚠️  WARNING: Failed to retrieve tags, skipping"
        echo ""
        continue
    fi

    # Count total tags
    tag_count=$(echo "$tags" | wc -l)
    TOTAL_TAGS=$((TOTAL_TAGS + tag_count))

    echo "  Total tags: $tag_count"

    # Separate :latest tag (always keep)
    latest_tag=$(echo "$tags" | grep '|latest$' || true)
    other_tags=$(echo "$tags" | grep -v '|latest$' || true)

    # Count tags to keep/delete (excluding :latest)
    tags_to_keep=$retention
    tags_to_delete=0

    if [ -n "$other_tags" ]; then
        other_tag_count=$(echo "$other_tags" | wc -l)

        if [ $other_tag_count -gt $tags_to_keep ]; then
            tags_to_delete=$((other_tag_count - tags_to_keep))
        fi
    fi

    echo "  Tags to delete: $tags_to_delete"

    if [ $tags_to_delete -eq 0 ]; then
        echo "  ✅ No cleanup needed"
        echo ""
        continue
    fi

    TOTAL_TO_DELETE=$((TOTAL_TO_DELETE + tags_to_delete))

    # Show tags being kept
    echo ""
    echo "  Keeping ($tags_to_keep newest + :latest):"

    if [ -n "$latest_tag" ]; then
        echo "    ✅ latest (protected)"
    fi

    if [ -n "$other_tags" ]; then
        echo "$other_tags" | head -n "$tags_to_keep" | while IFS='|' read -r date tag; do
            echo "    ✅ $tag ($(echo $date | cut -d'T' -f1))"
        done
    fi

    echo ""

    # Show/delete old tags
    if [ "$DRY_RUN" = true ]; then
        echo "  Would delete ($tags_to_delete oldest):"

        echo "$other_tags" | tail -n "+$((tags_to_keep + 1))" | while IFS='|' read -r date tag; do
            echo "    🗑️  $tag ($(echo $date | cut -d'T' -f1))"
        done

        size_mb=$(estimate_size $tags_to_delete)
        echo ""
        echo "  Estimated storage freed: ~${size_mb} MB"
    else
        echo "  🗑️  Deleting $tags_to_delete tags..."

        deleted_count=0
        echo "$other_tags" | tail -n "+$((tags_to_keep + 1))" | while IFS='|' read -r date tag; do
            if delete_tag "$registry" "$repo" "$tag"; then
                echo "    Deleted: $tag ✅"
                deleted_count=$((deleted_count + 1))
            fi
        done

        TOTAL_DELETED=$((TOTAL_DELETED + tags_to_delete))

        size_mb=$(estimate_size $tags_to_delete)
        echo ""
        echo "  ✅ Deleted $tags_to_delete tags (~${size_mb} MB freed)"
    fi

    echo ""
done

# ============================================
# SUMMARY
# ============================================

echo "📊 Summary"
echo "=========="
echo "Total repositories processed: $TOTAL_REPOS"
echo "Total tags found: $TOTAL_TAGS"

if [ "$DRY_RUN" = true ]; then
    echo "Tags to keep: $((TOTAL_TAGS - TOTAL_TO_DELETE))"
    echo "Tags to delete: $TOTAL_TO_DELETE"

    total_size_mb=$(estimate_size $TOTAL_TO_DELETE)
    echo "Estimated storage freed: ~${total_size_mb} MB"

    echo ""
    echo "⚠️  DRY RUN - No changes made"
    echo ""
    echo "ℹ️  Run with --confirm to actually delete tags:"
    echo "   bash .claude/skills/registry-cleanup/execute.sh --confirm"
else
    echo "Tags deleted: $TOTAL_DELETED"

    total_size_mb=$(estimate_size $TOTAL_DELETED)
    echo "Actual storage freed: ~${total_size_mb} MB"

    echo ""
    echo "✅ Registry cleanup complete"
    echo ""
    echo "ℹ️  Notes:"
    echo "   - Storage metrics may take 1-2 hours to update in DigitalOcean"
    echo "   - Deleted tags cannot be recovered"
    echo "   - :latest tags are always preserved"
fi

echo ""
