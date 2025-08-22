#!/usr/bin/env python3
"""
OWASP ZAP Security Scanning Automation Script
Automates security scanning using OWASP ZAP's API
"""

import json
import time
import sys
import argparse
from datetime import datetime
from zapv2 import ZAPv2
import requests
from urllib.parse import urlparse

class ZAPSecurityScanner:
    def __init__(self, target_url, api_key='changeme', proxy_host='localhost', proxy_port=8080):
        """Initialize ZAP scanner with configuration"""
        self.target_url = target_url
        self.api_key = api_key
        self.proxy = f'http://{proxy_host}:{proxy_port}'
        
        # Initialize ZAP API client
        self.zap = ZAPv2(apikey=api_key, proxies={
            'http': self.proxy,
            'https': self.proxy
        })
        
        self.session_name = f"scan_{urlparse(target_url).netloc}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        
    def start_zap_session(self):
        """Start a new ZAP session"""
        print(f"[*] Starting new ZAP session: {self.session_name}")
        self.zap.core.new_session(name=self.session_name, overwrite=True)
        
    def spider_target(self):
        """Spider the target website"""
        print(f"[*] Spidering target: {self.target_url}")
        
        # Start spider scan
        scan_id = self.zap.spider.scan(self.target_url)
        
        # Wait for spider to complete
        while int(self.zap.spider.status(scan_id)) < 100:
            print(f"[*] Spider progress: {self.zap.spider.status(scan_id)}%")
            time.sleep(5)
            
        print("[+] Spider scan completed")
        
        # Get spider results
        results = self.zap.spider.results(scan_id)
        print(f"[*] Found {len(results)} URLs during spidering")
        
        return results
        
    def ajax_spider_target(self):
        """AJAX Spider for modern web applications"""
        print(f"[*] Starting AJAX spider for: {self.target_url}")
        
        # Start AJAX spider
        self.zap.ajaxSpider.scan(self.target_url)
        
        # Wait for AJAX spider
        while self.zap.ajaxSpider.status == 'running':
            print(f"[*] AJAX Spider running... Found {self.zap.ajaxSpider.number_of_results} results")
            time.sleep(5)
            
        print("[+] AJAX spider completed")
        
    def passive_scan(self):
        """Wait for passive scanning to complete"""
        print("[*] Waiting for passive scan to complete")
        
        while int(self.zap.pscan.records_to_scan) > 0:
            print(f"[*] Passive scan queue: {self.zap.pscan.records_to_scan}")
            time.sleep(2)
            
        print("[+] Passive scanning completed")
        
    def active_scan(self):
        """Perform active security scan"""
        print(f"[*] Starting active scan on: {self.target_url}")
        
        # Configure scan policy
        scan_policy_name = "Default Policy"
        
        # Start active scan
        scan_id = self.zap.ascan.scan(
            self.target_url,
            recurse=True,
            inscopeonly=False,
            scanpolicyname=scan_policy_name
        )
        
        # Monitor scan progress
        while int(self.zap.ascan.status(scan_id)) < 100:
            print(f"[*] Active scan progress: {self.zap.ascan.status(scan_id)}%")
            time.sleep(10)
            
        print("[+] Active scan completed")
        
    def get_alerts(self):
        """Retrieve and categorize security alerts"""
        alerts = self.zap.core.alerts(baseurl=self.target_url)
        
        # Categorize alerts by risk level
        categorized = {
            'High': [],
            'Medium': [],
            'Low': [],
            'Informational': []
        }
        
        for alert in alerts:
            risk = alert.get('risk', 'Informational')
            categorized[risk].append(alert)
            
        return categorized
        
    def generate_report(self, output_format='json'):
        """Generate security scan report"""
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        
        if output_format == 'json':
            # JSON report
            alerts = self.get_alerts()
            report_data = {
                'scan_info': {
                    'target': self.target_url,
                    'timestamp': timestamp,
                    'session': self.session_name
                },
                'summary': {
                    'high_risk': len(alerts['High']),
                    'medium_risk': len(alerts['Medium']),
                    'low_risk': len(alerts['Low']),
                    'informational': len(alerts['Informational'])
                },
                'alerts': alerts
            }
            
            filename = f"zap_report_{timestamp}.json"
            with open(filename, 'w') as f:
                json.dump(report_data, f, indent=2)
                
            print(f"[+] JSON report saved to: {filename}")
            
        elif output_format == 'html':
            # HTML report
            html_report = self.zap.core.htmlreport()
            filename = f"zap_report_{timestamp}.html"
            
            with open(filename, 'w') as f:
                f.write(html_report)
                
            print(f"[+] HTML report saved to: {filename}")
            
        elif output_format == 'xml':
            # XML report
            xml_report = self.zap.core.xmlreport()
            filename = f"zap_report_{timestamp}.xml"
            
            with open(filename, 'w') as f:
                f.write(xml_report)
                
            print(f"[+] XML report saved to: {filename}")
            
        return filename
        
    def check_specific_vulnerabilities(self):
        """Check for specific vulnerability types"""
        vuln_checks = {
            'SQL Injection': [],
            'Cross Site Scripting': [],
            'Path Traversal': [],
            'Remote File Inclusion': [],
            'Server Side Include': [],
            'Cross Site Request Forgery': []
        }
        
        alerts = self.zap.core.alerts(baseurl=self.target_url)
        
        for alert in alerts:
            alert_name = alert.get('alert', '')
            for vuln_type in vuln_checks:
                if vuln_type.lower() in alert_name.lower():
                    vuln_checks[vuln_type].append(alert)
                    
        return vuln_checks
        
    def run_full_scan(self):
        """Execute complete security scan workflow"""
        try:
            # Start session
            self.start_zap_session()
            
            # Spider the target
            self.spider_target()
            
            # AJAX spider for dynamic content
            self.ajax_spider_target()
            
            # Wait for passive scan
            self.passive_scan()
            
            # Run active scan
            self.active_scan()
            
            # Get vulnerability summary
            alerts = self.get_alerts()
            
            print("\n[*] Security Scan Summary:")
            print(f"    High Risk: {len(alerts['High'])}")
            print(f"    Medium Risk: {len(alerts['Medium'])}")
            print(f"    Low Risk: {len(alerts['Low'])}")
            print(f"    Informational: {len(alerts['Informational'])}")
            
            # Check specific vulnerabilities
            specific_vulns = self.check_specific_vulnerabilities()
            
            print("\n[*] Specific Vulnerability Check:")
            for vuln_type, found in specific_vulns.items():
                if found:
                    print(f"    [!] {vuln_type}: {len(found)} instance(s) found")
                    
            # Generate reports
            print("\n[*] Generating reports...")
            self.generate_report('json')
            self.generate_report('html')
            
            return True
            
        except Exception as e:
            print(f"[!] Error during scan: {str(e)}")
            return False
            
