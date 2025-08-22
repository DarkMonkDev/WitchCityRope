#!/bin/bash
# launch-chrome-wsl.sh - Redirects to the universal launcher
# This script now simply calls the universal launcher to ensure consistency

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$SCRIPT_DIR/launch-chrome-universal.sh" "$@"