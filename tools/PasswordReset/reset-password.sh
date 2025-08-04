#!/bin/bash

# Password Reset Tool for WitchCityRope
# Usage: ./reset-password.sh <email> <new-password>

if [ $# -ne 2 ]; then
    echo "Usage: $0 <email> <new-password>"
    echo "Example: $0 admin@witchcityrope.com NewPassword123!"
    exit 1
fi

EMAIL="$1"
PASSWORD="$2"

# Change to the tool directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"

# Run the password reset tool
echo "Resetting password for user: $EMAIL"
dotnet run -- "$EMAIL" "$PASSWORD"