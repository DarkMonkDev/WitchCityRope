#!/bin/bash

# Install ngrok for webhook testing
# This script can be used across multiple projects

set -e

echo "Installing ngrok for webhook testing..."

# Detect OS
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="linux"
    if [[ $(uname -m) == "x86_64" ]]; then
        ARCH="amd64"
    elif [[ $(uname -m) == "aarch64" ]]; then
        ARCH="arm64"
    else
        ARCH="386"
    fi
elif [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="darwin"
    if [[ $(uname -m) == "arm64" ]]; then
        ARCH="arm64"
    else
        ARCH="amd64"
    fi
else
    echo "Unsupported OS: $OSTYPE"
    exit 1
fi

NGROK_URL="https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-${PLATFORM}-${ARCH}.tgz"

# Create local bin directory if it doesn't exist
mkdir -p ~/bin

# Download and extract ngrok
echo "Downloading ngrok for ${PLATFORM}-${ARCH}..."
cd /tmp
wget -q --show-progress "$NGROK_URL" -O ngrok.tgz
tar xzf ngrok.tgz

# Move to user's bin directory
mv ngrok ~/bin/
chmod +x ~/bin/ngrok
rm ngrok.tgz

# Add ~/bin to PATH if not already there
if ! echo "$PATH" | grep -q "$HOME/bin"; then
    echo ""
    echo "Adding ~/bin to PATH in ~/.bashrc..."
    echo 'export PATH="$HOME/bin:$PATH"' >> ~/.bashrc
    echo ""
    echo "⚠️  Please run: source ~/.bashrc"
    echo "   Or restart your terminal for PATH changes to take effect"
fi

# Verify installation
if ~/bin/ngrok version > /dev/null 2>&1; then
    echo ""
    echo "✅ ngrok installed successfully!"
    ~/bin/ngrok version
    echo ""
    echo "Usage examples:"
    echo "  ngrok http 5653                    # Expose local port 5653"
    echo "  ngrok http 5173                    # Expose React dev server"
    echo "  ngrok http --host-header=rewrite 5653  # With host header rewrite"
    echo ""
    echo "For PayPal webhooks:"
    echo "  1. Start your API: dotnet run --project apps/api"
    echo "  2. Start ngrok: ngrok http 5653"
    echo "  3. Copy the https URL (e.g., https://abc123.ngrok-free.app)"
    echo "  4. Use webhook URL: https://abc123.ngrok-free.app/api/webhooks/paypal"
else
    echo "❌ Installation failed. Please check the error messages above."
    exit 1
fi