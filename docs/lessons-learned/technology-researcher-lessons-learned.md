# Technology Researcher Lessons Learned

## 🚨 CRITICAL: WORKTREE COMPLIANCE - MANDATORY 🚨

### ALL WORK MUST BE IN THE SPECIFIED WORKTREE DIRECTORY

**VIOLATION = CATASTROPHIC FAILURE**

When given a Working Directory like:
`/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management`

**YOU MUST:**
- Write ALL files to paths within the worktree directory
- NEVER write to `/home/chad/repos/witchcityrope-react/` main repository
- ALWAYS use the full worktree path in file operations
- VERIFY you're in the correct directory before ANY file operation

**Example:**
- ✅ CORRECT: `/home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-24-events-management/docs/...`
- ❌ WRONG: `/home/chad/repos/witchcityrope-react/docs/...`

**Why This Matters:**
- Worktrees isolate feature branches
- Writing to main repo pollutes other branches
- Can cause merge conflicts and lost work
- BREAKS the entire development workflow

## 🚨 MANDATORY STARTUP PROCEDURE - READ FIRST 🚨

### Critical Architecture Documents (MUST READ BEFORE ANY WORK):
1. **Migration Architecture**: `/docs/architecture/react-migration/domain-layer-architecture.md`
2. **DTO Strategy**: `/docs/architecture/react-migration/DTO-ALIGNMENT-STRATEGY.md`
3. **Architecture Discovery Process**: `/docs/standards-processes/architecture-discovery-process.md`
4. **Migration Plan**: `/docs/architecture/react-migration/migration-plan.md`

### Validation Gates (MUST COMPLETE):
- [ ] Read all architecture documents above
- [ ] Check if solution already exists
- [ ] Reference existing patterns in your work
- [ ] NEVER create manual DTO interfaces (use NSwag)

### Technology Researcher Specific Rules:
- **BEFORE researching alternatives, check if solution exists in architecture docs**
- **If work involves DTOs, APIs, or types → Check NSwag implementation FIRST**
- **Document: 'Verified no existing solution in architecture docs: [list checked]'**
- **Research established patterns before suggesting new ones**

## 🚨 CRITICAL: FILE PLACEMENT RULES - ZERO TOLERANCE 🚨

### NEVER Create Files in Project Root
**VIOLATIONS = IMMEDIATE WORKFLOW FAILURE**

### Mandatory File Locations:
- **Research Scripts (.js, .ts, .sh)**: `/scripts/research/`
- **Technology Evaluation Tools**: `/scripts/evaluation/`
- **Benchmark Scripts**: `/scripts/benchmark/`
- **ADR Draft Files**: `/docs/architecture/adrs/drafts/`
- **Technology Comparison Files**: `/docs/architecture/research/`
- **POC Scripts**: `/scripts/poc/`

### Pre-Work Validation:
```bash
# Check for violations in project root
ls -la *.js *.ts *.sh research-*.* eval-*.* bench-*.* poc-*.* 2>/dev/null
# If ANY research files found in root = STOP and move to correct location
```

### Violation Response:
1. STOP all work immediately
2. Move files to correct locations
3. Update file registry
4. Continue only after compliance

### FORBIDDEN LOCATIONS:
- ❌ Project root for ANY research files
- ❌ Research scripts outside `/scripts/research/`
- ❌ POC files in random locations
- ❌ Technology evaluation tools outside proper structure

---

## Research Methodology Standards

**Date**: 2025-08-17
**Category**: Process
**Severity**: High

### Context
Establishing efficient and reliable technology research patterns for React migration decisions.

### What We Learned
- Always check official documentation first for accurate information
- Use context7 MCP for current library documentation and examples
- Quantitative comparison matrices provide clear decision support
- WitchCityRope-specific criteria must be weighted appropriately
- Confidence levels help stakeholders understand recommendation reliability

### Action Items
- [ ] START with official documentation for all technology evaluations
- [ ] USE context7 MCP when researching current libraries and frameworks
- [ ] CREATE quantitative comparison matrices for multi-option decisions
- [ ] WEIGHT evaluation criteria based on WitchCityRope's specific needs
- [ ] PROVIDE confidence levels with all recommendations

### Impact
Ensures research quality and reduces decision-making uncertainty for technical choices.

### Tags
#high #methodology #research #documentation

## Platform-Specific Evaluation Criteria

**Date**: 2025-08-17
**Category**: Evaluation
**Severity**: Critical

### Context
Technology choices for WitchCityRope must consider unique platform requirements including safety, privacy, and community needs.

### What We Learned
- Safety and privacy requirements are non-negotiable constraints
- Mobile-first design affects technology selection significantly
- Community values should influence technology adoption decisions
- Resource constraints (volunteer development) affect maintenance burden considerations
- Authentication architecture (httpOnly cookies) limits certain technology options

