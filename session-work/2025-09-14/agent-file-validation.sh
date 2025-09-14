#!/bin/bash

# Agent File Validation Script
# Checks all agent configuration files for correct file references

echo "=========================================="
echo "Agent File Reference Validation Report"
echo "Date: $(date)"
echo "=========================================="
echo ""

# Base directory
BASE_DIR="/home/chad/repos/witchcityrope-react"
AGENT_DIR="$BASE_DIR/.claude/agents"
LESSONS_DIR="$BASE_DIR/docs/lessons-learned"

# Track issues found
ISSUES_FOUND=0

# Function to check if file exists
check_file() {
    local file="$1"
    local agent="$2"
    local context="$3"
    
    # Convert relative paths to absolute
    if [[ "$file" =~ ^/docs/ ]]; then
        file="$BASE_DIR$file"
    elif [[ "$file" =~ ^/ ]]; then
        # Already absolute
        :
    else
        # Assume relative to BASE_DIR
        file="$BASE_DIR/$file"
    fi
    
    if [ ! -f "$file" ]; then
        echo "❌ MISSING: $agent - $context"
        echo "   File: $file"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
        return 1
    else
        echo "✅ FOUND: $agent - $context"
        return 0
    fi
}

# Function to extract file references from agent config
extract_references() {
    local agent_file="$1"
    grep -E "Read .*/.*\.(md|json|yml|yaml|txt)" "$agent_file" | \
        sed -E 's/.*Read [`"]([^`"]+)[`"].*/\1/' | \
        grep -v "^Read$"
}

echo "Phase 1: Checking Agent Configuration Files"
echo "============================================"
echo ""

# List of all agents
agents=(
    "design/database-designer"
    "design/ui-designer"
    "development/react-developer"
    "implementation/backend-developer"
    "implementation/blazor-developer"
    "planning/business-requirements"
    "planning/functional-spec"
    "quality/lint-validator"
    "quality/prettier-formatter"
    "research/technology-researcher"
    "testing/code-reviewer"
    "testing/test-developer"
    "testing/test-executor"
    "utility/git-manager"
    "utility/librarian"
)

for agent_path in "${agents[@]}"; do
    agent_file="$AGENT_DIR/${agent_path}.md"
    agent_name=$(basename "$agent_path")
    
    echo "Checking: $agent_name"
    echo "-------------------"
    
    if [ ! -f "$agent_file" ]; then
        echo "❌ Agent config file missing: $agent_file"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
        continue
    fi
    
    # Check primary lessons learned file
    lessons_file="$LESSONS_DIR/${agent_name}-lessons-learned.md"
    check_file "$lessons_file" "$agent_name" "Primary lessons learned"
    
    # Extract and check all file references in the agent config
    while IFS= read -r ref; do
        if [ ! -z "$ref" ]; then
            check_file "$ref" "$agent_name" "Referenced in config"
        fi
    done < <(extract_references "$agent_file")
    
    echo ""
done

echo ""
echo "Phase 2: Checking Lessons Learned Files Exist"
echo "=============================================="
echo ""

# Expected lessons learned files based on agents
expected_lessons=(
    "backend-developer-lessons-learned.md"
    "blazor-developer-lessons-learned.md"
    "business-requirements-lessons-learned.md"
    "code-reviewer-lessons-learned.md"
    "database-designer-lessons-learned.md"
    "functional-spec-lessons-learned.md"
    "git-manager-lessons-learned.md"
    "librarian-lessons-learned.md"
    "lint-validator-lessons-learned.md"
    "orchestrator-lessons-learned.md"
    "prettier-formatter-lessons-learned.md"
    "react-developer-lessons-learned.md"
    "technology-researcher-lessons-learned.md"
    "test-developer-lessons-learned.md"
    "test-executor-lessons-learned.md"
    "ui-designer-lessons-learned.md"
)

for lessons in "${expected_lessons[@]}"; do
    file="$LESSONS_DIR/$lessons"
    if [ ! -f "$file" ]; then
        echo "❌ MISSING: $lessons"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
        
        # Check if there's a similar file (case sensitivity issue)
        similar=$(ls "$LESSONS_DIR" | grep -i "$(echo $lessons | sed 's/-/_/g')")
        if [ ! -z "$similar" ]; then
            echo "   Possible match: $similar"
        fi
    else
        echo "✅ EXISTS: $lessons"
    fi
done

echo ""
echo "Phase 3: Checking Critical Workflow Files"
echo "=========================================="
echo ""

critical_files=(
    "/docs/standards-processes/workflow-orchestration-process.md"
    "/docs/standards-processes/agent-handoff-template.md"
    "/docs/lessons-learned/LESSONS-LEARNED-TEMPLATE.md"
    "/docs/lessons-learned/LESSONS-LEARNED-VALIDATION-CHECKLIST.md"
    "/docs/architecture/functional-area-master-index.md"
    "/docs/architecture/file-registry.md"
)

for file in "${critical_files[@]}"; do
    check_file "$file" "Workflow" "Critical file"
done

echo ""
echo "=========================================="
echo "VALIDATION SUMMARY"
echo "=========================================="
echo "Total Issues Found: $ISSUES_FOUND"
echo ""

if [ $ISSUES_FOUND -eq 0 ]; then
    echo "🎉 All file references are valid!"
else
    echo "⚠️  Found $ISSUES_FOUND issues that need to be fixed"
    echo ""
    echo "Next Steps:"
    echo "1. Create missing lessons learned files"
    echo "2. Fix incorrect file paths in agent configurations"
    echo "3. Ensure case sensitivity matches exactly"
fi