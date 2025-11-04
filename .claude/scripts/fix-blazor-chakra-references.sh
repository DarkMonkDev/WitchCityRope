#!/bin/bash

# Script to fix Blazor and Chakra references in agent files
# Date: 2025-11-04

echo "Fixing Blazor and Chakra references in agent files..."

# Fix functional-spec.md
echo "Fixing functional-spec.md..."
sed -i 's/### MUST Use/### Technology Stack\n\n### MUST Use/g' /home/chad/repos/witchcityrope/.claude/agents/planning/functional-spec.md
sed -i 's/- ✅ Blazor Server (NOT WebAssembly)/- ✅ React 18 + TypeScript (NOT Vue or Angular)/g' /home/chad/repos/witchcityrope/.claude/agents/planning/functional-spec.md
sed -i 's/- ✅ Syncfusion (NOT MudBlazor)/- ✅ Mantine v7 (NOT Material-UI or Chakra)/g' /home/chad/repos/witchcityrope/.claude/agents/planning/functional-spec.md

# Fix librarian.md
echo "Fixing librarian.md..."
sed -i 's/### Blazor Developers/### React Developers/g' /home/chad/repos/witchcityrope/.claude/agents/utility/librarian.md

# Fix backend-developer.md
echo "Fixing backend-developer.md..."
sed -i 's/Blazor Server auth architecture/React SPA authentication architecture/g' /home/chad/repos/witchcityrope/.claude/agents/implementation/backend-developer.md
sed -i 's/SignInManager directly in Blazor components/SignInManager directly (use API endpoints)/g' /home/chad/repos/witchcityrope/.claude/agents/implementation/backend-developer.md

# Fix test-developer.md
echo "Fixing test-developer.md..."
sed -i 's/, bUnit for Blazor,/, Vitest + Testing Library for React,/g' /home/chad/repos/witchcityrope/.claude/agents/testing/test-developer.md
sed -i 's/- bUnit for Blazor component testing/- Vitest + Testing Library for React component testing/g' /home/chad/repos/witchcityrope/.claude/agents/testing/test-developer.md
sed -i 's/### 3. Blazor Component Tests/### 3. React Component Tests/g' /home/chad/repos/witchcityrope/.claude/agents/testing/test-developer.md
sed -i 's/- Use bUnit TestContext for Blazor components/- Use Testing Library for React components/g' /home/chad/repos/witchcityrope/.claude/agents/testing/test-developer.md

# Fix code-reviewer.md
echo "Fixing code-reviewer.md..."
sed -i 's/, Blazor,/, React,/g' /home/chad/repos/witchcityrope/.claude/agents/testing/code-reviewer.md
sed -i 's/#### Blazor Components/#### React Components/g' /home/chad/repos/witchcityrope/.claude/agents/testing/code-reviewer.md
sed -i 's/### Blazor-Specific/### React-Specific/g' /home/chad/repos/witchcityrope/.claude/agents/testing/code-reviewer.md
sed -i 's/Direct SignInManager use in Blazor/Direct SignInManager use (use API endpoints)/g' /home/chad/repos/witchcityrope/.claude/agents/testing/code-reviewer.md

# Fix react-developer.md (Chakra → Mantine)
echo "Fixing react-developer.md..."
sed -i 's/CSS modules or Chakra/CSS modules or Mantine/g' /home/chad/repos/witchcityrope/.claude/agents/development/react-developer.md
sed -i 's/Chakra UI component limitations or customization needs/Mantine v7 component limitations or customization needs/g' /home/chad/repos/witchcityrope/.claude/agents/development/react-developer.md

# Fix technology-researcher.md (Chakra → Mantine)
echo "Fixing technology-researcher.md..."
sed -i 's/Material-UI, Ant Design, Chakra UI/Material-UI, Ant Design, Mantine v7/g' /home/chad/repos/witchcityrope/.claude/agents/research/technology-researcher.md

echo ""
echo "✅ All references fixed!"
echo ""
echo "Fixed files:"
echo "  1. functional-spec.md"
echo "  2. librarian.md"
echo "  3. backend-developer.md"
echo "  4. test-developer.md"
echo "  5. code-reviewer.md"
echo "  6. react-developer.md"
echo "  7. technology-researcher.md"
echo ""
echo "Verification:"
echo "  Blazor references remaining:"
grep -r "Blazor" /home/chad/repos/witchcityrope/.claude/agents/ --include="*.md" | grep -v "lessons-learned" | wc -l
echo "  Chakra references remaining:"
grep -r "Chakra" /home/chad/repos/witchcityrope/.claude/agents/ --include="*.md" | wc -l
