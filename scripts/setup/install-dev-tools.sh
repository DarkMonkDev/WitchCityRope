#!/bin/bash

# This script installs development tools on Ubuntu
# Run with: bash install-dev-tools.sh

echo "=== Development Tools Installation Script ==="
echo "This script will install:"
echo "- .NET SDK 9.0"
echo "- PostgreSQL 16"
echo "(Node.js 22.17.0 and npm 10.9.2 are already installed)"
echo ""
read -p "Press Enter to continue or Ctrl+C to cancel..."

# Update package list
echo "Updating package list..."
sudo apt update

# Install .NET SDK 9.0
echo ""
echo "=== Installing .NET SDK 9.0 ==="

# Get Ubuntu version
source /etc/os-release
ubuntu_version=$VERSION_ID

# Download and install Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$ubuntu_version/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list again to include Microsoft repository
sudo apt update

# Install .NET SDK 9.0
sudo apt install -y dotnet-sdk-9.0

# Install PostgreSQL 16
echo ""
echo "=== Installing PostgreSQL 16 ==="

# Install required packages
sudo apt install -y wget ca-certificates

# Create the repository configuration
sudo sh -c 'echo "deb https://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main" > /etc/apt/sources.list.d/pgdg.list'

# Import the repository signing key
wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -

# Update package list to include PostgreSQL repository
sudo apt update

# Install PostgreSQL 16
sudo apt install -y postgresql-16 postgresql-client-16

# Verify installations
echo ""
echo "=== Verifying Installations ==="
echo ""

echo "Checking .NET SDK:"
if command -v dotnet &> /dev/null; then
    dotnet --version
else
    echo "❌ .NET SDK installation failed"
fi

echo ""
echo "Checking PostgreSQL:"
if command -v psql &> /dev/null; then
    psql --version
else
    echo "❌ PostgreSQL installation failed"
fi

echo ""
echo "Checking Node.js (already installed):"
node --version

echo ""
echo "Checking npm (already installed):"
npm --version

echo ""
echo "=== Installation Complete ==="
echo ""
echo "PostgreSQL service status:"
sudo systemctl status postgresql --no-pager

echo ""
echo "To start using PostgreSQL:"
echo "  sudo -u postgres psql"
echo ""
echo "To create a new database user:"
echo "  sudo -u postgres createuser --interactive"