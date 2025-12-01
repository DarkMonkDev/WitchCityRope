#!/usr/bin/env node
/**
 * Fix hardcoded localhost URLs in test files
 *
 * This script:
 * 1. Adds environment-aware URL constants after imports
 * 2. Replaces hardcoded URLs with template literal references
 */

const fs = require('fs');
const path = require('path');

const files = [
  'tests/e2e/admin-checkin-sessions.spec.ts',
  'tests/e2e/admin-events-sessions.spec.ts',
  'tests/e2e/checkin-attendee-workflow.spec.ts',
  'tests/e2e/cms-accessibility.spec.ts',
  'tests/e2e/cms.spec.ts',
  'tests/e2e/cms-workflow.spec.ts',
  'tests/e2e/compare-wireframe.spec.ts',
  'tests/e2e/comprehensive-rsvp-verification.spec.ts',
  'tests/e2e/docker-services-test.spec.ts',
  'tests/e2e/events-basic-validation.spec.ts',
  'tests/e2e/global-setup.ts',
  'tests/e2e/infinite-loop-verification.spec.ts',
  'tests/e2e/navigation-workflow.spec.ts',
  'tests/e2e/pages/events-list.page.ts',
  'tests/e2e/payment.spec.ts',
  'tests/e2e/paypal-integration.spec.ts',
  'tests/e2e/profile-workflow.spec.ts',
  'tests/e2e/public-events-anonymous.spec.ts',
  'tests/e2e/react-render-test.spec.ts',
  'tests/e2e/rsvp-event-waiver.spec.ts',
  'tests/e2e/rsvp-lifecycle-persistence.spec.ts',
  'tests/e2e/scroll-restoration.spec.ts',
  'tests/e2e/templates/event-creation-persistence-template.ts',
  'tests/e2e/templates/persistence-test-template.ts',
  'tests/e2e/templates/profile-update-persistence-template.ts',
  'tests/e2e/templates/rsvp-persistence-template.ts',
  'tests/e2e/templates/ticket-cancellation-persistence-template.ts',
  'tests/e2e/test-utils/helpers/service.helper.ts',
  'tests/e2e/test-utils/utils/database-helpers.ts',
  'tests/e2e/ticket-lifecycle-persistence.spec.ts',
  'tests/e2e/ticket-purchase-waiver.spec.ts',
  'tests/e2e/ticket-refund-workflow.spec.ts',
  'tests/e2e/tiptap-editors.spec.ts',
  'tests/e2e/user-dashboard-workflow.spec.ts',
  'tests/e2e/utils/database-helpers.ts',
  'tests/e2e/venue-creation.spec.ts',
  'tests/e2e/venue-display.spec.ts',
  'tests/e2e/venue-editing.spec.ts',
  'tests/e2e/venue-location-field.spec.ts',
  'tests/e2e/vetting-workflow-integration.spec.ts',
  'tests/e2e/volunteer-event-waiver.spec.ts',
];

// Skip files that are already properly fixed
const alreadyFixed = [
  'tests/e2e/checkin/helpers/tokenHelpers.ts',
  'tests/e2e/checkin-staff-authentication.spec.ts',
];

const CONSTANTS = `
// Environment-aware URLs for container/host compatibility
const WEB_BASE_URL = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5173';
const API_BASE_URL = process.env.API_URL || 'http://localhost:5655';
`;

function hasConstants(content) {
  return content.includes('WEB_BASE_URL') || content.includes('API_BASE_URL');
}

function findInsertPosition(content) {
  // Find the end of imports section
  const lines = content.split('\n');
  let lastImportLine = -1;
  let inMultilineImport = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Track multiline imports
    if (line.includes('import ') && line.includes('{') && !line.includes('}')) {
      inMultilineImport = true;
    }
    if (inMultilineImport && line.includes('}')) {
      inMultilineImport = false;
      lastImportLine = i;
      continue;
    }

    if (line.trim().startsWith('import ') || line.trim().startsWith('import{')) {
      lastImportLine = i;
    }
  }

  // If no imports found, insert at start
  if (lastImportLine === -1) {
    return 0;
  }

  return lastImportLine + 1;
}

