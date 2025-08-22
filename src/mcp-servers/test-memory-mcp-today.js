const { spawn } = require('child_process');
const readline = require('readline');

// Create interface for reading stdin
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

// Helper function to send JSON-RPC request
function sendRequest(proc, method, params = {}) {
  const request = {
    jsonrpc: '2.0',
    method,
    params,
    id: Date.now()
  };
  
  const message = JSON.stringify(request);
  const contentLength = Buffer.byteLength(message, 'utf8');
  
  proc.stdin.write(`Content-Length: ${contentLength}\r\n\r\n${message}`);
}

// Helper function to parse response
function parseResponse(data) {
  const lines = data.toString().split('\r\n');
  
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].startsWith('Content-Length:')) {
      const contentLength = parseInt(lines[i].split(':')[1].trim());
      const jsonStart = i + 2; // Skip empty line
      
      if (jsonStart < lines.length) {
        try {
          const jsonData = lines.slice(jsonStart).join('\r\n');
          const parsed = JSON.parse(jsonData.substring(0, contentLength));
          return parsed;
        } catch (e) {
          // Continue parsing
        }
      }
    }
  }
  return null;
}

async function testMemoryServer() {
  console.log('Starting Memory MCP Server test...\n');
  
  // Start the Memory MCP server
  const memoryServer = spawn('npx', ['-y', '@modelcontextprotocol/server-memory'], {
    stdio: ['pipe', 'pipe', 'pipe']
  });
  
  let responseBuffer = '';
  
  memoryServer.stdout.on('data', (data) => {
    responseBuffer += data.toString();
    const response = parseResponse(responseBuffer);
    
    if (response) {
      console.log('Response received:', JSON.stringify(response, null, 2));
      responseBuffer = '';
    }
  });
  
  memoryServer.stderr.on('data', (data) => {
    console.error('Server error:', data.toString());
  });
  
  // Wait a bit for server to start
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Initialize the connection
  console.log('Initializing connection...');
  sendRequest(memoryServer, 'initialize', {
    protocolVersion: '2024-11-05',
    capabilities: {},
    clientInfo: {
      name: 'test-client',
      version: '1.0.0'
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 1000));
  
  // Test 1: Create an entity about today's browser MCP solution
  console.log('\n1. Creating entity about browser MCP solution...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'create_entities',
    arguments: {
      entities: [{
        name: 'Browser MCP Solution 2025-06-29',
        entityType: 'technical_solution',
        observations: [
          'Successfully implemented browser automation through WSL2 environment',
          'Uses Chrome in headless mode with remote debugging on port 9222',
          'Implements a PowerShell bridge script (browser-bridge.ps1) for WSL-to-Windows communication',
          'Provides persistent browser server management with automatic startup',
          'Supports multiple browser automation tools: navigate, screenshot, click, type, etc.',
          'Tested and verified working on Windows 11 with WSL2 Ubuntu'
        ]
      }]
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Test 2: Add observations about PowerShell bridge discovery
  console.log('\n2. Adding observations about PowerShell bridge discovery...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'add_observations',
    arguments: {
      entityName: 'Browser MCP Solution 2025-06-29',
      observations: [
        'PowerShell bridge enables WSL2 processes to launch Windows Chrome instances',
        'Bridge script handles path translation between WSL and Windows filesystems',
        'Discovered that PowerShell.exe can be called directly from WSL with proper escaping',
        'Bridge solution eliminates need for complex X11 forwarding or WSLg setup'
      ]
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Test 3: Create a related entity for the PowerShell bridge
  console.log('\n3. Creating PowerShell bridge entity...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'create_entities',
    arguments: {
      entities: [{
        name: 'PowerShell Bridge for WSL',
        entityType: 'component',
        observations: [
          'Script location: /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-bridge.ps1',
          'Enables launching Windows executables from WSL environment',
          'Handles path conversion using wslpath utility',
          'Key discovery: PowerShell.exe accessible from WSL at /mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe'
        ]
      }]
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Test 4: Create relationship between entities
  console.log('\n4. Creating relationship between entities...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'create_relations',
    arguments: {
      relations: [{
        from: 'Browser MCP Solution 2025-06-29',
        to: 'PowerShell Bridge for WSL',
        relationType: 'depends_on'
      }]
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Test 5: Retrieve stored information
  console.log('\n5. Retrieving all stored information...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'read_graph',
    arguments: {}
  });
  
  await new Promise(resolve => setTimeout(resolve, 2000));
  
  // Test 6: Search for specific nodes
  console.log('\n6. Searching for browser-related nodes...');
  sendRequest(memoryServer, 'tools/call', {
    name: 'search_nodes',
    arguments: {
      query: 'browser'
    }
  });
  
  await new Promise(resolve => setTimeout(resolve, 3000));
  
  // Cleanup
  console.log('\nTest completed. Press Ctrl+C to exit.');
  
  process.on('SIGINT', () => {
    console.log('\nShutting down server...');
    memoryServer.kill();
    process.exit(0);
  });
}

// Run the test
testMemoryServer().catch(console.error);