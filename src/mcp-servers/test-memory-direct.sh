#!/bin/bash

echo "Testing Memory MCP Server directly..."
echo "======================================="

# Function to call MCP tool
call_mcp_tool() {
    local tool_name=$1
    local args=$2
    
    echo -e "\nCalling tool: $tool_name"
    echo "Arguments: $args"
    
    # Create the request
    local request=$(cat <<EOF
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "$tool_name",
    "arguments": $args
  },
  "id": 1
}
EOF
)
    
    # Send request to memory server
    echo "$request" | npx -y @modelcontextprotocol/server-memory
}

# Test 1: Create entity about browser MCP solution
echo -e "\n1. Creating entity about browser MCP solution..."
call_mcp_tool "create_entities" '{
  "entities": [{
    "name": "Browser MCP Solution 2025-06-29",
    "entityType": "technical_solution",
    "observations": [
      "Successfully implemented browser automation through WSL2 environment",
      "Uses Chrome in headless mode with remote debugging on port 9222",
      "Implements a PowerShell bridge script for WSL-to-Windows communication",
      "Provides persistent browser server management with automatic startup",
      "Supports multiple browser automation tools",
      "Tested and verified working on Windows 11 with WSL2 Ubuntu"
    ]
  }]
}'

# Test 2: Add observations
echo -e "\n2. Adding observations about PowerShell bridge discovery..."
call_mcp_tool "add_observations" '{
  "entityName": "Browser MCP Solution 2025-06-29",
  "observations": [
    "PowerShell bridge enables WSL2 processes to launch Windows Chrome instances",
    "Bridge script handles path translation between WSL and Windows filesystems",
    "Discovered that PowerShell.exe can be called directly from WSL with proper escaping",
    "Bridge solution eliminates need for complex X11 forwarding or WSLg setup"
  ]
}'

# Test 3: Create PowerShell bridge entity
echo -e "\n3. Creating PowerShell bridge entity..."
call_mcp_tool "create_entities" '{
  "entities": [{
    "name": "PowerShell Bridge for WSL",
    "entityType": "component",
    "observations": [
      "Script location: browser-bridge.ps1",
      "Enables launching Windows executables from WSL environment",
      "Handles path conversion using wslpath utility",
      "Key discovery: PowerShell.exe accessible from WSL"
    ]
  }]
}'

# Test 4: Create relationship
echo -e "\n4. Creating relationship between entities..."
call_mcp_tool "create_relations" '{
  "relations": [{
    "from": "Browser MCP Solution 2025-06-29",
    "to": "PowerShell Bridge for WSL",
    "relationType": "depends_on"
  }]
}'

# Test 5: Read graph
echo -e "\n5. Reading entire knowledge graph..."
call_mcp_tool "read_graph" '{}'

# Test 6: Search nodes
echo -e "\n6. Searching for browser-related nodes..."
call_mcp_tool "search_nodes" '{
  "query": "browser"
}'

echo -e "\nTest completed!"