function fixFile(filePath) {
  const fullPath = path.join(process.cwd(), filePath);

  if (!fs.existsSync(fullPath)) {
    console.log(`Skipping (not found): ${filePath}`);
    return;
  }

  let content = fs.readFileSync(fullPath, 'utf-8');
  const originalContent = content;

  // Check if file has localhost URLs that need fixing
  const hasWeb = content.includes('localhost:5173');
  const hasApi = content.includes('localhost:5655');

  if (!hasWeb && !hasApi) {
    console.log(`Skipping (no localhost URLs): ${filePath}`);
    return;
  }

  // Add constants if not present
  const needsConstants = !hasConstants(content);
  if (needsConstants) {
    const lines = content.split('\n');
    const insertPos = findInsertPosition(content);
    lines.splice(insertPos, 0, CONSTANTS);
    content = lines.join('\n');
  }

  // Process line by line to avoid replacing the constants themselves
  const lines = content.split('\n');
  const processedLines = lines.map(line => {
    // Skip constant definition lines
    if (line.includes('const WEB_BASE_URL') || line.includes('const API_BASE_URL')) {
      return line;
    }

    let processedLine = line;

    // Pattern 1: Template literals like `http://localhost:5173/path`
    processedLine = processedLine.replace(/`http:\/\/localhost:5173([^`]*)`/g, '`${WEB_BASE_URL}$1`');
    processedLine = processedLine.replace(/`http:\/\/localhost:5655([^`]*)`/g, '`${API_BASE_URL}$1`');

    // Pattern 2: Regular strings 'http://localhost:5173/path' - convert to template literal
    processedLine = processedLine.replace(/'http:\/\/localhost:5173([^']+)'/g, '`${WEB_BASE_URL}$1`');
    processedLine = processedLine.replace(/'http:\/\/localhost:5655([^']+)'/g, '`${API_BASE_URL}$1`');

    // Pattern 3: Just the base URL 'http://localhost:5173' or 'http://localhost:5655'
    processedLine = processedLine.replace(/'http:\/\/localhost:5173'/g, 'WEB_BASE_URL');
    processedLine = processedLine.replace(/'http:\/\/localhost:5655'/g, 'API_BASE_URL');

    // Pattern 4: Double quotes
    processedLine = processedLine.replace(/"http:\/\/localhost:5173([^"]+)"/g, '`${WEB_BASE_URL}$1`');
    processedLine = processedLine.replace(/"http:\/\/localhost:5655([^"]+)"/g, '`${API_BASE_URL}$1`');
    processedLine = processedLine.replace(/"http:\/\/localhost:5173"/g, 'WEB_BASE_URL');
    processedLine = processedLine.replace(/"http:\/\/localhost:5655"/g, 'API_BASE_URL');

    // Pattern 5: Already has process.env fallback - just use constant
    processedLine = processedLine.replace(/process\.env\.PLAYWRIGHT_BASE_URL\s*\|\|\s*'http:\/\/localhost:5173'/g, 'WEB_BASE_URL');
    processedLine = processedLine.replace(/process\.env\.API_URL\s*\|\|\s*'http:\/\/localhost:5655'/g, 'API_BASE_URL');

    // Pattern 6: Fix any baseURL assignment
    processedLine = processedLine.replace(/baseURL:\s*'http:\/\/localhost:5173'/g, 'baseURL: WEB_BASE_URL');
    processedLine = processedLine.replace(/baseURL:\s*'http:\/\/localhost:5655'/g, 'baseURL: API_BASE_URL');

    return processedLine;
  });

  content = processedLines.join('\n');

  if (content !== originalContent) {
    fs.writeFileSync(fullPath, content);
    console.log(`Fixed: ${filePath}`);
  } else {
    console.log(`No changes needed: ${filePath}`);
  }
}

// Process all files
console.log('Fixing hardcoded localhost URLs in test files...\n');

for (const file of files) {
  if (alreadyFixed.includes(file)) {
    console.log(`Skipping (already fixed): ${file}`);
    continue;
  }
  fixFile(file);
}

console.log('\nDone!');