### Action Items
- [ ] PRIORITIZE safety and privacy implications in all evaluations
- [ ] EVALUATE mobile experience impact for every technology choice
- [ ] CONSIDER long-term maintenance burden for volunteer development team
- [ ] ALIGN technology choices with community values and mission
- [ ] VALIDATE compatibility with httpOnly cookie authentication pattern

### Impact
Prevents technology choices that conflict with WitchCityRope's core requirements and constraints.

### Tags
#critical #platform-specific #safety #privacy #mobile

## Research Documentation Standards

**Date**: 2025-08-17
**Category**: Documentation
**Severity**: High

### Context
Research documents must provide clear, actionable information for architecture decisions.

### What We Learned
- Executive summaries enable quick decision-making for stakeholders
- Structured comparison tables provide objective evaluation basis
- Implementation considerations prevent research-to-reality gaps
- Risk assessments enable proactive mitigation planning
- Source documentation enables verification and follow-up research

### Action Items
- [ ] INCLUDE executive summary with clear recommendation and confidence level
- [ ] CREATE structured comparison tables with weighted scoring
- [ ] DOCUMENT implementation path and integration considerations
- [ ] ASSESS risks with mitigation strategies for each recommendation
- [ ] REFERENCE all sources for verification and credibility

### Impact
Improves decision quality and reduces implementation risks through better documentation.

### Tags
#high #documentation #standards #decision-support

## UI Framework Issue Resolution Patterns

**Date**: 2025-08-18
**Category**: Technical Research
**Severity**: High

### Context
Successfully researched and solved complex Mantine v7 placeholder visibility issue using comprehensive multi-source research approach.

### What We Learned
- CSS-only solutions often outperform JavaScript state management for simple visual enhancements
- Mantine's Styles API provides powerful customization but requires understanding CSS specificity
- Focus-based CSS selectors (::placeholder, :focus, :focus-within) enable elegant conditional styling
- Browser compatibility testing is essential for CSS pseudo-element solutions
- Performance impact should be quantified (+200-400 bytes for CSS vs state management overhead)
- Implementation examples with actual code accelerate development adoption
- GitHub issues provide real-world developer experiences with specific problems
- Stack Overflow discussions reveal common workarounds and community solutions
- Multiple search term variations are needed to find comprehensive solutions

### Action Items
- [ ] RESEARCH existing solutions before creating custom implementations
- [ ] CHECK multiple information sources (GitHub, Stack Overflow, docs) for comprehensive understanding
- [ ] PRIORITIZE CSS-only solutions for visual enhancements when possible
- [ ] RESEARCH CSS specificity conflicts with UI framework internal styles
- [ ] PROVIDE working code examples in research recommendations
- [ ] QUANTIFY performance impact in measurable terms (bytes, runtime cost)
- [ ] VALIDATE solutions across all relevant input types and browsers
- [ ] CONSIDER framework-specific patterns when researching UI library issues

### Impact
Prevents reinventing solutions, ensures comprehensive coverage of technical requirements, and provides immediately actionable research outputs.

### References
- Mantine v7 Styles API Documentation
- GitHub Issues: Mantine placeholder visibility discussions
- Stack Overflow: CSS specificity with component libraries

### Tags
#high #css #mantine #performance #implementation #research-strategy

## Problem-Specific Research Strategy

**Date**: 2025-08-18
**Category**: Problem Solving
**Severity**: Critical

### Context
Researching specific technical issues requires targeted search strategies and multiple information sources to find comprehensive solutions.

### What We Learned
- GitHub issues provide real-world developer experiences with specific problems
- Official documentation may not cover edge cases or complex use scenarios
- WebFetch tool is invaluable for getting detailed information from documentation pages
- Community discussions (GitHub discussions, Reddit) offer practical insights
- Browser-specific behavior differences need to be considered for CSS solutions
- CSS specificity conflicts are common with UI frameworks and require framework-specific targeting

### Action Items
- [ ] SEARCH GitHub issues for reported problems and solutions first
- [ ] CHECK Stack Overflow for community workarounds and discussions
- [ ] USE multiple search term variations to capture different perspectives
- [ ] FETCH official documentation pages for detailed technical information
- [ ] EVALUATE browser compatibility for CSS-based solutions
- [ ] CONSIDER CSS specificity conflicts when recommending styling approaches
- [ ] DOCUMENT implementation code examples for immediate use
- [ ] ASSESS performance impact of different solution approaches

### Impact
Enables efficient problem resolution by leveraging existing community knowledge and avoiding reinvention of solutions.

### Tags
#critical #problem-solving #search-strategy #community-research #css-specificity

## API Contract Mismatch Prevention Research

