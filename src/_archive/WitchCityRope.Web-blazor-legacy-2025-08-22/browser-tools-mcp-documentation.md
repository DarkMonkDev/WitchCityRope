# Browser Tools MCP Documentation

## Overview

The Browser Tools MCP (Model Context Protocol) is a browser automation extension that provides tools for interacting with web pages, running audits, and capturing browser state. This document compares its capabilities with traditional Puppeteer automation.

## Browser Tools MCP Features

### Available Tools (v1.2.0)

1. **getConsoleLogs**
   - Description: Check browser logs
   - Input Schema: `{"type": "object"}`
   - Use Case: Retrieve all console.log() outputs from the browser

2. **getConsoleErrors**
   - Description: Check browser console errors
   - Input Schema: `{"type": "object"}`
   - Use Case: Retrieve all console.error() outputs and JavaScript errors

3. **getNetworkErrors**
   - Description: Check network ERROR logs
   - Input Schema: `{"type": "object"}`
   - Use Case: Monitor failed network requests (4xx, 5xx status codes)

4. **getNetworkLogs**
   - Description: Check ALL network logs
   - Input Schema: `{"type": "object"}`
   - Use Case: Complete network activity monitoring including successful requests

5. **takeScreenshot**
   - Description: Take a screenshot of the current browser tab
   - Input Schema: `{"type": "object"}`
   - Use Case: Capture visual state of the page

6. **getSelectedElement**
   - Description: Get the selected element from the browser
   - Input Schema: `{"type": "object"}`
   - Use Case: Inspect currently focused or selected DOM element

7. **wipeLogs**
   - Description: Wipe all browser logs from memory
   - Input Schema: `{"type": "object"}`
   - Use Case: Clear accumulated logs for fresh monitoring

8. **runAccessibilityAudit**
   - Description: Run an accessibility audit on the current page
   - Input Schema: `{"type": "object", "properties": {}, "additionalProperties": false}`
   - Use Case: Check WCAG compliance and accessibility issues

9. **runPerformanceAudit**
   - Description: Run a performance audit on the current page
   - Input Schema: `{"type": "object", "properties": {}, "additionalProperties": false}`
   - Use Case: Analyze page load times, resource usage, and performance metrics

10. **runSEOAudit**
    - Description: Run an SEO audit on the current page
    - Input Schema: `{"type": "object", "properties": {}, "additionalProperties": false}`
    - Use Case: Check meta tags, structure, and SEO best practices

11. **runNextJSAudit**
    - Description: Next.js specific audit (no description provided)
    - Input Schema: `{"type": "object", "properties": {}, "additionalProperties": false}`
    - Use Case: Likely checks Next.js specific optimizations and best practices

12. **runDebuggerMode**
    - Description: Run debugger mode to debug an issue in our application
    - Input Schema: `{"type": "object"}`
    - Use Case: Enable debugging features for troubleshooting

13. **runAuditMode**
    - Description: Run audit mode to optimize for SEO, accessibility and performance
    - Input Schema: `{"type": "object"}`
    - Use Case: Comprehensive audit combining multiple checks

14. **runBestPracticesAudit**
    - Description: Run a best practices audit on the current page
    - Input Schema: `{"type": "object", "properties": {}, "additionalProperties": false}`
    - Use Case: Check general web development best practices

## Puppeteer MCP Features

### Available Tools (v0.1.0)

1. **puppeteer_navigate**
   - Properties: 
     - `url` (required): URL to navigate to
     - `launchOptions`: PuppeteerJS LaunchOptions
     - `allowDangerous`: Allow dangerous LaunchOptions
   - Use Case: Navigate to web pages with full browser control

2. **puppeteer_screenshot**
   - Properties:
     - `name` (required): Name for the screenshot
     - `selector`: CSS selector for element to screenshot
     - `width`: Width in pixels (default: 800)
     - `height`: Height in pixels (default: 600)
     - `encoded`: Capture as base64-encoded data URI
   - Use Case: Flexible screenshot capture with element targeting

3. **puppeteer_click**
   - Properties:
     - `selector` (required): CSS selector for element to click
   - Use Case: Interact with page elements

4. **puppeteer_fill**
   - Properties:
     - `selector` (required): CSS selector for input field
     - `value` (required): Value to fill
   - Use Case: Form automation and input field interaction

