#!/usr/bin/env node

/**
 * Test script to verify Browser Tools MCP can connect to the persistent server
 */

const { Client } = require('@modelcontextprotocol/sdk/client/index.js');
const { StdioClientTransport } = require('@modelcontextprotocol/sdk/client/stdio.js');
const { spawn } = require('child_process');
const path = require('path');

// Configuration
const BROWSER_TOOLS_PATH = path.join(__dirname, '..', 'node_modules', '.bin', 'browser-tools-mcp');
const SERVER_PORT = process.env.BROWSER_SERVER_PORT || 9222;
const SERVER_HOST = process.env.BROWSER_SERVER_HOST || '127.0.0.1';

// Test function
async function testBrowserConnection() {
  console.log('Testing Browser Tools MCP Connection...');
  console.log(`Server: ${SERVER_HOST}:${SERVER_PORT}`);
  console.log(`Browser Tools Path: ${BROWSER_TOOLS_PATH}`);
  
  try {
    // Create transport for browser-tools-mcp
    const browserProcess = spawn('node', [BROWSER_TOOLS_PATH], {
      env: {
        ...process.env,
        BROWSER_SERVER_PORT: SERVER_PORT,
        BROWSER_SERVER_HOST: SERVER_HOST,
        CHROME_PATH: process.env.CHROME_PATH || '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe'
      }
    });

    const transport = new StdioClientTransport({
      command: 'node',
      args: [BROWSER_TOOLS_PATH],
      env: {
        ...process.env,
        BROWSER_SERVER_PORT: SERVER_PORT,
        BROWSER_SERVER_HOST: SERVER_HOST,
        CHROME_PATH: process.env.CHROME_PATH || '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe'
      }
    });

    // Create MCP client
    const client = new Client({
      name: 'browser-test-client',
      version: '1.0.0'
    }, {
      capabilities: {}
    });

    // Connect to the server
    console.log('Connecting to browser tools...');
    await client.connect(transport);
    console.log('✓ Connected successfully!');

    // List available tools
    const tools = await client.listTools();
    console.log('\nAvailable Browser Tools:');
    tools.tools.forEach(tool => {
      console.log(`  - ${tool.name}: ${tool.description}`);
    });

    // Test a simple browser operation
    console.log('\nTesting browser navigation...');
    try {
      const result = await client.callTool('browser_navigate', {
        url: 'https://example.com'
      });
      console.log('✓ Navigation successful!');
      console.log('Result:', JSON.stringify(result, null, 2));
    } catch (error) {
      console.error('✗ Navigation failed:', error.message);
    }

    // Test screenshot capability
    console.log('\nTesting screenshot capability...');
    try {
      const screenshot = await client.callTool('browser_screenshot', {});
      console.log('✓ Screenshot captured!');
      console.log('Screenshot data length:', screenshot.content[0].text.length);
    } catch (error) {
      console.error('✗ Screenshot failed:', error.message);
    }

    // Close connection
    await client.close();
    console.log('\n✓ All tests completed!');
    
    // Cleanup
    browserProcess.kill();
    process.exit(0);

  } catch (error) {
    console.error('\n✗ Test failed:', error);
    console.error('Error details:', error.stack);
    process.exit(1);
  }
}

// Run the test
console.log('Browser Tools MCP Connection Test');
console.log('=================================\n');

testBrowserConnection().catch(error => {
  console.error('Fatal error:', error);
  process.exit(1);
});