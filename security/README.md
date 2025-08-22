# Security Audit and Testing Suite

This directory contains comprehensive security audit scripts, SSL setup documentation, and testing tools for maintaining robust application security.

## Contents

### 1. **owasp_zap_automation.py**
Automated OWASP ZAP security scanning script that performs:
- Spider scanning for content discovery
- AJAX spider for modern web applications
- Passive security scanning
- Active vulnerability scanning
- Automated report generation (JSON/HTML/XML)

**Usage:**
```bash
python owasp_zap_automation.py https://example.com --api-key your-api-key
```

### 2. **SSL_SETUP_GUIDE.md**
Comprehensive SSL/TLS setup documentation including:
- Let's Encrypt automated setup
- Commercial SSL certificate installation
- Nginx and Apache configurations
- Security best practices
- Automated renewal scripts
- Troubleshooting guide

### 3. **security_headers_validator.py**
Security headers validation tool that checks for:
- Missing security headers (HSTS, CSP, X-Frame-Options, etc.)
- Dangerous information disclosure headers
- Header configuration issues
- Generates reports in text, JSON, and HTML formats

**Usage:**
```bash
python security_headers_validator.py https://example.com --format html
```

### 4. **PENETRATION_TESTING_CHECKLIST.md**
Comprehensive penetration testing checklist covering:
- Pre-engagement requirements
- Information gathering techniques
- Vulnerability assessment procedures
- Exploitation methodologies
- Post-exploitation activities
- Detailed reporting guidelines
- Compliance considerations

### 5. **vulnerability_scanner.py**
Automated vulnerability scanning orchestrator that integrates:
- Nmap for network scanning
- Nikto for web server scanning
- TestSSL for SSL/TLS testing
- Nuclei for template-based scanning
- Directory fuzzing capabilities
- Security header checking
- Basic SQL injection detection

**Usage:**
```bash
python vulnerability_scanner.py https://example.com --scan-type full
```

### 6. **SECURITY_AUDIT_REPORT_TEMPLATE.md**
Professional security audit report template featuring:
- Executive summary format
- Risk assessment methodology
- Detailed findings structure
- Compliance assessment sections
- Remediation roadmap
- Resource allocation planning
- Professional formatting

## Prerequisites

### Required Tools
- Python 3.7+
- OWASP ZAP (for ZAP automation)
- Nmap
- Nikto
- TestSSL
- Nuclei
- Gobuster or similar directory fuzzing tool

### Python Dependencies
```bash
pip install requests zapv2 python-nmap
```

### Tool Installation

#### Ubuntu/Debian:
```bash
# Install Nmap
sudo apt-get install nmap

# Install Nikto
sudo apt-get install nikto

# Install Gobuster
sudo apt-get install gobuster

# Install OWASP ZAP
wget https://github.com/zaproxy/zaproxy/releases/download/v2.14.0/ZAP_2.14.0_Linux.tar.gz
tar -xvf ZAP_2.14.0_Linux.tar.gz

# Install Nuclei
go install -v github.com/projectdiscovery/nuclei/v3/cmd/nuclei@latest

# Install TestSSL
git clone https://github.com/drwetter/testssl.sh.git
```

## Quick Start

### 1. Basic Security Scan
```bash
# Run a quick vulnerability scan
python vulnerability_scanner.py https://yoursite.com --scan-type quick

# Check security headers
python security_headers_validator.py https://yoursite.com
```

### 2. Comprehensive Security Audit
```bash
# Start OWASP ZAP (in daemon mode)
./zap.sh -daemon -port 8080 -config api.key=your-api-key

# Run comprehensive ZAP scan
python owasp_zap_automation.py https://yoursite.com --api-key your-api-key

# Run full vulnerability scan
python vulnerability_scanner.py https://yoursite.com --scan-type full
```

### 3. SSL/TLS Setup
Follow the SSL_SETUP_GUIDE.md for:
- Setting up Let's Encrypt certificates
- Configuring web servers for optimal security
- Implementing security headers
- Setting up automated renewal

## Security Best Practices

1. **Regular Scanning**: Schedule automated security scans weekly or after deployments
2. **Patch Management**: Address critical vulnerabilities within 24-48 hours
3. **Security Headers**: Implement all recommended security headers
4. **SSL/TLS**: Use strong cipher suites and keep certificates updated
5. **Documentation**: Maintain detailed records of all security assessments

## Reporting

All tools generate reports in multiple formats:
- **JSON**: For programmatic processing
- **HTML**: For management presentation
- **Text**: For quick command-line review

Reports are saved in timestamped directories for historical tracking.

## Compliance

The tools and templates support compliance with:
- OWASP Top 10
- PCI-DSS
- HIPAA
- GDPR
- SOC 2
- ISO 27001

## Contributing

When adding new security tools or updating existing ones:
1. Ensure compatibility with existing toolchain
2. Update this README with usage instructions
3. Include error handling and logging
4. Add report generation capabilities
5. Test thoroughly before deployment

## Support

For issues or questions:
1. Check tool-specific documentation
2. Review error logs in scan output directories
3. Consult the penetration testing checklist for methodology questions
4. Refer to OWASP documentation for vulnerability details

## License

These security tools are provided for authorized security testing only. Ensure you have proper authorization before scanning any systems you do not own.