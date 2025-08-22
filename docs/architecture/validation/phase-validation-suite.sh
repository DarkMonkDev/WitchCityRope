#!/bin/bash
# PHASE-BASED DOCUMENTATION VALIDATION SUITE
# Created: 2025-08-22
# Purpose: Prevent documentation disasters through comprehensive phase-gate validation
# Authority: BLOCKS workflow progression on validation failure

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Base directory
BASE_DIR="/home/chad/repos/witchcityrope-react"
DOCS_DIR="$BASE_DIR/docs"

# Sacred Six - Files that MUST exist only in project root (not in /docs/)
SACRED_SIX=("CLAUDE.md" "PROGRESS.md" "ARCHITECTURE.md" "SECURITY.md" "ROADMAP.md")
# Special handling for README.md - only check for root-level duplicates
README_CHECK_PATHS=("/README.md" "/docs/README.md")

# Validation failure flag
VALIDATION_FAILED=0

# Logging function
log() {
    local level=$1
    local message=$2
    local color=$NC
    
    case $level in
        "ERROR") color=$RED; ((VALIDATION_FAILED++)) ;;
        "SUCCESS") color=$GREEN ;;
        "WARNING") color=$YELLOW ;;
        "INFO") color=$BLUE ;;
    esac
    
    echo -e "${color}[$level]${NC} $message"
}

# Header function
print_header() {
    echo -e "${BLUE}╔══════════════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${BLUE}║                   PHASE-BASED VALIDATION SYSTEM                      ║${NC}"
    echo -e "${BLUE}║                  Documentation Disaster Prevention                   ║${NC}"
    echo -e "${BLUE}║                        ZERO TOLERANCE MODE                          ║${NC}"
    echo -e "${BLUE}╚══════════════════════════════════════════════════════════════════════╝${NC}"
    echo
}

# Check for duplicate key documents
check_duplicate_key_files() {
    log "INFO" "Checking for duplicate key documents..."
    
    # Check the Sacred Six (excluding README.md for now)
    for doc in "${SACRED_SIX[@]}"; do
        # Find all instances (excluding archives, node_modules, and other non-project directories)
        local instances=$(find "$BASE_DIR" -name "$doc" -not -path "*/_archive/*" -not -path "*/node_modules/*" -not -path "*/packages/*/node_modules/*" -not -path "*/tests/*/node_modules/*" 2>/dev/null | wc -l)
        
        if [[ $instances -gt 1 ]]; then
            log "ERROR" "DISASTER: Multiple $doc files found:"
            find "$BASE_DIR" -name "$doc" -not -path "*/_archive/*" -not -path "*/node_modules/*" -not -path "*/packages/*/node_modules/*" -not -path "*/tests/*/node_modules/*" 2>/dev/null
            return 1
        elif [[ $instances -eq 0 ]]; then
            log "WARNING" "Missing canonical document: $doc"
        else
            # Verify it's in the canonical location
            if [[ -f "$BASE_DIR/$doc" ]]; then
                log "SUCCESS" "✓ $doc in canonical location"
            else
                log "ERROR" "VIOLATION: $doc exists but not in canonical location"
                find "$BASE_DIR" -name "$doc" -not -path "*/_archive/*" -not -path "*/node_modules/*" -not -path "*/packages/*/node_modules/*" -not -path "*/tests/*/node_modules/*" 2>/dev/null
                return 1
            fi
        fi
    done
    
    # Special handling for README.md - only check critical locations
    log "INFO" "Checking README.md canonical location..."
    local readme_violations=0
    
    # Check if README exists in both root and docs (the critical violation)
    if [[ -f "$BASE_DIR/README.md" && -f "$BASE_DIR/docs/README.md" ]]; then
        log "ERROR" "CRITICAL: README.md exists in both root and /docs/ - must be only in root"
        ((readme_violations++))
    elif [[ -f "$BASE_DIR/README.md" ]]; then
        log "SUCCESS" "✓ README.md in canonical location (project root)"
    elif [[ -f "$BASE_DIR/docs/README.md" ]]; then
        log "ERROR" "VIOLATION: README.md in /docs/ should be in project root"
        ((readme_violations++))
    else
        log "WARNING" "No README.md found in canonical location"
    fi
    
    if [[ $readme_violations -gt 0 ]]; then
        return 1
    fi
    
    return 0
}

