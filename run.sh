#!/bin/bash
# Quick launcher for WitchCityRope development
# Simply calls the main development startup script

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Make sure the dev-start script is executable
chmod +x "$SCRIPT_DIR/scripts/dev-start.sh"

# Run the development startup script
exec "$SCRIPT_DIR/scripts/dev-start.sh" "$@"