**Date**: 2025-08-19
**Category**: Architecture Research
**Severity**: Critical

### Context
Research conducted in response to major frontend/backend API mismatch despite having fully documented and tested endpoints. Analysis of recent agent improvements and industry best practices for preventing such issues.

### What We Learned
- **Architecture Discovery Process IS effective** - when followed, prevents exactly the type of mismatch we experienced
- **Contract-first development patterns** are industry standard for preventing API mismatches
- **Multi-layered validation approach** (design-time + build-time + runtime) provides comprehensive protection
- **Enhanced agent workflow validation** can be implemented immediately with existing orchestrator system
- **NSwag implementation success** demonstrates our technical capability to execute automated solutions
- **Weighted comparison matrices** effectively evaluate complex architectural decisions with multiple stakeholders

### Key Research Patterns
- **Industry Research First**: OpenAPI contract-first development is well-established pattern
- **Multi-source Validation**: AWS, Azure, Google Cloud all emphasize gateway-level validation
- **Cost-Benefit Analysis**: Enhanced workflow provides 95% effectiveness at zero infrastructure cost
- **Implementation Phasing**: Immediate/short-term/long-term approach enables progressive improvement
- **Risk-based Prioritization**: Address highest-impact, lowest-complexity solutions first

### Action Items
- [ ] RESEARCH existing architecture before proposing solutions (critical validation)
- [ ] USE weighted comparison matrices for complex technology decisions involving multiple criteria
- [ ] ANALYZE recent improvements BEFORE assuming problems are unsolved
- [ ] IMPLEMENT phased approaches for complex architectural solutions
- [ ] LEVERAGE existing successful patterns (NSwag, orchestrator) rather than creating new systems
- [ ] DOCUMENT specific line references when citing architecture decisions
- [ ] CONSIDER industry best practices but adapt to organizational constraints and capabilities

### Implementation Insights
- **Agent workflow enhancements** can prevent architectural violations more effectively than runtime validation
- **Mandatory discovery phases** create forcing functions for proper research
- **Escalation processes** handle edge cases without breaking development flow
- **Quality gate checklists** ensure comprehensive coverage of all decision factors

### Impact
Provides framework for preventing costly architectural mismatches while building on successful existing patterns. Demonstrates research methodology for complex technology decisions involving multiple stakeholders and criteria.

### References
- OpenAPI Best Practices (learn.openapis.org)
- Contract Testing Patterns (dev.to TypeScript/OpenAPI integration)
- API Gateway Validation (AWS/Azure documentation)
- Internal Architecture Discovery Process documentation

### Tags
#critical #api-contracts #architecture #prevention #workflow-enhancement #research-methodology

## Approved Research Websites for .NET and C# Technologies

**Date**: 2025-08-22
**Category**: Research Sources
**Severity**: Critical

### Context
Curated list of high-quality, authoritative sources for .NET 9 and C# research. These websites provide current, accurate information from recognized experts in the .NET ecosystem.

### Approved Sources
1. **Milan Jovanović's Blog** (https://www.milanjovanovic.tech/)
   - Clean architecture, domain-driven design
   - .NET performance optimization
   - Minimal APIs and vertical slice architecture
   - Published weekly, always current with .NET releases

2. **CodeOpinion** (https://codeopinion.com/)
   - Event-driven architecture patterns
   - Microservices and distributed systems
   - CQRS and Event Sourcing
   - Practical architecture guidance

3. **Scott Brady's Articles** (https://www.scottbrady.io/articles)
   - Security and authentication best practices
   - OAuth 2.0 and OpenID Connect
   - ASP.NET Core security patterns
   - Identity and access management

4. **Microsoft .NET Blog** (https://devblogs.microsoft.com/dotnet/)
   - Official .NET announcements and releases
   - Performance improvements and benchmarks
   - New feature documentation
   - Best practices from the .NET team

### Research Guidelines
- **ALWAYS verify article date** - Must be published within last 6 months for .NET 9 topics
- **CHECK .NET version compatibility** - Ensure content applies to .NET 9, not older versions
- **USE search features** - Most sites have search fields to find specific topics
- **CROSS-REFERENCE sources** - Validate recommendations across multiple authorities
- **PRIORITIZE official Microsoft docs** for API specifications and breaking changes

### Search Strategies
- For minimal API patterns: Search Milan's blog and CodeOpinion
- For security/auth: Start with Scott Brady's articles
- For official features: Microsoft .NET Blog first
- For architecture patterns: CodeOpinion and Milan's blog

### Impact
Ensures research is based on current, authoritative sources from recognized .NET experts, preventing outdated or incorrect architectural decisions.

### Tags
#critical #research-sources #dotnet9 #approved-websites #authority

---

*This file is maintained by the technology researcher agent. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-22 - Added approved research websites for .NET and C# technologies*