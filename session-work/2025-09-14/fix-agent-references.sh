#!/bin/bash

# Script to fix all agent file references
# Date: 2025-09-14

BASE_DIR="/home/chad/repos/witchcityrope-react"
AGENT_DIR="$BASE_DIR/.claude/agents"

echo "=========================================="
echo "Fixing Agent File References"
echo "Date: $(date)"
echo "=========================================="
echo ""

# Fix database-designer.md
echo "Fixing database-designer.md..."
sed -i 's|/docs/lessons-learned/database-developers.md|/home/chad/repos/witchcityrope-react/docs/lessons-learned/database-designer-lessons-learned.md|g' "$AGENT_DIR/design/database-designer.md"

# Fix ui-designer.md
echo "Fixing ui-designer.md..."
sed -i 's|/docs/ARCHITECTURE.md|/home/chad/repos/witchcityrope-react/ARCHITECTURE.md|g' "$AGENT_DIR/design/ui-designer.md"
# Remove references to non-existent files
sed -i '/\/docs\/standards-processes\/form-fields-and-validation-standards.md/d' "$AGENT_DIR/design/ui-designer.md"
sed -i '/\/docs\/standards-processes\/development-standards\/react-patterns.md/d' "$AGENT_DIR/design/ui-designer.md"

# Fix react-developer.md
echo "Fixing react-developer.md..."
sed -i 's|/docs/ARCHITECTURE.md|/home/chad/repos/witchcityrope-react/ARCHITECTURE.md|g' "$AGENT_DIR/development/react-developer.md"
# Remove references to non-existent files
sed -i '/\/docs\/standards-processes\/form-fields-and-validation-standards.md/d' "$AGENT_DIR/development/react-developer.md"
sed -i '/\/docs\/functional-areas\/authentication\/jwt-service-to-service-auth.md/d' "$AGENT_DIR/development/react-developer.md"
# Fix malformed Docker guide reference
sed -i 's|1\. Read the Docker Operations Guide at: ||g' "$AGENT_DIR/development/react-developer.md"

# Fix backend-developer.md
echo "Fixing backend-developer.md..."
# Fix malformed Docker guide reference
sed -i 's|1\. Read the Docker Operations Guide at: ||g' "$AGENT_DIR/implementation/backend-developer.md"

# Fix blazor-developer.md (deprecated)
echo "Fixing blazor-developer.md..."
sed -i 's|/docs/lessons-learned/ui-developers.md|/home/chad/repos/witchcityrope-react/docs/lessons-learned/ui-designer-lessons-learned.md|g' "$AGENT_DIR/implementation/blazor-developer.md"
# Remove references to non-existent files
sed -i '/\/docs\/standards-processes\/form-fields-and-validation-standards.md/d' "$AGENT_DIR/implementation/blazor-developer.md"
sed -i '/\/docs\/functional-areas\/authentication\/jwt-service-to-service-auth.md/d' "$AGENT_DIR/implementation/blazor-developer.md"

# Fix functional-spec.md
echo "Fixing functional-spec.md..."
sed -i 's|/docs/lessons-learned/ui-developers.md|/home/chad/repos/witchcityrope-react/docs/lessons-learned/ui-designer-lessons-learned.md|g' "$AGENT_DIR/planning/functional-spec.md"
sed -i 's|/docs/lessons-learned/database-developers.md|/home/chad/repos/witchcityrope-react/docs/lessons-learned/database-designer-lessons-learned.md|g' "$AGENT_DIR/planning/functional-spec.md"

# Fix test-developer.md
echo "Fixing test-developer.md..."
# Fix malformed Docker guide reference
sed -i 's|1\. Read the Docker Operations Guide at: ||g' "$AGENT_DIR/testing/test-developer.md"

# Fix test-executor.md
echo "Fixing test-executor.md..."
# Remove references to non-existent files
sed -i '/\/docs\/guides-setup\/docker\/docker-development.md/d' "$AGENT_DIR/testing/test-executor.md"
# Remove malformed references
sed -i "/4\. Read '\/home\/chad\/repos\/witchcityrope-react\/docs\/standards-processes\/development-standards\/docker-development.md'/d" "$AGENT_DIR/testing/test-executor.md"
sed -i "/5\. Read '\/home\/chad\/repos\/witchcityrope-react\/docs\/standards-processes\/testing\/TESTING_GUIDE.md'/d" "$AGENT_DIR/testing/test-executor.md"
# Fix malformed Docker guide reference
sed -i 's|1\. Read the Docker Operations Guide at: ||g' "$AGENT_DIR/testing/test-executor.md"

echo ""
echo "=========================================="
echo "Standardizing all lessons learned paths..."
echo "=========================================="
echo ""

# Standardize all lessons learned references to use absolute paths
for agent_file in $(find "$AGENT_DIR" -name "*.md" -type f); do
    agent_name=$(basename "$agent_file" .md)
    echo "Checking $agent_name..."
    
    # Update lessons learned references to use absolute paths
    sed -i "s|/docs/lessons-learned/${agent_name}-lessons-learned.md|/home/chad/repos/witchcityrope-react/docs/lessons-learned/${agent_name}-lessons-learned.md|g" "$agent_file"
done

echo ""
echo "=========================================="
echo "Verification Complete"
echo "=========================================="
echo ""
echo "Next step: Run validation script to verify all fixes:"
echo "  /home/chad/repos/witchcityrope-react/session-work/2025-09-14/agent-file-validation.sh"