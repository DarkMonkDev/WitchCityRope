#!/bin/bash

# Setup script for Context7 MCP Server
# For WitchCityRope React Migration Project

echo "Setting up Context7 MCP Server..."

# Check Node.js version
NODE_VERSION=$(node -v | cut -d'v' -f2 | cut -d'.' -f1)
if [ "$NODE_VERSION" -lt 18 ]; then
    echo "Error: Node.js version 18+ required. Current version: $(node -v)"
    exit 1
fi

echo "Node.js version check passed: $(node -v)"

# Install Context7 MCP globally
echo "Installing Context7 MCP globally..."
npm install -g @upstash/context7-mcp@latest

# Check if installation was successful
if command -v context7-mcp &> /dev/null; then
    echo "✅ Context7 MCP installed successfully"
else
    echo "⚠️ Global command not found, but package may still work with npx"
fi

# Create MCP servers directory if it doesn't exist
mkdir -p /home/chad/mcp-servers/context7

# Create a local configuration file
cat > /home/chad/mcp-servers/context7/config.json << 'EOF'
{
  "name": "context7",
  "description": "Provides up-to-date documentation for libraries",
  "command": "npx",
  "args": ["-y", "@upstash/context7-mcp"],
  "transportType": "stdio",
  "enabled": true
}
EOF

echo "✅ Context7 configuration created at /home/chad/mcp-servers/context7/config.json"

# Test the installation
echo "Testing Context7 MCP..."
npx -y @upstash/context7-mcp --version 2>/dev/null || echo "Note: Version flag may not be supported"

echo ""
echo "Setup complete! Context7 MCP is ready to use."
echo ""
echo "Usage in prompts:"
echo "  - Add 'use context7' to any prompt to get latest documentation"
echo "  - Example: 'Create a React form with Mantine. use context7'"
echo ""
echo "Configuration location: /home/chad/mcp-servers/context7/config.json"