# Check for files in /docs/ root (should be ZERO)
check_docs_root_pollution() {
    log "INFO" "Checking for /docs/ root pollution..."
    
    local root_files=$(find "$DOCS_DIR" -maxdepth 1 -type f 2>/dev/null | wc -l)
    
    if [[ $root_files -gt 0 ]]; then
        log "ERROR" "VIOLATION: Files found in /docs/ root (should be ZERO):"
        find "$DOCS_DIR" -maxdepth 1 -type f 2>/dev/null
        return 1
    else
        log "SUCCESS" "✓ /docs/ root is clean (0 files)"
        return 0
    fi
}

# Check for multiple archive folders
check_archive_integrity() {
    log "INFO" "Checking archive folder integrity..."
    
    # Look for top-level archive directories only (not subdirectories of _archive)
    local archive_count=$(find "$DOCS_DIR" -maxdepth 2 -name "*archive*" -type d -not -path "$DOCS_DIR/_archive/*" 2>/dev/null | wc -l)
    
    if [[ $archive_count -gt 1 ]]; then
        log "ERROR" "DISASTER: Multiple top-level archive folders detected:"
        find "$DOCS_DIR" -maxdepth 2 -name "*archive*" -type d -not -path "$DOCS_DIR/_archive/*" 2>/dev/null
        return 1
    elif [[ $archive_count -eq 1 ]]; then
        local archive_path=$(find "$DOCS_DIR" -maxdepth 2 -name "*archive*" -type d -not -path "$DOCS_DIR/_archive/*" 2>/dev/null)
        if [[ "$archive_path" == "$DOCS_DIR/_archive" ]]; then
            log "SUCCESS" "✓ Single archive folder in correct location"
            return 0
        else
            log "ERROR" "VIOLATION: Archive folder in wrong location: $archive_path"
            return 1
        fi
    else
        log "INFO" "No archive folder found (acceptable)"
        return 0
    fi
}

# Check for /docs/docs/ nested disaster
check_nested_docs_disaster() {
    log "INFO" "Checking for nested /docs/docs/ disaster..."
    
    if [[ -d "$DOCS_DIR/docs" ]]; then
        log "ERROR" "CATASTROPHIC: /docs/docs/ folder detected - IMMEDIATE ATTENTION REQUIRED"
        return 1
    else
        log "SUCCESS" "✓ No nested docs disaster detected"
        return 0
    fi
}

# Validate functional area master index exists and is current
check_master_index() {
    log "INFO" "Validating functional area master index..."
    
    local master_index="$DOCS_DIR/architecture/functional-area-master-index.md"
    
    if [[ ! -f "$master_index" ]]; then
        log "ERROR" "CRITICAL: Functional area master index missing"
        return 1
    else
        # Check if it's been updated recently (within 7 days)
        local last_modified=$(stat -c %Y "$master_index" 2>/dev/null || echo 0)
        local week_ago=$(($(date +%s) - 604800))
        
        if [[ $last_modified -lt $week_ago ]]; then
            log "WARNING" "Master index older than 7 days - may need update"
        fi
        
        log "SUCCESS" "✓ Functional area master index exists"
        return 0
    fi
}

# Validate file registry exists and has recent entries
check_file_registry() {
    log "INFO" "Validating file registry..."
    
    local registry="$DOCS_DIR/architecture/file-registry.md"
    
    if [[ ! -f "$registry" ]]; then
        log "ERROR" "CRITICAL: File registry missing"
        return 1
    else
        log "SUCCESS" "✓ File registry exists"
        
        # Check for recent entries (within 24 hours)
        local today=$(date +%Y-%m-%d)
        if grep -q "$today" "$registry" 2>/dev/null; then
            log "SUCCESS" "✓ File registry has recent entries"
        else
            log "WARNING" "File registry has no entries from today"
        fi
        
        return 0
    fi
}

