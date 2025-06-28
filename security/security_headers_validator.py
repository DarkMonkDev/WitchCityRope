#!/usr/bin/env python3
"""
Security Headers Validation Script
Checks web applications for proper security header implementation
"""

import requests
import json
import sys
import argparse
from datetime import datetime
from urllib.parse import urlparse
import re
from typing import Dict, List, Tuple

class SecurityHeadersValidator:
    def __init__(self):
        self.required_headers = {
            'Strict-Transport-Security': {
                'required': True,
                'recommended_value': 'max-age=31536000; includeSubDomains; preload',
                'description': 'Forces HTTPS connections',
                'severity': 'HIGH'
            },
            'X-Frame-Options': {
                'required': True,
                'recommended_value': ['DENY', 'SAMEORIGIN'],
                'description': 'Prevents clickjacking attacks',
                'severity': 'HIGH'
            },
            'X-Content-Type-Options': {
                'required': True,
                'recommended_value': 'nosniff',
                'description': 'Prevents MIME type sniffing',
                'severity': 'MEDIUM'
            },
            'Content-Security-Policy': {
                'required': True,
                'recommended_value': None,  # Complex validation needed
                'description': 'Controls resources the browser can load',
                'severity': 'HIGH'
            },
            'X-XSS-Protection': {
                'required': False,  # Deprecated but still checked
                'recommended_value': '1; mode=block',
                'description': 'XSS filter (deprecated in modern browsers)',
                'severity': 'LOW'
            },
            'Referrer-Policy': {
                'required': True,
                'recommended_value': ['strict-origin-when-cross-origin', 'no-referrer'],
                'description': 'Controls referrer information',
                'severity': 'MEDIUM'
            },
            'Permissions-Policy': {
                'required': True,
                'recommended_value': None,  # Complex validation needed
                'description': 'Controls browser features and APIs',
                'severity': 'MEDIUM'
            },
            'X-Permitted-Cross-Domain-Policies': {
                'required': False,
                'recommended_value': 'none',
                'description': 'Controls Adobe products cross-domain access',
                'severity': 'LOW'
            }
        }
        
        self.deprecated_headers = [
            'X-XSS-Protection',
            'Public-Key-Pins',
            'X-Content-Security-Policy',
            'X-WebKit-CSP'
        ]
        
        self.dangerous_headers = [
            'Server',
            'X-Powered-By',
            'X-AspNet-Version',
            'X-AspNetMvc-Version'
        ]
        
    def check_url(self, url: str, follow_redirects: bool = True) -> Dict:
        """Check security headers for a given URL"""
        try:
            response = requests.get(
                url,
                allow_redirects=follow_redirects,
                timeout=10,
                headers={'User-Agent': 'Security Headers Validator/1.0'}
            )
            
            return self.analyze_headers(response.headers, url)
            
        except requests.exceptions.RequestException as e:
            return {
                'error': f'Failed to connect: {str(e)}',
                'url': url,
                'timestamp': datetime.now().isoformat()
            }
            
    def analyze_headers(self, headers: Dict, url: str) -> Dict:
        """Analyze response headers for security issues"""
        results = {
            'url': url,
            'timestamp': datetime.now().isoformat(),
            'score': 0,
            'max_score': 0,
            'grade': 'F',
            'missing_headers': [],
            'present_headers': [],
            'warnings': [],
            'dangerous_headers': [],
            'recommendations': []
        }
        
        # Check required security headers
        for header, config in self.required_headers.items():
            self._check_header(header, headers, config, results)
            
        # Check for dangerous headers
        for header in self.dangerous_headers:
            if header.lower() in [h.lower() for h in headers.keys()]:
                results['dangerous_headers'].append({
                    'header': header,
                    'value': headers.get(header, ''),
                    'recommendation': f'Remove {header} header to avoid information disclosure'
                })
                
        # Calculate score and grade
        results['grade'] = self._calculate_grade(results['score'], results['max_score'])
        
        # Add general recommendations
        self._add_recommendations(results)
        
        return results
        
    def _check_header(self, header: str, headers: Dict, config: Dict, results: Dict):
        """Check individual header against requirements"""
        header_lower = header.lower()
        found_header = None
        header_value = None
        
        # Case-insensitive header search
        for h, v in headers.items():
            if h.lower() == header_lower:
                found_header = h
                header_value = v
                break
                
        # Update max score
        if config['required']:
            results['max_score'] += 10
            
        if found_header:
            header_info = {
                'header': header,
                'value': header_value,
                'status': 'FAIL',
                'issues': []
            }
            
            # Validate header value
            if header == 'Content-Security-Policy':
                header_info['status'], header_info['issues'] = self._validate_csp(header_value)
            elif header == 'Permissions-Policy':
                header_info['status'], header_info['issues'] = self._validate_permissions_policy(header_value)
            elif header == 'Strict-Transport-Security':
                header_info['status'], header_info['issues'] = self._validate_hsts(header_value)
            else:
                header_info['status'], header_info['issues'] = self._validate_simple_header(
                    header_value, config['recommended_value']
                )
                
            if header_info['status'] == 'PASS':
                results['score'] += 10
            elif header_info['status'] == 'WARN':
                results['score'] += 5
                
            results['present_headers'].append(header_info)
            
            # Add warnings for deprecated headers
            if header in self.deprecated_headers:
                results['warnings'].append(f"{header} is deprecated but still present")
                
        else:
            if config['required']:
                results['missing_headers'].append({
                    'header': header,
                    'description': config['description'],
                    'severity': config['severity'],
                    'recommendation': f"Add {header} header"
                })
                
    def _validate_csp(self, value: str) -> Tuple[str, List[str]]:
        """Validate Content Security Policy"""
        issues = []
        status = 'PASS'
        
        # Check for unsafe directives
        unsafe_patterns = [
            (r'unsafe-inline', 'Avoid unsafe-inline in script-src'),
            (r'unsafe-eval', 'Avoid unsafe-eval in script-src'),
            (r'\*', 'Avoid wildcards in CSP directives'),
            (r'data:', 'Be cautious with data: URIs')
        ]
        
        for pattern, message in unsafe_patterns:
            if re.search(pattern, value):
                issues.append(message)
                status = 'WARN'
                
        # Check for important directives
        important_directives = ['default-src', 'script-src', 'style-src']
        for directive in important_directives:
            if directive not in value:
                issues.append(f"Consider adding {directive} directive")
                
        # Check for report-uri or report-to
        if 'report-uri' not in value and 'report-to' not in value:
            issues.append("Consider adding reporting mechanism")
            
        return status, issues
        
    def _validate_permissions_policy(self, value: str) -> Tuple[str, List[str]]:
        """Validate Permissions Policy (formerly Feature Policy)"""
        issues = []
        status = 'PASS'
        
        # Check for overly permissive policies
        if '*' in value:
            issues.append("Avoid wildcards in Permissions-Policy")
            status = 'WARN'
            
        # Recommend restricting sensitive features
        sensitive_features = ['geolocation', 'camera', 'microphone', 'payment']
        for feature in sensitive_features:
            if feature not in value:
                issues.append(f"Consider explicitly restricting {feature}")
                
        return status, issues
        
    def _validate_hsts(self, value: str) -> Tuple[str, List[str]]:
        """Validate Strict-Transport-Security header"""
        issues = []
        status = 'PASS'
        
        # Parse max-age
        max_age_match = re.search(r'max-age=(\d+)', value)
        if max_age_match:
            max_age = int(max_age_match.group(1))
            if max_age < 31536000:  # Less than 1 year
                issues.append(f"max-age ({max_age}) should be at least 31536000 (1 year)")
                status = 'WARN'
        else:
            issues.append("max-age directive is missing")
            status = 'FAIL'
            
        # Check for includeSubDomains
        if 'includeSubDomains' not in value:
            issues.append("Consider adding includeSubDomains")
            
        # Check for preload
        if 'preload' not in value:
            issues.append("Consider adding preload directive")
            
        return status, issues
        
    def _validate_simple_header(self, value: str, recommended: any) -> Tuple[str, List[str]]:
        """Validate simple headers with exact or list matching"""
        issues = []
        status = 'FAIL'
        
        if recommended is None:
            status = 'PASS'
        elif isinstance(recommended, list):
            if value in recommended:
                status = 'PASS'
            else:
                issues.append(f"Value should be one of: {', '.join(recommended)}")
        else:
            if value == recommended:
                status = 'PASS'
            else:
                issues.append(f"Value should be: {recommended}")
                
        return status, issues
        
    def _calculate_grade(self, score: int, max_score: int) -> str:
        """Calculate letter grade based on score"""
        if max_score == 0:
            return 'F'
            
        percentage = (score / max_score) * 100
        
        if percentage >= 90:
            return 'A'
        elif percentage >= 80:
            return 'B'
        elif percentage >= 70:
            return 'C'
        elif percentage >= 60:
            return 'D'
        else:
            return 'F'
            
    def _add_recommendations(self, results: Dict):
        """Add general security recommendations"""
        # HTTPS recommendation
        if results['url'].startswith('http://'):
            results['recommendations'].append({
                'priority': 'HIGH',
                'recommendation': 'Use HTTPS instead of HTTP'
            })
            
        # Missing critical headers
        critical_missing = [h for h in results['missing_headers'] 
                          if h['severity'] == 'HIGH']
        if critical_missing:
            results['recommendations'].append({
                'priority': 'HIGH',
                'recommendation': f"Implement {len(critical_missing)} critical security headers"
            })
            
        # Dangerous headers present
        if results['dangerous_headers']:
            results['recommendations'].append({
                'priority': 'MEDIUM',
                'recommendation': 'Remove headers that expose server information'
            })
            
    def generate_report(self, results: Dict, format: str = 'text') -> str:
        """Generate formatted report"""
        if format == 'json':
            return json.dumps(results, indent=2)
        elif format == 'html':
            return self._generate_html_report(results)
        else:
            return self._generate_text_report(results)
            
    def _generate_text_report(self, results: Dict) -> str:
        """Generate text format report"""
        report = []
        report.append("=" * 80)
        report.append("SECURITY HEADERS VALIDATION REPORT")
        report.append("=" * 80)
        report.append(f"URL: {results['url']}")
        report.append(f"Scan Date: {results['timestamp']}")
        report.append(f"Score: {results['score']}/{results['max_score']} (Grade: {results['grade']})")
        report.append("")
        
        if results.get('error'):
            report.append(f"ERROR: {results['error']}")
            return '\n'.join(report)
            
        # Present headers
        if results['present_headers']:
            report.append("PRESENT HEADERS:")
            report.append("-" * 40)
            for header in results['present_headers']:
                status_symbol = '✓' if header['status'] == 'PASS' else '✗'
                report.append(f"{status_symbol} {header['header']}: {header['value'][:50]}...")
                if header['issues']:
                    for issue in header['issues']:
                        report.append(f"  - {issue}")
                        
        # Missing headers
        if results['missing_headers']:
            report.append("\nMISSING HEADERS:")
            report.append("-" * 40)
            for header in results['missing_headers']:
                report.append(f"✗ {header['header']} ({header['severity']})")
                report.append(f"  {header['description']}")
                
        # Dangerous headers
        if results['dangerous_headers']:
            report.append("\nDANGEROUS HEADERS FOUND:")
            report.append("-" * 40)
            for header in results['dangerous_headers']:
                report.append(f"⚠ {header['header']}: {header['value']}")
                report.append(f"  {header['recommendation']}")
                
        # Recommendations
        if results['recommendations']:
            report.append("\nRECOMMENDATIONS:")
            report.append("-" * 40)
            for rec in results['recommendations']:
                report.append(f"[{rec['priority']}] {rec['recommendation']}")
                
        report.append("=" * 80)
        return '\n'.join(report)
        
    def _generate_html_report(self, results: Dict) -> str:
        """Generate HTML format report"""
        grade_colors = {
            'A': '#4CAF50',
            'B': '#8BC34A', 
            'C': '#FFC107',
            'D': '#FF9800',
            'F': '#F44336'
        }
        
        html = f"""
<!DOCTYPE html>
<html>
<head>
    <title>Security Headers Report</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 900px;
            margin: 0 auto;
            background-color: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .grade {{
            display: inline-block;
            font-size: 48px;
            font-weight: bold;
            color: white;
            background-color: {grade_colors.get(results['grade'], '#999')};
            width: 80px;
            height: 80px;
            line-height: 80px;
            border-radius: 50%;
            margin: 10px;
        }}
        .score {{
            font-size: 20px;
            color: #666;
        }}
        .section {{
            margin: 20px 0;
            padding: 15px;
            background-color: #f9f9f9;
            border-radius: 5px;
        }}
        .pass {{ color: #4CAF50; }}
        .fail {{ color: #F44336; }}
        .warn {{ color: #FF9800; }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 10px 0;
        }}
        th, td {{
            padding: 10px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }}
        th {{
            background-color: #f0f0f0;
            font-weight: bold;
        }}
        .recommendation {{
            padding: 10px;
            margin: 5px 0;
            border-left: 4px solid #2196F3;
            background-color: #E3F2FD;
        }}
        .high {{ border-color: #F44336; background-color: #FFEBEE; }}
        .medium {{ border-color: #FF9800; background-color: #FFF3E0; }}
        .low {{ border-color: #4CAF50; background-color: #E8F5E9; }}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Security Headers Validation Report</h1>
            <div class="grade">{results['grade']}</div>
            <div class="score">Score: {results['score']}/{results['max_score']}</div>
            <p>URL: <a href="{results['url']}">{results['url']}</a></p>
            <p>Scan Date: {results['timestamp']}</p>
        </div>
"""
        
        # Present Headers Section
        if results['present_headers']:
            html += """
        <div class="section">
            <h2>Present Security Headers</h2>
            <table>
                <tr>
                    <th>Header</th>
                    <th>Value</th>
                    <th>Status</th>
                    <th>Issues</th>
                </tr>
"""
            for header in results['present_headers']:
                status_class = header['status'].lower()
                issues_html = '<br>'.join(header['issues']) if header['issues'] else 'None'
                html += f"""
                <tr>
                    <td><strong>{header['header']}</strong></td>
                    <td><code>{header['value'][:100]}{'...' if len(header['value']) > 100 else ''}</code></td>
                    <td class="{status_class}">{header['status']}</td>
                    <td>{issues_html}</td>
                </tr>
"""
            html += """
            </table>
        </div>
"""
        
        # Missing Headers Section
        if results['missing_headers']:
            html += """
        <div class="section">
            <h2>Missing Security Headers</h2>
            <table>
                <tr>
                    <th>Header</th>
                    <th>Severity</th>
                    <th>Description</th>
                </tr>
"""
            for header in results['missing_headers']:
                severity_class = header['severity'].lower()
                html += f"""
                <tr>
                    <td><strong>{header['header']}</strong></td>
                    <td class="{severity_class}">{header['severity']}</td>
                    <td>{header['description']}</td>
                </tr>
"""
            html += """
            </table>
        </div>
"""
        
        # Recommendations Section
        if results['recommendations']:
            html += """
        <div class="section">
            <h2>Recommendations</h2>
"""
            for rec in results['recommendations']:
                priority_class = rec['priority'].lower()
                html += f"""
            <div class="recommendation {priority_class}">
                <strong>[{rec['priority']}]</strong> {rec['recommendation']}
            </div>
"""
            html += """
        </div>
"""
        
        html += """
    </div>
</body>
</html>
"""
        return html

def main():
    parser = argparse.ArgumentParser(description='Security Headers Validation Tool')
    parser.add_argument('url', help='URL to check')
    parser.add_argument('--format', choices=['text', 'json', 'html'], 
                       default='text', help='Output format')
    parser.add_argument('--output', help='Output file (default: stdout)')
    parser.add_argument('--no-follow-redirects', action='store_true',
                       help='Do not follow redirects')
    
    args = parser.parse_args()
    
    # Ensure URL has protocol
    if not args.url.startswith(('http://', 'https://')):
        args.url = 'https://' + args.url
        
    # Create validator and check URL
    validator = SecurityHeadersValidator()
    results = validator.check_url(args.url, follow_redirects=not args.no_follow_redirects)
    
    # Generate report
    report = validator.generate_report(results, args.format)
    
    # Output report
    if args.output:
        with open(args.output, 'w') as f:
            f.write(report)
        print(f"Report saved to: {args.output}")
    else:
        print(report)
        
    # Exit with appropriate code
    if results.get('error'):
        sys.exit(1)
    elif results['grade'] in ['F', 'D']:
        sys.exit(2)
    else:
        sys.exit(0)

if __name__ == "__main__":
    main()