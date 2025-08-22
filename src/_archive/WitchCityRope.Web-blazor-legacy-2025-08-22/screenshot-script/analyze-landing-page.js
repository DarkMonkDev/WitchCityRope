const puppeteer = require('puppeteer');
const fs = require('fs').promises;
const path = require('path');

async function analyzeLandingPage() {
    console.log('Starting landing page analysis...');
    const browser = await puppeteer.launch({
        headless: 'new',
        args: ['--no-sandbox', '--disable-setuid-sandbox']
    });

    try {
        const page = await browser.newPage();
        await page.setViewport({ width: 1920, height: 1080 });

        console.log('Navigating to http://localhost:5651...');
        await page.goto('http://localhost:5651', { 
            waitUntil: 'networkidle2',
            timeout: 30000 
        });

        // Take full-page screenshot
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const screenshotPath = path.join(__dirname, 'screenshots', `landing-page-fullpage-${timestamp}.png`);
        await page.screenshot({ 
            path: screenshotPath,
            fullPage: true 
        });
        console.log(`Full-page screenshot saved to: ${screenshotPath}`);

        // Extract page content for analysis
        const pageAnalysis = await page.evaluate(() => {
            const analysis = {
                title: document.title,
                heroSection: {},
                navigation: {},
                featuresSection: {},
                ropeDivider: {},
                footer: {},
                colorScheme: {},
                discrepancies: []
            };

            // Analyze Hero Section
            const heroSection = document.querySelector('.hero-section, [class*="hero"], section:first-of-type');
            if (heroSection) {
                const tagline = heroSection.querySelector('h2, .tagline, [class*="tagline"]');
                analysis.heroSection.found = true;
                analysis.heroSection.tagline = tagline ? tagline.textContent.trim() : null;
                analysis.heroSection.hasCorrectTagline = tagline && tagline.textContent.includes('Where curiosity meets connection');
                
                if (!analysis.heroSection.hasCorrectTagline) {
                    analysis.discrepancies.push(`Hero tagline mismatch. Expected: "Where curiosity meets connection", Found: "${analysis.heroSection.tagline}"`);
                }

                // Check for multi-line title
                const titles = heroSection.querySelectorAll('h1, .hero-title, [class*="title"]');
                analysis.heroSection.titles = Array.from(titles).map(t => t.textContent.trim());
                analysis.heroSection.hasEducationPracticeEmphasis = Array.from(titles).some(t => 
                    t.includes('Education') && t.includes('Practice')
                );

                if (!analysis.heroSection.hasEducationPracticeEmphasis) {
                    analysis.discrepancies.push('Multi-line title missing emphasis on "Education & Practice"');
                }
            } else {
                analysis.heroSection.found = false;
                analysis.discrepancies.push('Hero section not found');
            }

            // Analyze Navigation
            const nav = document.querySelector('nav, .navbar, [class*="nav"]');
            if (nav) {
                const loginLink = nav.querySelector('a[href*="login"], button:has-text("Login"), [class*="login"]');
                const signupLink = nav.querySelector('a[href*="signup"], a[href*="register"], button:has-text("Sign Up"), [class*="signup"]');
                
                analysis.navigation.found = true;
                analysis.navigation.hasLogin = !!loginLink;
                analysis.navigation.hasSignUp = !!signupLink;
                analysis.navigation.loginText = loginLink ? loginLink.textContent.trim() : null;
                analysis.navigation.signupText = signupLink ? signupLink.textContent.trim() : null;

                if (!analysis.navigation.hasLogin || !analysis.navigation.hasSignUp) {
                    analysis.discrepancies.push('Navigation missing Login/Sign Up links for non-authenticated users');
                }
            } else {
                analysis.navigation.found = false;
                analysis.discrepancies.push('Navigation not found');
            }

            // Analyze Rope SVG Divider
            const svgDividers = document.querySelectorAll('svg, [class*="rope"], [class*="divider"]');
            analysis.ropeDivider.found = svgDividers.length > 0;
            analysis.ropeDivider.count = svgDividers.length;
            
            if (!analysis.ropeDivider.found) {
                analysis.discrepancies.push('Rope SVG divider not found between sections');
            }

            // Analyze Features Section
            const featuresSection = Array.from(document.querySelectorAll('section, .features, [class*="features"]'))
                .find(el => el.textContent.includes('What Makes Our Community Special'));
            
            if (featuresSection) {
                analysis.featuresSection.found = true;
                analysis.featuresSection.hasCorrectTitle = true;
                const featureCards = featuresSection.querySelectorAll('.feature-card, .card, [class*="feature"]');
                analysis.featuresSection.featureCount = featureCards.length;
            } else {
                analysis.featuresSection.found = false;
                analysis.discrepancies.push('Features section with title "What Makes Our Community Special" not found');
            }

            // Analyze Footer
            const footer = document.querySelector('footer, .footer, [class*="footer"]');
            if (footer) {
                analysis.footer.found = true;
                const sections = footer.querySelectorAll('.footer-section, .col, [class*="col"]');
                analysis.footer.sectionCount = sections.length;
                analysis.footer.hasCorrectSectionCount = sections.length === 4;
                
                if (!analysis.footer.hasCorrectSectionCount) {
                    analysis.discrepancies.push(`Footer should have 4 sections, found ${sections.length}`);
                }
            } else {
                analysis.footer.found = false;
                analysis.discrepancies.push('Footer not found');
            }

            // Analyze Color Scheme
            const computedStyles = getComputedStyle(document.documentElement);
            const bodyStyles = getComputedStyle(document.body);
            
            analysis.colorScheme.primaryColors = {
                background: bodyStyles.backgroundColor,
                text: bodyStyles.color,
                cssVariables: {}
            };

            // Check for CSS variables
            const cssVars = ['--primary', '--secondary', '--accent', '--burgundy', '--amber', '--purple'];
            cssVars.forEach(varName => {
                const value = computedStyles.getPropertyValue(varName);
                if (value) {
                    analysis.colorScheme.cssVariables[varName] = value.trim();
                }
            });

            // Check for gradient usage
            const elementsWithGradients = Array.from(document.querySelectorAll('*')).filter(el => {
                const bg = getComputedStyle(el).backgroundImage;
                return bg && bg.includes('gradient');
            });
            analysis.colorScheme.hasGradients = elementsWithGradients.length > 0;
            analysis.colorScheme.gradientCount = elementsWithGradients.length;

            // Check color scheme
            const hasExpectedColors = Object.values(analysis.colorScheme.cssVariables).some(color => 
                color.includes('burgundy') || color.includes('#8B') || color.includes('#7') || 
                color.includes('amber') || color.includes('#FF') || color.includes('#F') ||
                color.includes('purple') || color.includes('#9') || color.includes('#8')
            );

            if (!hasExpectedColors && !analysis.colorScheme.hasGradients) {
                analysis.discrepancies.push('Color scheme does not match wireframe (missing burgundy, amber, purple gradients)');
            }

            return analysis;
        });

        // Save analysis report
        const reportPath = path.join(__dirname, 'landing-page-analysis-report.json');
        await fs.writeFile(reportPath, JSON.stringify(pageAnalysis, null, 2));
        console.log(`Analysis report saved to: ${reportPath}`);

        // Generate markdown report
        const markdownReport = generateMarkdownReport(pageAnalysis, screenshotPath);
        const markdownPath = path.join(__dirname, 'landing-page-analysis.md');
        await fs.writeFile(markdownPath, markdownReport);
        console.log(`Markdown report saved to: ${markdownPath}`);

        return pageAnalysis;
    } catch (error) {
        console.error('Error during analysis:', error);
        throw error;
    } finally {
        await browser.close();
    }
}