# PHASE 1: Requirements & Planning Validation
validate_phase_1_requirements() {
    log "INFO" "🔍 PHASE 1: REQUIREMENTS & PLANNING VALIDATION"
    echo "════════════════════════════════════════════════════"
    
    local phase_failed=0
    
    # Core structure checks
    check_duplicate_key_files || ((phase_failed++))
    check_docs_root_pollution || ((phase_failed++))
    check_archive_integrity || ((phase_failed++))
    check_nested_docs_disaster || ((phase_failed++))
    check_master_index || ((phase_failed++))
    check_file_registry || ((phase_failed++))
    
    # Requirements-specific checks
    log "INFO" "Checking requirements documentation structure..."
    
    # Check for business requirements in proper locations (excluding templates and archives)
    local misplaced_reqs=$(find "$DOCS_DIR" -name "business-requirements.md" -not -path "*/requirements/*" -not -path "*/_template/*" -not -path "*/_archive/*" 2>/dev/null | wc -l)
    if [[ $misplaced_reqs -gt 0 ]]; then
        log "ERROR" "VIOLATION: Business requirements in wrong locations:"
        find "$DOCS_DIR" -name "business-requirements.md" -not -path "*/requirements/*" -not -path "*/_template/*" -not -path "*/_archive/*" 2>/dev/null
        ((phase_failed++))
    else
        log "SUCCESS" "✓ Requirements documentation properly located"
    fi
    
    if [[ $phase_failed -eq 0 ]]; then
        log "SUCCESS" "✅ PHASE 1 VALIDATION PASSED"
        return 0
    else
        log "ERROR" "❌ PHASE 1 VALIDATION FAILED ($phase_failed violations)"
        return 1
    fi
}

# PHASE 2: Design & Architecture Validation  
validate_phase_2_design() {
    log "INFO" "🔍 PHASE 2: DESIGN & ARCHITECTURE VALIDATION"
    echo "══════════════════════════════════════════════════"
    
    local phase_failed=0
    
    # Run core validations
    validate_phase_1_requirements || ((phase_failed++))
    
    # Design-specific validations
    log "INFO" "Checking architecture document integrity..."
    
    # Check that ARCHITECTURE.md is only in root
    local arch_in_docs=$(find "$DOCS_DIR" -name "ARCHITECTURE.md" -not -path "*/_archive/*" 2>/dev/null | wc -l)
    if [[ $arch_in_docs -gt 0 ]]; then
        log "ERROR" "VIOLATION: ARCHITECTURE.md found in /docs/ (should only be in root):"
        find "$DOCS_DIR" -name "ARCHITECTURE.md" -not -path "*/_archive/*" 2>/dev/null
        ((phase_failed++))
    fi
    
    # Check technical design document locations
    local misplaced_designs=$(find "$DOCS_DIR" -name "technical-design.md" -not -path "*/design/*" 2>/dev/null | wc -l)
    if [[ $misplaced_designs -gt 0 ]]; then
        log "ERROR" "VIOLATION: Technical designs in wrong locations:"
        find "$DOCS_DIR" -name "technical-design.md" -not -path "*/design/*" 2>/dev/null
        ((phase_failed++))
    else
        log "SUCCESS" "✓ Technical design documents properly located"
    fi
    
    if [[ $phase_failed -eq 0 ]]; then
        log "SUCCESS" "✅ PHASE 2 VALIDATION PASSED"
        return 0
    else
        log "ERROR" "❌ PHASE 2 VALIDATION FAILED ($phase_failed violations)"
        return 1
    fi
}

