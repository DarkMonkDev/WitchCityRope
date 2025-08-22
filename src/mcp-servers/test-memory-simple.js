const { Client } = require('@modelcontextprotocol/sdk/client/index.js');
const { StdioClientTransport } = require('@modelcontextprotocol/sdk/client/stdio.js');
const { spawn } = require('child_process');

async function testMemoryMCP() {
  console.log('Starting Memory MCP Server test...\n');
  
  try {
    // Spawn the Memory MCP server
    const serverProcess = spawn('npx', ['-y', '@modelcontextprotocol/server-memory'], {
      stdio: ['pipe', 'pipe', 'pipe'],
      env: { ...process.env }
    });

    // Create transport and client
    const transport = new StdioClientTransport({
      command: 'npx',
      args: ['-y', '@modelcontextprotocol/server-memory'],
      env: process.env
    });

    const client = new Client({
      name: 'memory-test-client',
      version: '1.0.0'
    }, {
      capabilities: {}
    });

    // Connect the client
    await client.connect(transport);
    console.log('Connected to Memory MCP server\n');

    // List available tools
    const tools = await client.listTools();
    console.log('Available tools:', tools.tools.map(t => t.name).join(', '), '\n');

    // Test 1: Create entity about browser MCP solution
    console.log('1. Creating entity about browser MCP solution...');
    const createResult = await client.callTool('create_entities', {
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
    });
    console.log('Create result:', JSON.stringify(createResult, null, 2), '\n');

    // Test 2: Add observations about PowerShell bridge
    console.log('2. Adding observations about PowerShell bridge discovery...');
    const addObsResult = await client.callTool('add_observations', {
      entityName: 'Browser MCP Solution 2025-06-29',
      observations: [
        'PowerShell bridge enables WSL2 processes to launch Windows Chrome instances',
        'Bridge script handles path translation between WSL and Windows filesystems',
        'Discovered that PowerShell.exe can be called directly from WSL with proper escaping',
        'Bridge solution eliminates need for complex X11 forwarding or WSLg setup'
      ]
    });
    console.log('Add observations result:', JSON.stringify(addObsResult, null, 2), '\n');

    // Test 3: Create PowerShell bridge entity
    console.log('3. Creating PowerShell bridge entity...');
    const bridgeResult = await client.callTool('create_entities', {
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
    });
    console.log('Bridge entity result:', JSON.stringify(bridgeResult, null, 2), '\n');

    // Test 4: Create relationship
    console.log('4. Creating relationship between entities...');
    const relationResult = await client.callTool('create_relations', {
      relations: [{
        from: 'Browser MCP Solution 2025-06-29',
        to: 'PowerShell Bridge for WSL',
        relationType: 'depends_on'
      }]
    });
    console.log('Relation result:', JSON.stringify(relationResult, null, 2), '\n');

    // Test 5: Read entire graph
    console.log('5. Reading entire knowledge graph...');
    const graphResult = await client.callTool('read_graph', {});
    console.log('Graph contents:', JSON.stringify(graphResult, null, 2), '\n');

    // Test 6: Search for browser-related nodes
    console.log('6. Searching for browser-related nodes...');
    const searchResult = await client.callTool('search_nodes', {
      query: 'browser'
    });
    console.log('Search results:', JSON.stringify(searchResult, null, 2), '\n');

    // Close the connection
    await client.close();
    console.log('Test completed successfully!');

  } catch (error) {
    console.error('Test failed:', error);
  }

  process.exit(0);
}

// Run the test
testMemoryMCP();