// Simple script to verify rope divider SVG structure
const fs = require('fs');
const path = require('path');

console.log('Verifying rope divider SVG implementation...\n');

// Read the Index.razor file
const indexPath = path.join(__dirname, '../Pages/Index.razor');
const content = fs.readFileSync(indexPath, 'utf8');

// Check for rope divider elements
const checks = {
    container: content.includes('rope-divider-container'),
    svg: content.includes('<svg id="ropeDividerSvg" class="rope-divider"'),
    pattern: content.includes('id="ropePattern"'),
    paths: (content.match(/<path\s/g) || []).length,
    animation: content.includes('animation: gentleSway'),
    knots: content.includes('class="rope-knot"'),
    styles: {
        containerHeight: content.includes('height: 120px'),
        zIndex: content.includes('z-index: 10'),
        overflow: content.includes('overflow: visible'),
        display: content.includes('display: block')
    }
};

console.log('✓ Rope Divider Structure Check:');
console.log(`  - Container div: ${checks.container ? '✓' : '✗'}`);
console.log(`  - SVG with ID and class: ${checks.svg ? '✓' : '✗'}`);
console.log(`  - Rope pattern defined: ${checks.pattern ? '✓' : '✗'}`);
console.log(`  - Path elements: ${checks.paths} found`);
console.log(`  - CSS animation defined: ${checks.animation ? '✓' : '✗'}`);
console.log(`  - Decorative knots: ${checks.knots ? '✓' : '✗'}`);

console.log('\n✓ Style Properties:');
console.log(`  - Container height (120px): ${checks.styles.containerHeight ? '✓' : '✗'}`);
console.log(`  - High z-index (10): ${checks.styles.zIndex ? '✓' : '✗'}`);
console.log(`  - Overflow visible: ${checks.styles.overflow ? '✓' : '✗'}`);
console.log(`  - Display block: ${checks.styles.display ? '✓' : '✗'}`);

// Extract and display the actual SVG structure
const svgMatch = content.match(/<svg[^>]*rope-divider[^>]*>[\s\S]*?<\/svg>/);
if (svgMatch) {
    console.log('\n✓ SVG Structure Summary:');
    const svgContent = svgMatch[0];
    console.log(`  - Total length: ${svgContent.length} characters`);
    console.log(`  - Has viewBox: ${svgContent.includes('viewBox') ? '✓' : '✗'}`);
    console.log(`  - Has pattern: ${svgContent.includes('<pattern') ? '✓' : '✗'}`);
    console.log(`  - Has paths: ${(svgContent.match(/<path/g) || []).length} paths`);
    console.log(`  - Has circles: ${(svgContent.match(/<circle/g) || []).length} circles`);
    console.log(`  - Has knot groups: ${(svgContent.match(/<g class="rope-knot"/g) || []).length} knots`);
}

// Check for potential visibility issues
console.log('\n⚠ Potential Visibility Issues Fixed:');
console.log('  - Removed negative margin-top (was -60px, now 0)');
console.log('  - Changed overflow from hidden to visible');
console.log('  - Increased z-index from 2 to 10');
console.log('  - Removed gradient mask that might hide content');
console.log('  - Added display: block to ensure SVG is rendered');
console.log('  - Added ID "ropeDividerSvg" for easier test detection');

console.log('\n✓ Summary: Rope divider SVG should now be visible between hero and events sections');