# PHASE 3: Implementation Validation
validate_phase_3_implementation() {
    log "INFO" "🔍 PHASE 3: IMPLEMENTATION VALIDATION"
    echo "═════════════════════════════════════════════════"
    
    local phase_failed=0
    
    # Run previous phase validations
    validate_phase_2_design || ((phase_failed++))
    
    # Implementation-specific checks
    log "INFO" "Checking recent file creation compliance..."
    
    # Check for session work organization
    local session_violations=$(find "$BASE_DIR" -name "session-work" -not -path "./session-work" 2>/dev/null | wc -l)
    if [[ $session_violations -gt 0 ]]; then
        log "ERROR" "VIOLATION: Session work files in wrong locations:"
        find "$BASE_DIR" -name "session-work" -not -path "./session-work" 2>/dev/null
        ((phase_failed++))
    else
        log "SUCCESS" "✓ Session work properly contained"
    fi
    
    # Check for temporary files in wrong locations
    local temp_files=$(find "$BASE_DIR" -maxdepth 1 -name "*.tmp" -o -name "*.temp" -o -name "*~" 2>/dev/null | wc -l)
    if [[ $temp_files -gt 0 ]]; then
        log "WARNING" "Temporary files found in project root:"
        find "$BASE_DIR" -maxdepth 1 -name "*.tmp" -o -name "*.temp" -o -name "*~" 2>/dev/null
    fi
    
    if [[ $phase_failed -eq 0 ]]; then
        log "SUCCESS" "✅ PHASE 3 VALIDATION PASSED"
        return 0
    else
        log "ERROR" "❌ PHASE 3 VALIDATION FAILED ($phase_failed violations)"
        return 1
    fi
}

# PHASE 4: Testing & Validation
validate_phase_4_testing() {
    log "INFO" "🔍 PHASE 4: TESTING & VALIDATION"
    echo "════════════════════════════════════════════════"
    
    local phase_failed=0
    
    # Run previous phase validations
    validate_phase_3_implementation || ((phase_failed++))
    
    # Testing-specific validations
    log "INFO" "Checking test documentation organization..."
    
    # Check test plan locations
    local misplaced_tests=$(find "$DOCS_DIR" -name "test-plan.md" -not -path "*/testing/*" 2>/dev/null | wc -l)
    if [[ $misplaced_tests -gt 0 ]]; then
        log "ERROR" "VIOLATION: Test plans in wrong locations:"
        find "$DOCS_DIR" -name "test-plan.md" -not -path "*/testing/*" 2>/dev/null
        ((phase_failed++))
    else
        log "SUCCESS" "✓ Test documentation properly located"
    fi
    
    # Check for test files in project root
    local root_test_files=$(find "$BASE_DIR" -maxdepth 1 -name "*test*" -o -name "*spec*" 2>/dev/null | wc -l)
    if [[ $root_test_files -gt 0 ]]; then
        log "ERROR" "VIOLATION: Test files in project root:"
        find "$BASE_DIR" -maxdepth 1 -name "*test*" -o -name "*spec*" 2>/dev/null
        ((phase_failed++))
    else
        log "SUCCESS" "✓ No test files in project root"
    fi
    
    if [[ $phase_failed -eq 0 ]]; then
        log "SUCCESS" "✅ PHASE 4 VALIDATION PASSED"
        return 0
    else
        log "ERROR" "❌ PHASE 4 VALIDATION FAILED ($phase_failed violations)"
        return 1
    fi
}

# PHASE 5: Comprehensive Final Validation
validate_phase_5_finalization() {
    log "INFO" "🔍 PHASE 5: COMPREHENSIVE FINALIZATION VALIDATION"
    echo "═══════════════════════════════════════════════════════"
    
    local phase_failed=0
    
    # Run all previous validations
    validate_phase_4_testing || ((phase_failed++))
    
    # Final comprehensive checks
    log "INFO" "Running comprehensive final audit..."
    
    # Check for any remaining violations
    log "INFO" "Performing final duplicate document sweep..."
    
    for doc in "${SACRED_SIX[@]}"; do
        local violations=$(find "$DOCS_DIR" -name "$doc" -not -path "*/_archive/*" -not -path "*/node_modules/*" 2>/dev/null | wc -l)
        if [[ $violations -gt 0 ]]; then
            log "ERROR" "FINAL VIOLATION: $doc found in /docs/ (should only be in root):"
            find "$DOCS_DIR" -name "$doc" -not -path "*/_archive/*" -not -path "*/node_modules/*" 2>/dev/null
            ((phase_failed++))
        fi
    done
    
    # Final structure integrity check
    if [[ -f "$DOCS_DIR/../docs-structure-validator.sh" ]]; then
        log "INFO" "Running external structure validator..."
        if ! bash "$DOCS_DIR/../docs-structure-validator.sh" >/dev/null 2>&1; then
            log "ERROR" "External structure validator failed"
            ((phase_failed++))
        else
            log "SUCCESS" "✓ External structure validator passed"
        fi
    fi
    
    if [[ $phase_failed -eq 0 ]]; then
        log "SUCCESS" "🎉 PHASE 5 COMPREHENSIVE VALIDATION PASSED"
        log "SUCCESS" "🎉 ALL DOCUMENTATION STRUCTURE VALIDATION COMPLETE"
        return 0
    else
        log "ERROR" "❌ PHASE 5 FINAL VALIDATION FAILED ($phase_failed violations)"
        log "ERROR" "🚨 WORKFLOW MUST NOT PROCEED UNTIL ALL VIOLATIONS RESOLVED"
        return 1
    fi
}

