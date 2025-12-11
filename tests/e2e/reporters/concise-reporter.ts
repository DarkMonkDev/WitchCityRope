/**
 * Concise Reporter for Playwright
 *
 * Outputs test results in a readable format:
 * - Passed - #num - Test name
 * - Failed - #num - Test name (with error details)
 *
 * Keeps output minimal for passing tests while showing full details for failures.
 */

import type { FullConfig, FullResult, Reporter, Suite, TestCase, TestResult } from '@playwright/test/reporter';

class ConciseReporter implements Reporter {
  private testNumber = 0;
  private passed = 0;
  private failed = 0;
  private skipped = 0;
  private failedTests: Array<{ number: number; title: string; error?: string }> = [];

  onBegin(config: FullConfig, suite: Suite): void {
    const totalTests = this.countTests(suite);
    console.log(`\nRunning ${totalTests} tests...\n`);
  }

  private countTests(suite: Suite): number {
    let count = 0;
    for (const test of suite.tests) {
      count++;
    }
    for (const child of suite.suites) {
      count += this.countTests(child);
    }
    return count;
  }

  onTestEnd(test: TestCase, result: TestResult): void {
    this.testNumber++;
    const fullTitle = test.titlePath().slice(1).join(' > '); // Skip the root suite name

    if (result.status === 'passed' || result.status === 'expected') {
      this.passed++;
      console.log(`Passed - ${this.testNumber} - ${fullTitle}`);
    } else if (result.status === 'skipped') {
      this.skipped++;
      console.log(`Skipped - ${this.testNumber} - ${fullTitle}`);
    } else {
      // Failed, timedOut, or unexpected
      this.failed++;
      console.log(`Failed - ${this.testNumber} - ${fullTitle}`);

      // Store failed test info for summary
      const errorMessage = result.errors?.[0]?.message?.split('\n')[0] || 'Unknown error';
      this.failedTests.push({
        number: this.testNumber,
        title: fullTitle,
        error: errorMessage
      });
    }
  }

  onEnd(result: FullResult): void {
    console.log('\n');
    console.log('════════════════════════════════════════════════════════════════');
    console.log('                    TEST RESULTS SUMMARY');
    console.log('════════════════════════════════════════════════════════════════');
    console.log('');
    console.log(`  Passed:  ${this.passed}`);
    console.log(`  Failed:  ${this.failed}`);
    console.log(`  Skipped: ${this.skipped}`);
    console.log('  ─────────────────');
    console.log(`  Total:   ${this.testNumber}`);
    console.log('');

    const passRate = this.testNumber > 0
      ? ((this.passed / this.testNumber) * 100).toFixed(1)
      : '0';

    if (this.failed === 0) {
      console.log(`  PASS RATE: ${passRate}%`);
    } else {
      console.log(`  PASS RATE: ${passRate}%`);
    }
    console.log('');
    console.log('════════════════════════════════════════════════════════════════');

    // List failed tests if any
    if (this.failedTests.length > 0) {
      console.log('');
      console.log('FAILED TESTS:');
      for (const test of this.failedTests) {
        console.log(`  Failed - ${test.number} - ${test.title}`);
        if (test.error) {
          console.log(`           Error: ${test.error.substring(0, 100)}`);
        }
      }
      console.log('');
    }

    console.log(`\nStatus: ${result.status.toUpperCase()}`);
    console.log('');
  }
}

export default ConciseReporter;
