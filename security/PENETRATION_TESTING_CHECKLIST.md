# Penetration Testing Checklist

## Table of Contents
1. [Pre-Engagement](#pre-engagement)
2. [Information Gathering](#information-gathering)
3. [Vulnerability Assessment](#vulnerability-assessment)
4. [Exploitation](#exploitation)
5. [Post-Exploitation](#post-exploitation)
6. [Reporting](#reporting)
7. [Tools and Resources](#tools-and-resources)

## Pre-Engagement

### Legal and Scope Definition
- [ ] Obtain written authorization for testing
- [ ] Define scope of testing (IP ranges, domains, applications)
- [ ] Identify systems explicitly out of scope
- [ ] Establish testing windows and blackout periods
- [ ] Define communication channels and escalation procedures
- [ ] Review applicable compliance requirements (PCI-DSS, HIPAA, etc.)
- [ ] Obtain emergency contacts

### Rules of Engagement
- [ ] Testing methodology agreed upon
- [ ] Types of tests allowed (black box, gray box, white box)
- [ ] Social engineering permitted? (phishing, physical access)
- [ ] DoS/DDoS testing allowed?
- [ ] Data exfiltration limits defined
- [ ] Password cracking depth agreed

## Information Gathering

### Passive Reconnaissance
- [ ] **DNS Enumeration**
  - [ ] Whois lookup
  - [ ] DNS records (A, AAAA, MX, TXT, NS)
  - [ ] Subdomain enumeration
  - [ ] DNS zone transfer attempts
  - [ ] Reverse DNS lookups

- [ ] **Search Engine Reconnaissance**
  - [ ] Google dorking (site:, filetype:, intitle:, etc.)
  - [ ] Shodan search for exposed services
  - [ ] Wayback Machine for historical data
  - [ ] Pastebin and code repository searches
  - [ ] Social media reconnaissance

- [ ] **Certificate Transparency**
  - [ ] crt.sh subdomain discovery
  - [ ] Certificate analysis
  - [ ] SSL/TLS configuration review

### Active Reconnaissance
- [ ] **Network Scanning**
  - [ ] Port scanning (TCP/UDP)
  - [ ] Service version detection
  - [ ] OS fingerprinting
  - [ ] Network topology mapping
  - [ ] Live host discovery

- [ ] **Web Application Discovery**
  - [ ] Directory and file enumeration
  - [ ] Technology stack identification
  - [ ] API endpoint discovery
  - [ ] Parameter discovery
  - [ ] Cookie analysis

## Vulnerability Assessment

### Infrastructure Vulnerabilities
- [ ] **Network Services**
  - [ ] SSH weak configurations
  - [ ] Telnet/FTP usage
  - [ ] SNMP community strings
  - [ ] SMB/NetBIOS vulnerabilities
  - [ ] Database service exposure

- [ ] **Operating System**
  - [ ] Missing patches/updates
  - [ ] Default credentials
  - [ ] Weak password policies
  - [ ] Unnecessary services running
  - [ ] Privilege escalation vectors

### Web Application Vulnerabilities

#### Authentication and Session Management
- [ ] Weak password policies
- [ ] Username enumeration
- [ ] Brute force protection
- [ ] Session fixation
- [ ] Session timeout implementation
- [ ] Cookie security flags
- [ ] Multi-factor authentication bypass
- [ ] Password reset vulnerabilities
- [ ] Account lockout mechanisms
- [ ] CAPTCHA bypass

#### Input Validation
- [ ] **SQL Injection**
  - [ ] Classic SQL injection
  - [ ] Blind SQL injection
  - [ ] Time-based blind SQL injection
  - [ ] Second-order SQL injection
  - [ ] NoSQL injection

- [ ] **Cross-Site Scripting (XSS)**
  - [ ] Reflected XSS
  - [ ] Stored XSS
  - [ ] DOM-based XSS
  - [ ] Self-XSS
  - [ ] Filter bypass techniques

- [ ] **Command Injection**
  - [ ] OS command injection
  - [ ] Code injection (PHP, Python, etc.)
  - [ ] LDAP injection
  - [ ] XPath injection
  - [ ] Header injection

#### Access Control
- [ ] Insecure Direct Object References (IDOR)
- [ ] Privilege escalation
- [ ] Horizontal privilege escalation
- [ ] Vertical privilege escalation
- [ ] Missing function-level access control
- [ ] Force browsing
- [ ] Path traversal
- [ ] Local file inclusion (LFI)
- [ ] Remote file inclusion (RFI)

#### Business Logic
- [ ] Price manipulation
- [ ] Race conditions
- [ ] Workflow bypass
- [ ] Negative value testing
- [ ] Integer overflow
- [ ] Time-of-check time-of-use (TOCTOU)
- [ ] Business constraint bypass

#### Configuration and Deployment
- [ ] Security headers missing
- [ ] SSL/TLS misconfigurations
- [ ] CORS misconfiguration
- [ ] Subdomain takeover
- [ ] Cloud storage misconfiguration
- [ ] Default credentials
- [ ] Unnecessary HTTP methods
- [ ] Information disclosure
- [ ] Debug mode enabled
- [ ] Directory listing

#### Other Web Vulnerabilities
- [ ] **Cross-Site Request Forgery (CSRF)**
  - [ ] State-changing operations
  - [ ] Token validation
  - [ ] Referer header validation

- [ ] **Server-Side Request Forgery (SSRF)**
  - [ ] Internal service access
  - [ ] Cloud metadata access
  - [ ] Port scanning via SSRF

- [ ] **XML External Entity (XXE)**
  - [ ] File disclosure
  - [ ] SSRF via XXE
  - [ ] Denial of service

- [ ] **File Upload Vulnerabilities**
  - [ ] Unrestricted file upload
  - [ ] File type validation bypass
  - [ ] File size limits
  - [ ] Malicious file execution

### API Security Testing
- [ ] **Authentication and Authorization**
  - [ ] API key security
  - [ ] OAuth implementation flaws
  - [ ] JWT vulnerabilities
  - [ ] Rate limiting

- [ ] **Input Validation**
  - [ ] JSON/XML injection
  - [ ] Mass assignment
  - [ ] GraphQL specific attacks
  - [ ] API versioning issues

- [ ] **Business Logic**
  - [ ] Resource exhaustion
  - [ ] Endpoint discovery
  - [ ] Method manipulation
  - [ ] Response manipulation

### Mobile Application Testing (if applicable)
- [ ] **Client-Side Security**
  - [ ] Insecure data storage
  - [ ] Weak cryptography
  - [ ] Certificate pinning
  - [ ] Root/jailbreak detection

- [ ] **Network Communication**
  - [ ] Man-in-the-middle vulnerabilities
  - [ ] Insecure protocols
  - [ ] API endpoint security

## Exploitation

### Preparation
- [ ] Backup critical data
- [ ] Document initial state
- [ ] Prepare exploit codes
- [ ] Set up logging and monitoring
- [ ] Notify stakeholders of exploitation phase

### Exploitation Techniques
- [ ] **Manual Exploitation**
  - [ ] Proof of concept development
  - [ ] Custom exploit writing
  - [ ] Chaining vulnerabilities
  - [ ] Bypassing security controls

- [ ] **Automated Exploitation**
  - [ ] Metasploit modules
  - [ ] SQLMap for database access
  - [ ] BeEF for browser exploitation
  - [ ] Commercial tool usage

### Evidence Collection
- [ ] Screenshot all successful exploits
- [ ] Capture network traffic
- [ ] Document exact steps to reproduce
- [ ] Collect system information
- [ ] Preserve logs and artifacts

## Post-Exploitation

### Persistence (if authorized)
- [ ] Backdoor installation testing
- [ ] Registry modification
- [ ] Scheduled task creation
- [ ] Service installation
- [ ] Rootkit simulation

### Lateral Movement
- [ ] Network enumeration from compromised host
- [ ] Credential harvesting
- [ ] Pass-the-hash attacks
- [ ] Kerberos ticket manipulation
- [ ] Pivot point establishment

### Data Exfiltration Testing
- [ ] Identify sensitive data locations
- [ ] Test data transfer methods
- [ ] Encryption/obfuscation techniques
- [ ] Bandwidth limitations
- [ ] Detection evasion

### Privilege Escalation
- [ ] **Windows**
  - [ ] Unquoted service paths
  - [ ] Weak service permissions
  - [ ] AlwaysInstallElevated
  - [ ] Kernel exploits
  - [ ] DLL hijacking

- [ ] **Linux**
  - [ ] SUID/SGID binaries
  - [ ] Sudo misconfigurations
  - [ ] Kernel exploits
  - [ ] Cron job abuse
  - [ ] Docker escape

### Cleanup
- [ ] Remove test files
- [ ] Close backdoors
- [ ] Restore modified configurations
- [ ] Clear logs (if authorized)
- [ ] Document all changes made

## Reporting

### Executive Summary
- [ ] High-level overview of findings
- [ ] Business impact analysis
- [ ] Risk ratings and prioritization
- [ ] Key recommendations
- [ ] Compliance implications

### Technical Details
- [ ] **Vulnerability Details**
  - [ ] Description
  - [ ] Affected systems/URLs
  - [ ] Proof of concept
  - [ ] CVSS scoring
  - [ ] Evidence (screenshots, logs)

- [ ] **Reproduction Steps**
  - [ ] Detailed methodology
  - [ ] Tools used
  - [ ] Commands executed
  - [ ] Expected vs actual results

### Remediation Recommendations
- [ ] **Immediate Actions**
  - [ ] Critical patches
  - [ ] Configuration changes
  - [ ] Access control updates

- [ ] **Short-term Improvements**
  - [ ] Security control implementations
  - [ ] Process improvements
  - [ ] Training recommendations

- [ ] **Long-term Strategy**
  - [ ] Architecture changes
  - [ ] Security program enhancements
  - [ ] Technology upgrades

### Appendices
- [ ] Tool output
- [ ] Raw scan results
- [ ] Scripts developed
- [ ] References and citations
- [ ] Glossary of terms

## Tools and Resources

### Network Scanning
- **Nmap**: Port scanning and service detection
- **Masscan**: High-speed port scanner
- **Zmap**: Internet-wide scanning
- **Netcat**: Network debugging and exploration

### Web Application Testing
- **Burp Suite**: Web vulnerability scanner
- **OWASP ZAP**: Open-source web scanner
- **SQLMap**: SQL injection tool
- **Nikto**: Web server scanner
- **Dirb/Dirbuster**: Directory enumeration
- **WPScan**: WordPress vulnerability scanner

### Exploitation Frameworks
- **Metasploit**: Penetration testing framework
- **Empire**: PowerShell post-exploitation
- **Cobalt Strike**: Adversary simulation
- **BeEF**: Browser exploitation framework

### Password Testing
- **John the Ripper**: Password cracker
- **Hashcat**: Advanced password recovery
- **Hydra**: Online password brute forcing
- **CeWL**: Custom wordlist generator

### Wireless Testing
- **Aircrack-ng**: WiFi security testing
- **Kismet**: Wireless network detector
- **Reaver**: WPS attack tool
- **Wifite**: Automated wireless auditor

### Mobile Testing
- **MobSF**: Mobile security framework
- **Objection**: Runtime mobile exploration
- **Frida**: Dynamic instrumentation
- **APKTool**: Android app reverse engineering

### Reporting Tools
- **Dradis**: Collaboration and reporting
- **Faraday**: Collaborative penetration test IDE
- **PlexTrac**: Penetration testing management
- **DefectDojo**: Security orchestration

### Online Resources
- **OWASP Testing Guide**: Comprehensive web testing methodology
- **PTES**: Penetration Testing Execution Standard
- **NIST Cybersecurity Framework**: Security guidelines
- **CVE Database**: Vulnerability database
- **Exploit-DB**: Exploit database
- **PayloadsAllTheThings**: Payload collection

## Best Practices

### During Testing
1. Always stay within scope
2. Document everything meticulously
3. Minimize impact on production systems
4. Communicate issues immediately
5. Follow responsible disclosure

### Communication
1. Regular status updates
2. Immediate notification of critical findings
3. Clear and professional reporting
4. Actionable recommendations
5. Knowledge transfer sessions

### Ethical Considerations
1. Respect data privacy
2. Avoid unnecessary damage
3. Report all findings honestly
4. Maintain confidentiality
5. Follow professional standards

## Compliance Considerations

### PCI-DSS Requirements
- [ ] Annual penetration testing
- [ ] Segmentation validation
- [ ] Critical patch validation
- [ ] Methodology documentation

### HIPAA Considerations
- [ ] PHI data handling procedures
- [ ] Encryption validation
- [ ] Access control testing
- [ ] Audit trail preservation

### SOC 2 Requirements
- [ ] Control effectiveness testing
- [ ] Change management validation
- [ ] Incident response testing
- [ ] Continuous monitoring

## Conclusion

This checklist serves as a comprehensive guide for penetration testing engagements. Always customize based on:
- Client requirements
- Regulatory compliance
- Time constraints
- Risk tolerance
- Technical environment

Remember: The goal is to improve security, not to cause harm. Always maintain professionalism and ethical standards throughout the engagement.