5. **puppeteer_select**
   - Properties:
     - `selector` (required): CSS selector for select element
     - `value` (required): Value to select
   - Use Case: Dropdown menu interaction

6. **puppeteer_hover**
   - Properties:
     - `selector` (required): CSS selector for element to hover
   - Use Case: Trigger hover states and tooltips

7. **puppeteer_evaluate**
   - Properties:
     - `script` (required): JavaScript code to execute
   - Use Case: Execute arbitrary JavaScript in browser context

### Resources
- `console://logs`: Browser console logs (text/plain)

## Comparison: Browser Tools MCP vs Puppeteer

### Browser Tools MCP Advantages
1. **Built-in Audit Capabilities**
   - Accessibility audits (WCAG compliance)
   - Performance audits
   - SEO audits
   - Best practices audits
   - Next.js specific audits

2. **Simplified Log Access**
   - Direct access to console logs and errors
   - Network error monitoring
   - Separate tools for different log types

3. **Audit Modes**
   - Dedicated debugging mode
   - Comprehensive audit mode combining multiple checks

4. **Focused on Analysis**
   - Designed for monitoring and auditing
   - Less focus on page interaction

### Puppeteer MCP Advantages
1. **Full Page Interaction**
   - Click, hover, fill forms
   - Select dropdown options
   - Navigate between pages

2. **Flexible Screenshot Options**
   - Element-specific screenshots
   - Custom dimensions
   - Base64 encoding option

3. **JavaScript Execution**
   - Execute arbitrary scripts
   - Full programmatic control

4. **Browser Launch Control**
   - Custom launch options
   - Headless/headful modes
   - Security controls

### Use Case Recommendations

**Use Browser Tools MCP when:**
- Running comprehensive audits (accessibility, performance, SEO)
- Monitoring console and network errors
- Taking simple full-page screenshots
- Debugging issues with built-in debugging mode
- Checking best practices and standards compliance

**Use Puppeteer MCP when:**
- Automating form submissions
- Interacting with page elements (clicking, hovering)
- Taking element-specific screenshots
- Executing custom JavaScript
- Navigating complex multi-page workflows
- Need fine-grained browser control

## Example Syntax (Based on MCP Protocol)

### Browser Tools MCP
```json
// Take a screenshot
{
  "tool": "takeScreenshot",
  "arguments": {}
}

// Run accessibility audit
{
  "tool": "runAccessibilityAudit",
  "arguments": {}
}

// Get console errors
{
  "tool": "getConsoleErrors",
  "arguments": {}
}
```

### Puppeteer MCP
```json
// Navigate to a page
{
  "tool": "puppeteer_navigate",
  "arguments": {
    "url": "http://localhost:5651/"
  }
}

// Take element screenshot
{
  "tool": "puppeteer_screenshot",
  "arguments": {
    "name": "header-screenshot",
    "selector": ".header",
    "width": 1200,
    "height": 400
  }
}

// Fill a form field
{
  "tool": "puppeteer_fill",
  "arguments": {
    "selector": "#username",
    "value": "testuser"
  }
}
```

## Integration Notes

1. **Server Initialization**: Both tools initialize as MCP servers and communicate via JSON-RPC protocol
2. **Protocol Version**: Both use protocol version "2024-11-05"
3. **Error Handling**: Both handle connection errors and provide logging
4. **Client Communication**: Standard MCP client-server message exchange

## Limitations and Considerations

### Browser Tools MCP
- Limited interaction capabilities (no click, fill, etc.)
- Focused on current tab/page
- No browser launch configuration
- Audit results format not specified in schema

### Puppeteer MCP
- No built-in audit capabilities
- Manual implementation needed for accessibility/performance checks
- Requires more complex scripts for analysis tasks
- Resource management needed for browser instances

## Conclusion

Browser Tools MCP and Puppeteer MCP serve complementary purposes:
- **Browser Tools MCP**: Excellent for auditing, monitoring, and analysis
- **Puppeteer MCP**: Superior for automation, interaction, and complex workflows

For comprehensive testing, consider using both tools:
1. Use Puppeteer to navigate and interact with the application
2. Use Browser Tools to run audits and capture logs
3. Combine results for complete test coverage