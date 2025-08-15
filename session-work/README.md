# Session Work Directory

## Purpose

This directory is for **temporary files** created during Claude or AI agent work sessions. 

## Structure

```
/session-work/
├── YYYY-MM-DD/          # Date-based subdirectories
│   ├── analysis/        # Analysis documents
│   ├── diagnostics/     # Diagnostic outputs
│   ├── temp-docs/       # Temporary documentation
│   └── README.md        # Session purpose and file list
└── README.md           # This file
```

## Rules

1. **ALL temporary files go here first**
2. **Create a dated subdirectory** for each session
3. **Include a README** in each session directory explaining:
   - Purpose of the session
   - List of files created
   - Which files should be promoted to permanent locations
4. **Review after 30 days** - Delete or archive old sessions

## File Lifecycle

1. **CREATE** → Files start here
2. **EVALUATE** → Determine if file should be permanent
3. **PROMOTE** → Move valuable files to proper documentation locations
4. **DELETE** → Remove temporary files after session

## Cleanup Schedule

- **Daily**: Review today's files at session end
- **Weekly**: Review week-old sessions
- **Monthly**: Delete sessions older than 30 days

## Example Session

```
/session-work/2025-01-20/
├── README.md                              # "CLAUDE.md reorganization session"
├── analysis/
│   └── claude-md-content-analysis.md      # Temporary analysis
├── temp-docs/
│   └── reorganization-plan.md             # Session planning doc
└── diagnostics/
    └── file-search-results.txt            # Command outputs
```

Remember: This prevents random files from cluttering the main project!