function generateMarkdownReport(analysis, screenshotPath) {
    const timestamp = new Date().toISOString();
    
    let report = `# Landing Page Analysis Report\n\n`;
    report += `**Generated:** ${timestamp}\n\n`;
    report += `**Screenshot:** ${path.basename(screenshotPath)}\n\n`;
    
    report += `## Summary\n\n`;
    report += `- **Total Discrepancies Found:** ${analysis.discrepancies.length}\n`;
    report += `- **Page Title:** ${analysis.title}\n\n`;
    
    report += `## Wireframe Compliance Check\n\n`;
    
    // Hero Section
    report += `### 1. Hero Section\n`;
    report += `- **Found:** ${analysis.heroSection.found ? '✅' : '❌'}\n`;
    report += `- **Tagline:** ${analysis.heroSection.tagline || 'Not found'}\n`;
    report += `- **Correct Tagline ("Where curiosity meets connection"):** ${analysis.heroSection.hasCorrectTagline ? '✅' : '❌'}\n`;
    report += `- **Multi-line Title with Education & Practice emphasis:** ${analysis.heroSection.hasEducationPracticeEmphasis ? '✅' : '❌'}\n`;
    report += `- **Titles found:** ${analysis.heroSection.titles ? analysis.heroSection.titles.join(', ') : 'None'}\n\n`;
    
    // Navigation
    report += `### 2. Navigation\n`;
    report += `- **Found:** ${analysis.navigation.found ? '✅' : '❌'}\n`;
    report += `- **Login Link:** ${analysis.navigation.hasLogin ? '✅' : '❌'} ${analysis.navigation.loginText ? `(${analysis.navigation.loginText})` : ''}\n`;
    report += `- **Sign Up Link:** ${analysis.navigation.hasSignUp ? '✅' : '❌'} ${analysis.navigation.signupText ? `(${analysis.navigation.signupText})` : ''}\n\n`;
    
    // Rope Divider
    report += `### 3. Rope SVG Divider\n`;
    report += `- **Found:** ${analysis.ropeDivider.found ? '✅' : '❌'}\n`;
    report += `- **Count:** ${analysis.ropeDivider.count}\n\n`;
    
    // Features Section
    report += `### 4. Features Section\n`;
    report += `- **Found:** ${analysis.featuresSection.found ? '✅' : '❌'}\n`;
    report += `- **Correct Title ("What Makes Our Community Special"):** ${analysis.featuresSection.hasCorrectTitle ? '✅' : '❌'}\n`;
    report += `- **Feature Cards:** ${analysis.featuresSection.featureCount || 0}\n\n`;
    
    // Footer
    report += `### 5. Footer\n`;
    report += `- **Found:** ${analysis.footer.found ? '✅' : '❌'}\n`;
    report += `- **Section Count:** ${analysis.footer.sectionCount} (Expected: 4)\n`;
    report += `- **Correct Section Count:** ${analysis.footer.hasCorrectSectionCount ? '✅' : '❌'}\n\n`;
    
    // Color Scheme
    report += `### 6. Color Scheme\n`;
    report += `- **Has Gradients:** ${analysis.colorScheme.hasGradients ? '✅' : '❌'} (${analysis.colorScheme.gradientCount} elements)\n`;
    report += `- **CSS Variables:**\n`;
    Object.entries(analysis.colorScheme.cssVariables).forEach(([key, value]) => {
        report += `  - ${key}: ${value}\n`;
    });
    report += `\n`;
    
    // Discrepancies
    report += `## Discrepancies Found\n\n`;
    if (analysis.discrepancies.length === 0) {
        report += `No discrepancies found! The landing page matches all wireframe requirements.\n`;
    } else {
        analysis.discrepancies.forEach((discrepancy, index) => {
            report += `${index + 1}. ${discrepancy}\n`;
        });
    }
    
    return report;
}

// Run the analysis
analyzeLandingPage()
    .then(() => console.log('Analysis complete!'))
    .catch(error => {
        console.error('Analysis failed:', error);
        process.exit(1);
    });