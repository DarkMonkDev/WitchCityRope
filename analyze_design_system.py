#!/usr/bin/env python3
import os
import re
from collections import defaultdict, Counter
from pathlib import Path

def extract_css_values(content, property_pattern):
    """Extract CSS property values from content"""
    values = []
    # Match inline styles
    inline_pattern = rf'{property_pattern}:\s*([^;}}]+)'
    values.extend(re.findall(inline_pattern, content, re.IGNORECASE))
    return values

def parse_spacing_value(value):
    """Parse spacing values and convert to px if possible"""
    value = value.strip()
    if value == '0' or value == 'auto':
        return value
    
    # Extract numeric values with units
    matches = re.findall(r'(\d+(?:\.\d+)?)(px|em|rem|%)?', value)
    parsed_values = []
    
    for match in matches:
        num, unit = match
        if unit == '' or unit == 'px':
            parsed_values.append(f"{num}px")
        else:
            parsed_values.append(f"{num}{unit}")
    
    return ' '.join(parsed_values) if parsed_values else value

def analyze_html_files(directory):
    """Analyze all HTML files in directory"""
    results = {
        'padding': Counter(),
        'margin': Counter(),
        'gap': Counter(),
        'border-radius': Counter(),
        'max-width': Counter(),
        'width': Counter(),
        'grid-template-columns': [],
        'flex-patterns': [],
        'breakpoints': set(),
        'file_analysis': {}
    }
    
    html_files = list(Path(directory).glob('*.html'))
    
    for file_path in html_files:
        print(f"Analyzing: {file_path.name}")
        
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        file_results = {
            'padding': Counter(),
            'margin': Counter(),
            'gap': Counter(),
            'border-radius': Counter(),
            'max-width': Counter(),
            'layout_patterns': []
        }
        
        # Extract padding values
        padding_values = extract_css_values(content, 'padding(?:-(?:top|right|bottom|left))?')
        for val in padding_values:
            parsed = parse_spacing_value(val)
            file_results['padding'][parsed] += 1
            results['padding'][parsed] += 1
        
        # Extract margin values
        margin_values = extract_css_values(content, 'margin(?:-(?:top|right|bottom|left))?')
        for val in margin_values:
            parsed = parse_spacing_value(val)
            file_results['margin'][parsed] += 1
            results['margin'][parsed] += 1
        
        # Extract gap values
        gap_values = extract_css_values(content, 'gap')
        for val in gap_values:
            parsed = parse_spacing_value(val)
            file_results['gap'][parsed] += 1
            results['gap'][parsed] += 1
        
        # Extract border-radius values
        radius_values = extract_css_values(content, 'border-radius')
        for val in radius_values:
            parsed = parse_spacing_value(val)
            file_results['border-radius'][parsed] += 1
            results['border-radius'][parsed] += 1
        
        # Extract max-width values
        max_width_values = extract_css_values(content, 'max-width')
        for val in max_width_values:
            parsed = parse_spacing_value(val)
            file_results['max-width'][parsed] += 1
            results['max-width'][parsed] += 1
        
        # Extract grid patterns
        grid_patterns = re.findall(r'grid-template-columns:\s*([^;]+);', content, re.IGNORECASE)
        results['grid-template-columns'].extend(grid_patterns)
        
        # Extract flex patterns
        if 'display: flex' in content or 'display:flex' in content:
            flex_count = content.count('display: flex') + content.count('display:flex')
            file_results['layout_patterns'].append(f'flexbox ({flex_count} instances)')
        
        if 'display: grid' in content or 'display:grid' in content:
            grid_count = content.count('display: grid') + content.count('display:grid')
            file_results['layout_patterns'].append(f'grid ({grid_count} instances)')
        
        # Extract media query breakpoints
        breakpoints = re.findall(r'@media\s*\([^)]*(?:max-width|min-width):\s*(\d+px)[^)]*\)', content)
        results['breakpoints'].update(breakpoints)
        
        results['file_analysis'][file_path.name] = file_results
    
    return results

def find_base_unit(spacing_values):
    """Find the most likely base spacing unit"""
    # Extract numeric values in px
    px_values = []
    for value, count in spacing_values.items():
        if 'px' in value and ' ' not in value:  # Single value in px
            try:
                num = int(re.findall(r'(\d+)px', value)[0])
                px_values.extend([num] * count)
            except:
                pass
    
    if not px_values:
        return None
    
    # Find GCD of all values
    from math import gcd
    from functools import reduce
    
    # Filter out 0 and very large values
    px_values = [v for v in px_values if 0 < v < 200]
    
    if not px_values:
        return None
    
    # Find common divisors
    divisor_counts = Counter()
    for divisor in [4, 5, 8, 10]:
        count = sum(1 for v in px_values if v % divisor == 0)
        if count > len(px_values) * 0.7:  # At least 70% divisible
            divisor_counts[divisor] = count
    
    if divisor_counts:
        return max(divisor_counts, key=divisor_counts.get)
    
    return None