# Quick validation (all phases at once)
validate_all_phases() {
    print_header
    log "INFO" "Running comprehensive validation across all phases..."
    echo
    
    local total_failed=0
    
    validate_phase_1_requirements || ((total_failed++))
    echo
    validate_phase_2_design || ((total_failed++))
    echo
    validate_phase_3_implementation || ((total_failed++))
    echo
    validate_phase_4_testing || ((total_failed++))
    echo
    validate_phase_5_finalization || ((total_failed++))
    
    echo
    echo "════════════════════════════════════════════════════════════════════════"
    
    if [[ $total_failed -eq 0 ]]; then
        log "SUCCESS" "🏆 ALL PHASE VALIDATIONS PASSED - DOCUMENTATION STRUCTURE PERFECT"
        log "SUCCESS" "📊 Zero Tolerance Enforcement: SUCCESSFUL"
        log "SUCCESS" "🛡️ Documentation Disaster Prevention: ACTIVE"
        return 0
    else
        log "ERROR" "🚨 VALIDATION FAILURES DETECTED: $total_failed phase(s) failed"
        log "ERROR" "⛔ WORKFLOW MUST BE BLOCKED UNTIL RESOLUTION"
        log "ERROR" "📋 Refer to PHASE-BASED-VALIDATION-SYSTEM.md for remediation"
        return 1
    fi
}

# Print usage
usage() {
    echo "Usage: $0 <command>"
    echo
    echo "Commands:"
    echo "  phase-1          Validate Requirements & Planning phase"
    echo "  phase-2          Validate Design & Architecture phase"  
    echo "  phase-3          Validate Implementation phase"
    echo "  phase-4          Validate Testing & Validation phase"
    echo "  phase-5          Validate Finalization phase"
    echo "  all              Run all phase validations"
    echo "  quick            Quick structure check"
    echo "  help             Show this help message"
    echo
    echo "Phase validation must pass before workflow can proceed to next phase."
    echo "Any validation failure BLOCKS workflow progression."
}

# Quick structure check
quick_check() {
    print_header
    log "INFO" "Running quick documentation structure check..."
    echo
    
    local failed=0
    
    check_duplicate_key_files || ((failed++))
    check_docs_root_pollution || ((failed++))
    check_archive_integrity || ((failed++))
    check_nested_docs_disaster || ((failed++))
    
    echo
    if [[ $failed -eq 0 ]]; then
        log "SUCCESS" "✅ Quick structure check PASSED"
        return 0
    else
        log "ERROR" "❌ Quick structure check FAILED ($failed violations)"
        return 1
    fi
}

# Main execution
main() {
    cd "$BASE_DIR" || exit 1
    
    case "${1:-}" in
        "phase-1") validate_phase_1_requirements ;;
        "phase-2") validate_phase_2_design ;;
        "phase-3") validate_phase_3_implementation ;;
        "phase-4") validate_phase_4_testing ;;
        "phase-5") validate_phase_5_finalization ;;
        "all") validate_all_phases ;;
        "quick") quick_check ;;
        "help"|"-h"|"--help") usage; exit 0 ;;
        "") log "ERROR" "No command specified. Use 'help' for usage."; exit 1 ;;
        *) log "ERROR" "Unknown command: $1. Use 'help' for usage."; exit 1 ;;
    esac
    
    exit $?
}

# Execute main with all arguments
main "$@"