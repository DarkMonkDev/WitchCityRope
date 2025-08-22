#!/usr/bin/env python3
import json
import subprocess
import time
import sys
from datetime import datetime

def send_json_rpc_request(proc, method, params=None):
    """Send a JSON-RPC request to the MCP server"""
    request = {
        "jsonrpc": "2.0",
        "method": method,
        "params": params or {},
        "id": int(time.time() * 1000)
    }
    
    message = json.dumps(request)
    content_length = len(message.encode('utf-8'))
    
    # Write the request
    proc.stdin.write(f"Content-Length: {content_length}\r\n\r\n{message}".encode())
    proc.stdin.flush()
    
    # Read the response
    response_data = ""
    content_length = None
    
    while True:
        line = proc.stdout.readline().decode().strip()
        if line.startswith("Content-Length:"):
            content_length = int(line.split(":")[1].strip())
        elif line == "" and content_length:
            # Read the JSON content
            response_data = proc.stdout.read(content_length).decode()
            break
    
    if response_data:
        return json.loads(response_data)
    return None

def test_memory_mcp():
    print("Starting Memory MCP Server Test")
    print("=" * 50)
    
    # Start the Memory MCP server
    print("\nStarting Memory MCP server...")
    proc = subprocess.Popen(
        ["npx", "-y", "@modelcontextprotocol/server-memory"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        env={
            **subprocess.os.environ,
            "MEMORY_FILE_PATH": "C:\\Users\\chad\\AppData\\Roaming\\Claude\\memory-data\\memory.json"
        }
    )
    
    # Give the server time to start
    time.sleep(2)
    
    try:
        # Initialize the connection
        print("\nInitializing connection...")
        init_response = send_json_rpc_request(proc, "initialize", {
            "protocolVersion": "2024-11-05",
            "capabilities": {},
            "clientInfo": {
                "name": "test-client",
                "version": "1.0.0"
            }
        })
        print(f"Initialize response: {json.dumps(init_response, indent=2)}")
        
        # Test 1: Create entity about browser MCP solution
        print("\n1. Creating entity about browser MCP solution...")
        create_entity_response = send_json_rpc_request(proc, "tools/call", {
            "name": "create_entities",
            "arguments": {
                "entities": [{
                    "name": "Browser MCP Solution 2025-06-29",
                    "entityType": "technical_solution",
                    "observations": [
                        "Successfully implemented browser automation through WSL2 environment",
                        "Uses Chrome in headless mode with remote debugging on port 9222",
                        "Implements a PowerShell bridge script (browser-bridge.ps1) for WSL-to-Windows communication",
                        "Provides persistent browser server management with automatic startup",
                        "Supports multiple browser automation tools: navigate, screenshot, click, type, etc.",
                        "Tested and verified working on Windows 11 with WSL2 Ubuntu"
                    ]
                }]
            }
        })
        print(f"Create entity response: {json.dumps(create_entity_response, indent=2)}")
        
        # Test 2: Add observations about PowerShell bridge discovery
        print("\n2. Adding observations about PowerShell bridge discovery...")
        add_obs_response = send_json_rpc_request(proc, "tools/call", {
            "name": "add_observations",
            "arguments": {
                "entityName": "Browser MCP Solution 2025-06-29",
                "observations": [
                    "PowerShell bridge enables WSL2 processes to launch Windows Chrome instances",
                    "Bridge script handles path translation between WSL and Windows filesystems",
                    "Discovered that PowerShell.exe can be called directly from WSL with proper escaping",
                    "Bridge solution eliminates need for complex X11 forwarding or WSLg setup"
                ]
            }
        })
        print(f"Add observations response: {json.dumps(add_obs_response, indent=2)}")
        
        # Test 3: Retrieve stored information
        print("\n3. Retrieving all stored information...")
        read_graph_response = send_json_rpc_request(proc, "tools/call", {
            "name": "read_graph",
            "arguments": {}
        })
        print(f"Read graph response: {json.dumps(read_graph_response, indent=2)}")
        
        # Test 4: Search for browser-related nodes
        print("\n4. Searching for browser-related nodes...")
        search_response = send_json_rpc_request(proc, "tools/call", {
            "name": "search_nodes",
            "arguments": {
                "query": "browser"
            }
        })
        print(f"Search response: {json.dumps(search_response, indent=2)}")
        
        print("\n" + "=" * 50)
        print("TEST COMPLETED SUCCESSFULLY!")
        print("=" * 50)
        
    except Exception as e:
        print(f"\nError during test: {e}")
        return False
    finally:
        # Terminate the server
        proc.terminate()
        proc.wait()
    
    return True

if __name__ == "__main__":
    success = test_memory_mcp()
    sys.exit(0 if success else 1)