def generate_report(results):
    """Generate structured report"""
    report = []
    report.append("# Design System Analysis Report")
    report.append("=" * 80)
    report.append("")
    
    # Spacing Analysis
    report.append("## 1. SPACING ANALYSIS")
    report.append("-" * 40)
    
    # Combine padding and margin for base unit analysis
    all_spacing = Counter()
    all_spacing.update(results['padding'])
    all_spacing.update(results['margin'])
    
    base_unit = find_base_unit(all_spacing)
    if base_unit:
        report.append(f"\n**Detected Base Unit: {base_unit}px**")
    
    # Padding values
    report.append("\n### Padding Values (sorted by frequency):")
    for value, count in results['padding'].most_common(15):
        report.append(f"  - {value}: {count} times")
    
    # Margin values
    report.append("\n### Margin Values (sorted by frequency):")
    for value, count in results['margin'].most_common(15):
        report.append(f"  - {value}: {count} times")
    
    # Gap values
    if results['gap']:
        report.append("\n### Gap Values (Flexbox/Grid):")
        for value, count in results['gap'].most_common():
            report.append(f"  - {value}: {count} times")
    
    # Layout Patterns
    report.append("\n## 2. LAYOUT PATTERNS")
    report.append("-" * 40)
    
    # Container widths
    report.append("\n### Container Max-Width Values:")
    for value, count in results['max-width'].most_common():
        report.append(f"  - {value}: {count} times")
    
    # Grid patterns
    if results['grid-template-columns']:
        report.append("\n### Grid Template Patterns:")
        grid_counter = Counter(results['grid-template-columns'])
        for pattern, count in grid_counter.most_common():
            report.append(f"  - {pattern.strip()}: {count} times")
    
    # Breakpoints
    if results['breakpoints']:
        report.append("\n### Media Query Breakpoints:")
        for bp in sorted(results['breakpoints'], key=lambda x: int(x.replace('px', ''))):
            report.append(f"  - {bp}")
    
    # Border Radius
    report.append("\n## 3. BORDER RADIUS VALUES")
    report.append("-" * 40)
    for value, count in results['border-radius'].most_common():
        report.append(f"  - {value}: {count} times")
    
    # Spacing Scale
    report.append("\n## 4. SPACING SCALE RECOMMENDATION")
    report.append("-" * 40)
    
    if base_unit:
        scale = []
        px_values = set()
        for value in all_spacing.keys():
            if 'px' in value and ' ' not in value:
                try:
                    num = int(re.findall(r'(\d+)px', value)[0])
                    if num > 0 and num % base_unit == 0:
                        px_values.add(num)
                except:
                    pass
        
        if px_values:
            report.append(f"\nBased on {base_unit}px base unit, recommended scale:")
            for val in sorted(px_values)[:12]:
                multiplier = val // base_unit
                report.append(f"  - {val}px ({multiplier}x base)")
    
    # Inconsistencies
    report.append("\n## 5. POTENTIAL INCONSISTENCIES")
    report.append("-" * 40)
    
    # Find similar but different values
    report.append("\n### Similar Spacing Values:")
    spacing_nums = []
    for value in all_spacing.keys():
        if 'px' in value and ' ' not in value:
            try:
                num = int(re.findall(r'(\d+)px', value)[0])
                spacing_nums.append(num)
            except:
                pass
    
    spacing_nums = sorted(set(spacing_nums))
    for i in range(len(spacing_nums) - 1):
        diff = spacing_nums[i+1] - spacing_nums[i]
        if 0 < diff <= 4:
            report.append(f"  - {spacing_nums[i]}px and {spacing_nums[i+1]}px (differ by {diff}px)")
    
    # File-specific patterns
    report.append("\n## 6. FILE-SPECIFIC PATTERNS")
    report.append("-" * 40)
    
    for filename, file_data in sorted(results['file_analysis'].items()):
        if file_data['layout_patterns']:
            report.append(f"\n### {filename}")
            report.append(f"  Layout: {', '.join(file_data['layout_patterns'])}")
            
            # Most common spacing in this file
            top_padding = file_data['padding'].most_common(3)
            top_margin = file_data['margin'].most_common(3)
            
            if top_padding:
                report.append(f"  Top padding: {', '.join([f'{v} ({c}x)' for v, c in top_padding])}")
            if top_margin:
                report.append(f"  Top margin: {', '.join([f'{v} ({c}x)' for v, c in top_margin])}")
    
    return '\n'.join(report)

if __name__ == "__main__":
    directory = "/mnt/c/Users/chad/source/repos/WitchCityRope/docs/design/wireframes"
    results = analyze_html_files(directory)
    report = generate_report(results)
    
    # Save report
    report_path = os.path.join(directory, "design_system_analysis.txt")
    with open(report_path, 'w') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_path}")
    print("\n" + "=" * 80)
    print(report)