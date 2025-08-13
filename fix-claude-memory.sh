#!/bin/bash

# Fix for Claude CLI JavaScript heap out of memory error
# This script increases Node.js memory limits and provides a wrapper for Claude CLI

echo "🔧 Fixing Claude CLI memory configuration..."

# 1. Check current memory
TOTAL_MEM=$(free -m | awk '/^Mem:/{print $2}')
echo "📊 Total system memory: ${TOTAL_MEM}MB"

# 2. Calculate optimal heap size (75% of total memory, max 16GB)
HEAP_SIZE=$((TOTAL_MEM * 75 / 100))
if [ $HEAP_SIZE -gt 16384 ]; then
    HEAP_SIZE=16384
fi
echo "💾 Setting heap size to: ${HEAP_SIZE}MB"

# 3. Update shell configuration files
update_shell_config() {
    local file=$1
    if [ -f "$file" ]; then
        # Remove old NODE_OPTIONS if exists
        sed -i '/export NODE_OPTIONS.*max-old-space-size/d' "$file"
        
        # Add new NODE_OPTIONS
        echo "" >> "$file"
        echo "# Claude CLI memory fix - added $(date)" >> "$file"
        echo "export NODE_OPTIONS=\"--max-old-space-size=${HEAP_SIZE} --max-semi-space-size=256\"" >> "$file"
        echo "✅ Updated $file"
    fi
}

# Update bash and zsh configs
update_shell_config "$HOME/.bashrc"
update_shell_config "$HOME/.zshrc"
update_shell_config "$HOME/.profile"

# 4. Create Claude wrapper script
CLAUDE_WRAPPER="$HOME/.local/bin/claude-mem"
mkdir -p "$HOME/.local/bin"

cat > "$CLAUDE_WRAPPER" << 'EOF'
#!/bin/bash
# Claude CLI wrapper with memory optimization

# Get system memory
TOTAL_MEM=$(free -m | awk '/^Mem:/{print $2}')
HEAP_SIZE=$((TOTAL_MEM * 75 / 100))
if [ $HEAP_SIZE -gt 16384 ]; then
    HEAP_SIZE=16384
fi

# Set Node.js options
export NODE_OPTIONS="--max-old-space-size=${HEAP_SIZE} --max-semi-space-size=256"

# Additional V8 flags for better memory management
export NODE_OPTIONS="$NODE_OPTIONS --expose-gc --optimize-for-size"

# Run Claude with optimized settings
exec claude "$@"
EOF

chmod +x "$CLAUDE_WRAPPER"
echo "✅ Created wrapper script at $CLAUDE_WRAPPER"

# 5. Create alias for easy access
ALIAS_CMD="alias claude='$CLAUDE_WRAPPER'"
grep -q "alias claude=" "$HOME/.bashrc" || echo "$ALIAS_CMD" >> "$HOME/.bashrc"
grep -q "alias claude=" "$HOME/.zshrc" 2>/dev/null || echo "$ALIAS_CMD" >> "$HOME/.zshrc" 2>/dev/null

# 6. Apply changes to current session
export NODE_OPTIONS="--max-old-space-size=${HEAP_SIZE} --max-semi-space-size=256 --expose-gc --optimize-for-size"

echo ""
echo "✨ Memory fix applied!"
echo ""
echo "📝 Configuration summary:"
echo "  - Heap size: ${HEAP_SIZE}MB"
echo "  - Semi-space size: 256MB"
echo "  - Garbage collection: Exposed"
echo "  - Optimization: Size-optimized"
echo ""
echo "🚀 To use the fix:"
echo "  1. For current session: source ~/.bashrc"
echo "  2. For new sessions: Changes will apply automatically"
echo "  3. Alternative: Use 'claude-mem' instead of 'claude'"
echo ""
echo "💡 Tips to prevent memory issues:"
echo "  - Clear context periodically with: claude clear"
echo "  - Start new sessions for unrelated work"
echo "  - Close unused browser tabs in Claude web interface"
echo "  - Monitor memory with: free -h"