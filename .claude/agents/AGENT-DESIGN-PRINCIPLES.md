# Agent Design Principles - MANDATORY READING

## 🚨 CRITICAL: Tool Restriction is THE Primary Enforcement Mechanism

### The Golden Rule
**If an agent has a tool, it WILL use it instead of delegating.**

No amount of instructions, warnings, or ALL CAPS text will prevent an agent from using available tools. The ONLY reliable way to enforce delegation is to physically remove the tools that would allow direct implementation.

## Coordinator Agent Design Pattern

### Coordinators MUST ONLY Have These Tools:
- `TodoWrite` - For tracking progress
- `Task` - For delegating to other agents  
- `Bash` - For running specific commands (if needed)

### Coordinators MUST NOT Have These Tools:
- ❌ `Read` - Prevents them from looking at code
- ❌ `Edit/Write/MultiEdit` - Prevents them from modifying code
- ❌ `Grep/Glob` - Prevents them from searching for code
- ❌ `WebSearch/WebFetch` - Prevents them from researching solutions

### Why This Matters
Research shows that AI agents will naturally take the path of least resistance. If they CAN do something directly, they WILL, regardless of instructions telling them not to. This is not a bug - it's emergent behavior.

## Implementation Agent Design Pattern

### Implementation Agents SHOULD Have:
- `Read` - To understand existing code
- `Edit/Write/MultiEdit` - To modify code
- `Grep/Glob` - To find relevant files
- `Bash` - To test their changes locally

### Implementation Agents SHOULD NOT Have:
- ❌ `Task` - They should not delegate further
- ❌ `TodoWrite` - Leave tracking to coordinators

## Testing the Design

### Before Deploying Any Agent:
1. **Tool Audit**: List every tool the agent has access to
2. **Capability Check**: For each tool, ask "Does this enable direct work?"
3. **Delegation Test**: If yes, and agent should delegate, REMOVE THE TOOL
4. **Instruction Review**: Ensure instructions match available tools

### Red Flags:
- Coordinator with Read/Edit tools
- Instructions saying "don't use X tool" when X is available
- Multiple agents with overlapping tool sets
- Vague delegation boundaries

## Enforcement Through Architecture, Not Instructions

### ❌ WRONG Approach:
```yaml
tools: Read, Edit, Task, Bash
instructions: "You MUST NOT edit files directly. Delegate to other agents."
```
**Result**: Agent will edit files directly.

### ✅ CORRECT Approach:
```yaml
tools: Task, Bash
instructions: "You coordinate work by delegating to specialized agents."
```
**Result**: Agent cannot edit files even if it wanted to.

## Lessons from Production Failures

### Failure Case 1: Test-Fix-Coordinator
- **Had**: Read, Grep, Glob, Edit tools
- **Behavior**: Read code and attempted fixes directly
- **Fix**: Removed Read, Grep, Glob, Edit tools
- **Result**: Now properly delegates all fixes

### Failure Case 2: Orchestrator
- **Had**: All tools available
- **Behavior**: Implemented features directly
- **Fix**: Restricted to TodoWrite, Task, Read, Bash only
- **Result**: Now properly delegates implementation

## Design Checklist for New Agents

- [ ] Define agent's single responsibility
- [ ] List minimum tools needed for that responsibility
- [ ] Remove ALL other tools
- [ ] Write instructions that match available tools
- [ ] Test with a delegation scenario
- [ ] Verify agent requests delegation when it lacks tools
- [ ] Document tool restrictions prominently in agent definition

## The Three Laws of Agent Tools

1. **An agent will use any tool it has access to**
2. **No instruction can reliably prevent tool usage**
3. **Tool restriction is the only reliable control**

## Implementation Guidelines

### When Creating a Coordinator:
1. Start with ONLY `Task` and `TodoWrite`
2. Add `Bash` only if needed for specific commands
3. NEVER add Read, Edit, Write, Grep, or Glob
4. Test that it delegates when encountering work

### When Creating a Worker:
1. Give it ALL tools needed for its specific task
2. Do NOT give it `Task` (no sub-delegation)
3. Make its scope narrow and specific
4. Test that it completes work independently

### When Updating Existing Agents:
1. Audit current tool list
2. Remove any tools that enable unwanted behavior
3. Update instructions to match new tool constraints
4. Test the agent cannot perform restricted actions

## Remember

**Instructions guide behavior. Tools determine capability.**

An agent without Edit tools cannot edit files, no matter what the user asks.
An agent with Edit tools will edit files, no matter what the instructions say.

Design your agents accordingly.