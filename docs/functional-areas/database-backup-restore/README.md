# Database Backup & Restore - WitchCityRope

<!-- Last Updated: 2025-11-24 -->
<!-- Version: 1.1 -->
<!-- Owner: DevOps/Backend Team -->
<!-- Status: Migration Planning -->

## Overview

Database backup and restore functionality for WitchCityRope PostgreSQL database, migrated from the account-automation repository. This feature provides automated database backups with DigitalOcean Spaces storage integration.

## Storage Structure

All backups are stored in DigitalOcean Spaces bucket `witchcityrope` with environment-specific folder isolation:

```
witchcityrope (bucket)
├── backups/
│   ├── local/              # Local development backups
│   │   └── backup-YYYY-MM-DD-HHMMSS.dump
│   ├── staging/            # Staging environment backups
│   │   └── backup-YYYY-MM-DD-HHMMSS.dump
│   └── production/         # Production environment backups
│       └── backup-YYYY-MM-DD-HHMMSS.dump
```

**Environment Isolation**:
- Each environment can only see and manage its own backups
- Prevents accidental cross-environment restoration
- Maintains clean separation between development, staging, and production

**Configuration**:
- Local: `BackupConfiguration__Spaces__FolderPrefix=backups/local` (docker-compose.dev.yml)
- Staging: `BackupConfiguration__Spaces__FolderPrefix=backups/staging` (docker-compose.staging.yml)
- Production: `BackupConfiguration__Spaces__FolderPrefix=backups/production` (docker-compose.production.yml)

## Purpose

Provide robust database backup and restore capabilities to ensure data safety, enable disaster recovery, and support development/testing workflows.

## Business Value

- **Data Protection**: Regular automated backups protect against data loss
- **Disaster Recovery**: Quick restoration capability minimizes downtime
- **Development Support**: Database snapshots for testing and development environments
- **Compliance**: Audit trail of backup operations
- **Peace of Mind**: Production-ready backup strategy

## Technology Stack

- **Database**: PostgreSQL (existing WitchCityRope database)
- **Storage**: DigitalOcean Spaces (S3-compatible object storage)
- **Source**: Account-automation repository (proven implementation)
- **Integration**: Admin Settings page

## Current Work

### 🆕 Migration from Account-Automation (2025-11-17)

**Status**: Phase 1 - Analysis & Planning (Started)

**Objective**: Migrate comprehensive database backup/restore feature from account-automation repository to WitchCityRope with DigitalOcean Spaces integration.

**Work Folder**: `/home/chad/repos/witchcityrope/docs/functional-areas/database-backup-restore/new-work/2025-11-17-migration-from-account-automation/`

**Key Documents**:
- **Analysis**: [Analysis Document](./new-work/2025-11-17-migration-from-account-automation/analysis/ANALYSIS.md) - Feature analysis, migration planning, technical considerations
- **Design**: Design specifications (TBD)
- **Implementation**: Implementation details (TBD)
- **Testing**: Test plans and results (TBD)

## Integration Points

### Admin Settings Page
- New "Database Backup" section in Admin Settings
- Manual backup trigger
- Backup history view
- Restore functionality

### DigitalOcean Spaces
- S3-compatible API for backup storage
- Secure credential management
- Automated retention policies

### PostgreSQL Database
- Database dump generation
- Point-in-time restore capability
- Minimal downtime procedures

## Features (Planned)

### Backup Operations
- [ ] Manual backup trigger from Admin UI
- [ ] Automated scheduled backups
- [ ] Backup verification
- [ ] Backup compression
- [ ] Metadata tracking (timestamp, size, database version)

### Restore Operations
- [ ] Browse available backups
- [ ] Point-in-time restore selection
- [ ] Restore preview/validation
- [ ] Rollback capability

### Management
- [ ] Backup retention policies
- [ ] Storage quota monitoring
- [ ] Backup health monitoring
- [ ] Admin notifications

### Security
- [ ] Encrypted backup storage
- [ ] Secure credential management (.NET User Secrets / Environment Variables)
- [ ] Audit logging
- [ ] Admin-only access control

## Architecture Considerations

### Migration Strategy
- Leverage existing account-automation implementation
- Adapt for WitchCityRope architecture patterns
- Maintain vertical slice organization
- Ensure Docker development compatibility

### Performance
- Asynchronous backup operations (no UI blocking)
- Efficient compression algorithms
- Incremental backup support (future enhancement)

### Reliability
- Retry logic for failed uploads
- Backup verification
- Multiple restore points maintained

## Success Criteria

- [ ] Manual backups execute successfully from Admin UI
- [ ] Backups stored in DigitalOcean Spaces
- [ ] Restore operations tested and validated
- [ ] Comprehensive documentation
- [ ] Admin user training materials

## Related Documentation

- **Backup Folder Separation Plan**: [2025-11-24-backup-folder-separation-plan.md](./investigation/2025-11-24-backup-folder-separation-plan.md) - Environment-specific storage implementation
- **Backup List Source Analysis**: [2025-11-23-backup-list-source-analysis.md](./investigation/2025-11-23-backup-list-source-analysis.md) - Investigation of backup list behavior
- **Secrets Management**: `/home/chad/repos/witchcityrope/docs/guides-setup/secrets-management-guide-2025-10-24.md`
- **Database Standards**: `/home/chad/repos/witchcityrope/docs/standards-processes/backend/database-migrations-guide.md`

## Timeline

**Estimated Duration**: 1-2 weeks (depending on migration complexity)

**Phases**:
1. **Analysis** (2-3 days): Feature analysis, migration planning, architectural decisions
2. **Design** (1-2 days): UI wireframes, API specifications, database schema
3. **Implementation** (3-5 days): Backend services, admin UI, DigitalOcean integration
4. **Testing** (2-3 days): Unit tests, integration tests, E2E backup/restore validation
5. **Finalization** (1 day): Documentation, deployment guide, handoff

## Questions for Stakeholders

1. **Backup Frequency**: What is the desired automated backup schedule? (Daily? Every 6 hours? Configurable?)
2. **Retention Policy**: How long should backups be retained? (30 days? 90 days? Configurable?)
3. **Restore Testing**: Should we implement automated restore testing to validate backup integrity?
4. **Notifications**: Who should be notified of backup success/failures? Email? Dashboard alerts?
5. **Cost Budget**: What is the acceptable DigitalOcean Spaces storage cost? (Affects retention policy)

## Out of Scope (V1)

- Multi-database support (focus on single WitchCityRope database)
- Real-time replication
- Cross-region backup replication
- Advanced backup encryption beyond DigitalOcean Spaces default
- Automated disaster recovery testing

---

**Next Steps**: Complete analysis document with account-automation feature review and migration plan.
