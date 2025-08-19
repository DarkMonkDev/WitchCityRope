# Technology Researcher Lessons Learned

## Research Methodology Standards (HIGH)
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

### Tags
#high #methodology #research #documentation

## Problem-Specific Research Patterns (CRITICAL)
**Date**: 2025-08-18
**Category**: Problem Solving
**Severity**: Critical

### Context
Researching specific technical issues like Mantine v7 placeholder visibility requires targeted search strategies and multiple information sources.

### What We Learned
- GitHub issues provide real-world developer experiences with specific problems
- Stack Overflow discussions reveal common workarounds and community solutions
- Official documentation may not cover edge cases or complex use scenarios
- Multiple search term variations are needed to find comprehensive solutions
- WebFetch tool is invaluable for getting detailed information from documentation pages
- Community discussions (GitHub discussions, Reddit) offer practical insights
- Browser-specific behavior differences need to be considered for CSS solutions
- CSS specificity conflicts are common with UI frameworks like Mantine

### Action Items
- [ ] SEARCH GitHub issues for reported problems and solutions
- [ ] CHECK Stack Overflow for community workarounds and discussions
- [ ] USE multiple search term variations to capture different perspectives
- [ ] FETCH official documentation pages for detailed technical information
- [ ] EVALUATE browser compatibility for CSS-based solutions
- [ ] CONSIDER CSS specificity conflicts when recommending styling approaches
- [ ] DOCUMENT implementation code examples for immediate use
- [ ] ASSESS performance impact of different solution approaches

### Tags
#critical #problem-solving #search-strategy #css #mantine

## Platform-Specific Evaluation Criteria (CRITICAL)
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

### Tags
#critical #platform-specific #safety #privacy #mobile

## Research Documentation Standards (HIGH)
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

### Tags
#high #documentation #standards #decision-support

## CSS Framework Integration Research Excellence (HIGH)
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
- Risk assessment prevents future maintenance issues

### Action Items
- [ ] PRIORITIZE CSS-only solutions for visual enhancements when possible
- [ ] RESEARCH CSS specificity conflicts with UI framework internal styles
- [ ] PROVIDE working code examples in research recommendations
- [ ] QUANTIFY performance impact in measurable terms (bytes, runtime cost)
- [ ] DOCUMENT browser compatibility requirements and testing needs
- [ ] ASSESS long-term maintenance implications of different approaches
- [ ] VALIDATE solutions against existing project architecture (CSS modules)

**Tags**: #high #css #mantine #performance #implementation

---

### UI Framework Issue Resolution Patterns - 2025-08-18

**Context**: Successfully resolved Mantine v7 placeholder visibility issues through comprehensive research and multiple solution validation.

**What We Learned**:
- **Research Existing Solutions First**: Always check GitHub issues, Stack Overflow, and community discussions before creating custom solutions
- **CSS Specificity Conflicts**: Common with component libraries - require understanding framework's internal class structure
- **Multi-Selector Approach**: When CSS isn't working as expected, check multiple selector approaches (class, type, data attributes)
- **Password Input Variations**: Different input types may have different internal structures requiring additional CSS targeting
- **Performance vs Maintenance**: CSS-only solutions often outperform React state management for simple visual behaviors

**Research Process That Worked**:
1. **GitHub Issues Search**: "Mantine placeholder visibility" revealed community discussions
2. **Stack Overflow Research**: Multiple search term variations captured different perspectives
3. **Official Documentation**: Mantine Styles API provided framework-specific customization patterns
4. **Browser Testing**: Validated CSS selector effectiveness across different input types
5. **Implementation Examples**: Provided working code for immediate adoption

**Critical Discovery**:
```css
/* Insufficient - only targets basic inputs */
.form input::placeholder { opacity: 0; }

/* Complete - targets all Mantine input variations */
.enhancedInput :global(.mantine-TextInput-input)::placeholder,
.enhancedInput :global(.mantine-PasswordInput-input)::placeholder,
.enhancedInput :global(.mantine-Textarea-input)::placeholder {
  opacity: 0 !important;
}
```

**Action Items**:
- [ ] RESEARCH existing solutions before creating custom implementations
- [ ] CHECK multiple information sources (GitHub, Stack Overflow, docs) for comprehensive understanding
- [ ] VALIDATE CSS solutions across all relevant input types
- [ ] QUANTIFY performance impact when recommending solutions
- [ ] PROVIDE working implementation examples with research recommendations
- [ ] CONSIDER framework-specific patterns when researching UI library issues

**Impact**: Prevents reinventing solutions and ensures comprehensive coverage of technical requirements.

**Tags**: #research-first #ui-frameworks #css-specificity #mantine #implementation-examples #performance-analysis

---
*This file is maintained by the technology researcher agent. Add new lessons immediately when discovered, remove outdated entries as needed.*
*Last updated: 2025-08-18 - Added UI framework issue resolution patterns*