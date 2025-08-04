# WitchCityRope TODO List

## Current Priority Tasks 🔥

### Critical - Security & Package Updates ✅
- [x] **Update Vulnerable NuGet Packages**
  - [x] Update System.Text.Json from 6.0.0 to 9.0.0 (security vulnerability GHSA-8g4q-xg66-9fp4) - **COMPLETED Dec 30**
  - [ ] Review and update any other packages with known vulnerabilities
  - [ ] Run security audit on all dependencies

- [x] **Fix Test Suite Dependencies**
  - [x] Add missing Bogus package to test projects that reference it - **COMPLETED Dec 30**
  - [x] Ensure all test projects can build successfully - **COMPLETED Dec 30**
  - [ ] Fix remaining failing tests (74.7% passing)

### High Priority - Infrastructure
- [ ] **Complete Aspire Integration**
  - [x] Create WitchCityRope.AppHost project
  - [x] Create WitchCityRope.ServiceDefaults project
  - [ ] Configure Aspire orchestration for development
  - [ ] Update connection string handling for Aspire service discovery
  - [ ] Test Aspire dashboard and telemetry features
  - [ ] Document Aspire development workflow

- [ ] **Fix PostgreSQL MCP Connection**
  - Update PostgreSQL password in claude_desktop_config.json to match docker-compose.postgres.yml
  - Default password in docker-compose is: your_secure_password_here
  - Ensure PostgreSQL container is running with docker-compose -f docker-compose.postgres.yml up -d
  - Verify connection with pgAdmin at localhost:5050

- [ ] **Migrate from SQLite to PostgreSQL**
  - PostgreSQL Docker setup already exists in docker-compose.postgres.yml
  - Container name: witchcityrope-postgres (port 5432)
  - Database name: witchcityrope_db
  - Update Entity Framework Core provider from SQLite to Npgsql
  - Migrate existing data from SQLite databases
  - Update all connection strings in configuration files
  - Test all database operations with PostgreSQL
  - Update deployment scripts for PostgreSQL
  - Document PostgreSQL backup and restore procedures

## Remaining Tasks 📋

### High Priority
- [ ] **Fix PostgreSQL MCP Connection**
  - Update PostgreSQL password in claude_desktop_config.json to match docker-compose.postgres.yml
  - Default password in docker-compose is: your_secure_password_here
  - Ensure PostgreSQL container is running with docker-compose -f docker-compose.postgres.yml up -d
  - Verify connection with pgAdmin at localhost:5050

- [ ] **Migrate from SQLite to PostgreSQL**
  - PostgreSQL Docker setup already exists in docker-compose.postgres.yml
  - Container name: witchcityrope-postgres (port 5432)
  - Database name: witchcityrope_db
  - Update Entity Framework Core provider from SQLite to Npgsql
  - Migrate existing data from SQLite databases
  - Update all connection strings in configuration files
  - Test all database operations with PostgreSQL
  - Update deployment scripts for PostgreSQL
  - Document PostgreSQL backup and restore procedures

### Medium Priority - Code Quality
- [ ] **Fix Nullable Reference Warnings**
  - [ ] Fix VettingApplication entity constructor warnings (8 properties)
  - [ ] Fix Payment entity nullable property warnings
  - [ ] Fix IncidentReport entity nullable property warnings
  - [ ] Fix Registration entity nullable property warnings
  - [ ] Fix User entity nullable property warnings
  - [ ] Review and fix all CS8618 warnings in Core project

- [ ] **Implement lazy loading for Syncfusion components**
  - Configure dynamic imports for Syncfusion modules
  - Optimize initial page load by deferring component loading
  - Add loading indicators for async components
  - Test performance improvements

- [ ] **Fix Stagehand MCP Server Configuration**
  - Update stagehand server path to use correct location
  - Verify OpenAI API key is valid and active
  - Configure local CDP URL for browser automation
  - Test browser automation functionality

- [ ] **Secure API Keys and Sensitive Data**
  - Remove hardcoded API keys from claude_desktop_config.json
  - Implement environment variable loading for sensitive data
  - Update GitHub token (appears to be exposed)
  - Update OpenAI API key (appears to be exposed)
  - Create .env.example file with required variables

### Low Priority
- [ ] **Bundle CSS and JavaScript with WebOptimizer**
  - Configure WebOptimizer for production builds
  - Set up bundling rules for CSS files
  - Configure JavaScript bundling and minification
  - Add cache busting for bundled assets

- [ ] **Add PWA support with manifest.json**
  - Create manifest.json file with app metadata
  - Add service worker for offline functionality
  - Configure app icons and splash screens
  - Implement install prompts for supported browsers

- [ ] **Clean up Docker Configuration**
  - Consolidate docker-compose files if possible
  - Add docker-compose override for local development
  - Create production-ready docker-compose configuration
  - Document Docker setup and usage

## Notes
- Browser MCP server setup completed successfully with PowerShell bridge method
- PostgreSQL MCP server configured but needs password update
- Discovered existing PostgreSQL Docker setup (docker-compose.postgres.yml)
- API keys in MCP config should be secured with environment variables
- Consider using Docker secrets for production deployments
- Aspire projects (AppHost and ServiceDefaults) exist but need configuration
- System.Text.Json vulnerability needs immediate attention
- Multiple nullable reference warnings need to be addressed for code quality

## Completed Tasks ✅

### Infrastructure & Setup (Completed)
- [x] **MCP Server Setup**
  - [x] Configure browser MCP server (browser-tools)
  - [x] Set up PostgreSQL MCP server
  - [x] Configure filesystem MCP server
  - [x] Set up memory MCP server
  - [x] Configure Docker MCP server
  - [x] Create universal Chrome launcher script for browser automation
  - [x] Document browser automation best practices

- [x] **Aspire Project Structure**
  - [x] Create WitchCityRope.AppHost project
  - [x] Create WitchCityRope.ServiceDefaults project

### Development & Features (Completed)
- [x] **Performance Optimizations**
  - [x] Implement compression middleware
  - [x] Configure response caching
  - [x] Enable JavaScript and CSS minification
  - [x] Optimize static file serving

- [x] **UI Testing and Fixes**
  - [x] Test all user interface components
  - [x] Fix responsive design issues
  - [x] Ensure cross-browser compatibility
  - [x] Validate accessibility standards

- [x] **Admin Interfaces**
  - [x] Complete admin dashboard implementation
  - [x] Add user management interface
  - [x] Implement content management features
  - [x] Create reporting and analytics views

- [x] **Database and Authentication**
  - [x] Set up database seeding
  - [x] Implement authentication system
  - [x] Configure user roles and permissions
  - [x] Add secure session management

- [x] **Build and Compilation**
  - [x] Fix all compilation errors (533 errors resolved)
  - [x] Resolve dependency conflicts
  - [x] Configure build pipeline
  - [x] Set up automated builds

- [x] **Navigation and Integration**
  - [x] Complete navigation integration tests
  - [x] Fix routing issues
  - [x] Implement breadcrumb navigation
  - [x] Add deep linking support
  - [x] Add "How To Join" navigation links
  - [x] Create vetting process pages and forms

- [x] **Docker Development Environment**
  - [x] Configure Docker Compose for development
  - [x] Set up PostgreSQL container with health checks
  - [x] Configure proper volume persistence
  - [x] Document port configurations to avoid conflicts