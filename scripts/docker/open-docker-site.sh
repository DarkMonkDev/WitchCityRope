#!/bin/bash
# Simple script to open the Docker site

echo "Opening WitchCityRope in Docker..."
echo "Web UI: http://localhost:5651"
echo "API: http://localhost:5653"

# Open in default browser
xdg-open http://localhost:5651 2>/dev/null || open http://localhost:5651 2>/dev/null || echo "Please open http://localhost:5651 in your browser"