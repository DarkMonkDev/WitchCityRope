#!/usr/bin/env node
/**
 * API Contract Validation Script
 *
 * This script validates that frontend API calls match the backend OpenAPI specification.
 * It catches mismatches like the /ticket vs /participation bug before they reach production.
 *
 * Usage:
 *   node scripts/validate-api-contract.js
 *
 * Exit codes:
 *   0 - All API calls match the OpenAPI spec
 *   1 - Mismatches found or validation error
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Colors for terminal output
const colors = {
  reset: '\x1b[0m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
};

function log(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

// Load OpenAPI specification
function loadOpenAPISpec() {
  const specPath = path.join(__dirname, '../apps/api/openapi.json');

  if (!fs.existsSync(specPath)) {
    log('❌ ERROR: OpenAPI spec not found at ' + specPath, 'red');
    log('   Run: ./scripts/export-openapi-spec.sh', 'yellow');
    process.exit(1);
  }

  try {
    const specContent = fs.readFileSync(specPath, 'utf-8');
    return JSON.parse(specContent);
  } catch (error) {
    log('❌ ERROR: Failed to parse OpenAPI spec: ' + error.message, 'red');
    process.exit(1);
  }
}

// Extract API endpoint definitions from OpenAPI spec
function extractEndpoints(spec) {
  const endpoints = new Map();

  for (const [path, methods] of Object.entries(spec.paths)) {
    for (const [method, definition] of Object.entries(methods)) {
      if (['get', 'post', 'put', 'delete', 'patch'].includes(method.toLowerCase())) {
        const key = `${method.toUpperCase()} ${path}`;
        endpoints.set(key, {
          path,
          method: method.toUpperCase(),
          operationId: definition.operationId,
          summary: definition.summary,
        });
      }
    }
  }

  return endpoints;
}

// Find all TypeScript/JavaScript files that might contain API calls
function findApiFiles(dir, fileList = []) {
  const files = fs.readdirSync(dir);

  files.forEach(file => {
    const filePath = path.join(dir, file);
    const stat = fs.statSync(filePath);

    if (stat.isDirectory()) {
      // Skip node_modules and build directories
      if (!['node_modules', 'dist', 'build', '.next', 'coverage'].includes(file)) {
        findApiFiles(filePath, fileList);
      }
    } else if (file.match(/\.(ts|tsx|js|jsx)$/)) {
      fileList.push(filePath);
    }
  });

  return fileList;
}

// Extract API calls from frontend code
function extractApiCalls(files) {
  const apiCalls = [];

  // Patterns to match API calls
  const patterns = [
    // axios.get/post/put/delete('/api/...')
    /axios\.(get|post|put|delete|patch)\s*\(\s*['"`](\/?api\/[^'"`\s]+)['"`]/g,
    // fetch('/api/...', { method: 'POST' })
    /fetch\s*\(\s*['"`](\/?api\/[^'"`\s]+)['"`]\s*,\s*\{[^}]*method:\s*['"`](GET|POST|PUT|DELETE|PATCH)['"`]/gi,
    // apiRequest('GET', '/api/...')
    /apiRequest\s*\(\s*['"`](GET|POST|PUT|DELETE|PATCH)['"`]\s*,\s*['"`](\/?api\/[^'"`\s]+)['"`]/gi,
    // Simple fetch('/api/...')
    /fetch\s*\(\s*['"`](\/?api\/[^'"`\s]+)['"`]\s*\)/g,
  ];

  files.forEach(file => {
    const content = fs.readFileSync(file, 'utf-8');

    patterns.forEach(pattern => {
      let match;
      pattern.lastIndex = 0; // Reset regex state

      while ((match = pattern.exec(content)) !== null) {
        let method, url;

        // Different capture group orders based on pattern
        if (match[0].includes('apiRequest')) {
          method = match[1];
          url = match[2];
        } else if (match[0].includes('fetch') && match[0].includes('method:')) {
          url = match[1];
          method = match[2];
        } else if (match[0].includes('axios')) {
          method = match[1].toUpperCase();
          url = match[2];
        } else {
          method = 'GET'; // Default for simple fetch
          url = match[1];
        }

        // Normalize URL
        url = url.replace(/^\//, ''); // Remove leading slash
        if (url.startsWith('api/')) {
          url = '/' + url;
        }

        apiCalls.push({
          file: file.replace(path.join(__dirname, '..'), ''),
          method: method.toUpperCase(),
          url,
          line: content.substring(0, match.index).split('\n').length,
        });
      }
    });
  });

  return apiCalls;
}

// Normalize path for comparison (handle route parameters)
function normalizePath(url) {
  // Convert frontend dynamic segments to OpenAPI format
  // Example: /api/events/${id} -> /api/events/{id}
  return url.replace(/\$\{[^}]+\}/g, (match) => {
    const paramName = match.slice(2, -1);
    return `{${paramName}}`;
  });
}

// Check if an API call matches an endpoint in the spec
function matchesEndpoint(call, endpoints) {
  const normalizedUrl = normalizePath(call.url);
  const key = `${call.method} ${normalizedUrl}`;

  if (endpoints.has(key)) {
    return { matched: true, endpoint: endpoints.get(key) };
  }

  // Check for close matches (fuzzy matching)
  const closeMatches = [];
  for (const [endpointKey, endpoint] of endpoints.entries()) {
    const [endpointMethod, endpointPath] = endpointKey.split(' ');

    if (endpointMethod === call.method) {
      // Calculate similarity
      const callParts = normalizedUrl.split('/').filter(Boolean);
      const endpointParts = endpointPath.split('/').filter(Boolean);

      if (Math.abs(callParts.length - endpointParts.length) <= 1) {
        let matches = 0;
        for (let i = 0; i < Math.min(callParts.length, endpointParts.length); i++) {
          if (callParts[i] === endpointParts[i] ||
              endpointParts[i].startsWith('{') ||
              callParts[i].startsWith('{')) {
            matches++;
          }
        }

        const similarity = matches / Math.max(callParts.length, endpointParts.length);
        if (similarity > 0.5) {
          closeMatches.push({ endpoint, similarity, key: endpointKey });
        }
      }
    }
  }

  return { matched: false, closeMatches };
}

// Main validation logic
function validateApiContract() {
  log('\n🔍 API Contract Validation', 'cyan');
  log('━'.repeat(80), 'cyan');

  // Step 1: Load OpenAPI spec
  log('\n📋 Loading OpenAPI specification...', 'blue');
  const spec = loadOpenAPISpec();
  const endpoints = extractEndpoints(spec);
  log(`   Found ${endpoints.size} endpoints in spec`, 'green');

  // Step 2: Find frontend files
  log('\n📁 Scanning frontend code...', 'blue');
  const webDir = path.join(__dirname, '../apps/web/src');
  const files = findApiFiles(webDir);
  log(`   Found ${files.length} source files to analyze`, 'green');

  // Step 3: Extract API calls
  log('\n🔎 Extracting API calls from frontend...', 'blue');
  const apiCalls = extractApiCalls(files);
  log(`   Found ${apiCalls.length} API calls`, 'green');

  // Step 4: Validate each call
  log('\n✅ Validating API calls against spec...', 'blue');

  const mismatches = [];
  const matched = [];

  apiCalls.forEach(call => {
    const result = matchesEndpoint(call, endpoints);

    if (result.matched) {
      matched.push(call);
    } else {
      mismatches.push({ call, closeMatches: result.closeMatches });
    }
  });

  // Step 5: Report results
  log('\n📊 Validation Results', 'cyan');
  log('━'.repeat(80), 'cyan');
  log(`✅ Matched: ${matched.length}`, 'green');
  log(`❌ Mismatched: ${mismatches.length}`, mismatches.length > 0 ? 'red' : 'green');

  if (mismatches.length > 0) {
    log('\n❌ API Contract Mismatches Found:', 'red');
    log('━'.repeat(80), 'red');

    mismatches.forEach(({ call, closeMatches }, index) => {
      log(`\n${index + 1}. ${call.method} ${call.url}`, 'yellow');
      log(`   File: ${call.file}:${call.line}`, 'reset');

      if (closeMatches.length > 0) {
        log(`   💡 Did you mean one of these?`, 'cyan');
        closeMatches
          .sort((a, b) => b.similarity - a.similarity)
          .slice(0, 3)
          .forEach(match => {
            log(`      ${match.key} (${Math.round(match.similarity * 100)}% match)`, 'yellow');
            if (match.endpoint.summary) {
              log(`         ${match.endpoint.summary}`, 'reset');
            }
          });
      } else {
        log(`   ⚠️  No similar endpoints found in OpenAPI spec`, 'yellow');
      }
    });

    log('\n🔧 How to Fix:', 'cyan');
    log('   1. Check if the endpoint exists in the backend', 'reset');
    log('   2. Update frontend to use correct endpoint path', 'reset');
    log('   3. Or implement missing endpoint in backend', 'reset');
    log('   4. Re-export OpenAPI spec: ./scripts/export-openapi-spec.sh', 'reset');

    log('\n❌ Validation FAILED', 'red');
    process.exit(1);
  }

  log('\n✅ All API calls match the OpenAPI specification!', 'green');
  log('\n💡 Remember to:', 'cyan');
  log('   - Re-export OpenAPI spec when adding new endpoints', 'reset');
  log('   - Run validation before committing API changes', 'reset');

  process.exit(0);
}

// Run validation
validateApiContract();