def main():
    parser = argparse.ArgumentParser(description='OWASP ZAP Security Scanner Automation')
    parser.add_argument('target', help='Target URL to scan')
    parser.add_argument('--api-key', default='changeme', help='ZAP API key')
    parser.add_argument('--zap-host', default='localhost', help='ZAP proxy host')
    parser.add_argument('--zap-port', default=8080, type=int, help='ZAP proxy port')
    parser.add_argument('--quick', action='store_true', help='Run quick scan (spider + passive only)')
    
    args = parser.parse_args()
    
    # Validate target URL
    if not args.target.startswith(('http://', 'https://')):
        print("[!] Target URL must include protocol (http:// or https://)")
        sys.exit(1)
        
    print(f"""
    ╔══════════════════════════════════════════╗
    ║     OWASP ZAP Security Scanner          ║
    ║     Automated Vulnerability Testing      ║
    ╚══════════════════════════════════════════╝
    
    Target: {args.target}
    ZAP Proxy: {args.zap_host}:{args.zap_port}
    """)
    
    # Create scanner instance
    scanner = ZAPSecurityScanner(
        args.target,
        api_key=args.api_key,
        proxy_host=args.zap_host,
        proxy_port=args.zap_port
    )
    
    # Run scan
    if args.quick:
        print("[*] Running quick scan (passive only)...")
        scanner.start_zap_session()
        scanner.spider_target()
        scanner.passive_scan()
        scanner.generate_report('json')
    else:
        print("[*] Running full security scan...")
        scanner.run_full_scan()
        
    print("\n[+] Security scan completed!")
    
if __name__ == "__main__":
    main()