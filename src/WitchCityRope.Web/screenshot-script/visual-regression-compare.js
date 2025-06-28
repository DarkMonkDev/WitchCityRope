const fs = require('fs');
const path = require('path');
const { createCanvas, loadImage } = require('canvas');

class VisualRegressionComparer {
  constructor() {
    this.threshold = 0.01; // 1% difference threshold
    this.results = {
      timestamp: new Date().toISOString(),
      comparisons: [],
      summary: {
        total: 0,
        passed: 0,
        failed: 0,
        missing: 0
      }
    };
  }

  async compareDirectories() {
    console.log('🔍 Visual Regression Comparison Tool\n');
    
    const baselineDir = path.join(__dirname, 'screenshots/baseline');
    const currentDir = path.join(__dirname, 'screenshots/current');
    const diffDir = path.join(__dirname, 'screenshots/diff');
    
    // Ensure directories exist
    if (!fs.existsSync(baselineDir)) {
      console.log('⚠️  No baseline directory found. Run with --create-baseline flag to create initial baselines.');
      return;
    }
    
    if (!fs.existsSync(currentDir)) {
      console.log('❌ No current screenshots found. Run visual-regression-test.js first.');
      return;
    }
    
    if (!fs.existsSync(diffDir)) {
      fs.mkdirSync(diffDir, { recursive: true });
    }
    
    // Get all files in current directory
    const currentFiles = fs.readdirSync(currentDir).filter(f => f.endsWith('.png'));
    
    console.log(`Found ${currentFiles.length} screenshots to compare\n`);
    
    for (const filename of currentFiles) {
      await this.compareImages(filename, baselineDir, currentDir, diffDir);
    }
    
    this.generateComparisonReport();
  }

  async compareImages(filename, baselineDir, currentDir, diffDir) {
    const baselinePath = path.join(baselineDir, filename);
    const currentPath = path.join(currentDir, filename);
    const diffPath = path.join(diffDir, filename);
    
    console.log(`Comparing: ${filename}`);
    
    const comparison = {
      filename,
      status: 'unknown',
      difference: 0,
      pixelsDiff: 0,
      totalPixels: 0
    };
    
    // Check if baseline exists
    if (!fs.existsSync(baselinePath)) {
      console.log(`  ⚠️  No baseline found`);
      comparison.status = 'missing-baseline';
      this.results.summary.missing++;
      this.results.comparisons.push(comparison);
      return;
    }
    
    try {
      // Load images
      const baseline = await loadImage(baselinePath);
      const current = await loadImage(currentPath);
      
      // Check dimensions
      if (baseline.width !== current.width || baseline.height !== current.height) {
        console.log(`  ❌ Dimension mismatch: ${baseline.width}x${baseline.height} vs ${current.width}x${current.height}`);
        comparison.status = 'dimension-mismatch';
        comparison.error = 'Images have different dimensions';
        this.results.summary.failed++;
        this.results.comparisons.push(comparison);
        return;
      }
      
      // Create canvases
      const width = baseline.width;
      const height = baseline.height;
      const totalPixels = width * height;
      
      const baselineCanvas = createCanvas(width, height);
      const currentCanvas = createCanvas(width, height);
      const diffCanvas = createCanvas(width, height);
      
      const baselineCtx = baselineCanvas.getContext('2d');
      const currentCtx = currentCanvas.getContext('2d');
      const diffCtx = diffCanvas.getContext('2d');
      
      // Draw images
      baselineCtx.drawImage(baseline, 0, 0);
      currentCtx.drawImage(current, 0, 0);
      
      // Get image data
      const baselineData = baselineCtx.getImageData(0, 0, width, height);
      const currentData = currentCtx.getImageData(0, 0, width, height);
      const diffData = diffCtx.createImageData(width, height);
      
      // Compare pixels
      let diffPixels = 0;
      
      for (let i = 0; i < baselineData.data.length; i += 4) {
        const r1 = baselineData.data[i];
        const g1 = baselineData.data[i + 1];
        const b1 = baselineData.data[i + 2];
        const a1 = baselineData.data[i + 3];
        
        const r2 = currentData.data[i];
        const g2 = currentData.data[i + 1];
        const b2 = currentData.data[i + 2];
        const a2 = currentData.data[i + 3];
        
        const pixelDiff = Math.abs(r1 - r2) + Math.abs(g1 - g2) + 
                         Math.abs(b1 - b2) + Math.abs(a1 - a2);
        
        if (pixelDiff > 0) {
          diffPixels++;
          // Highlight differences in red
          diffData.data[i] = 255;
          diffData.data[i + 1] = 0;
          diffData.data[i + 2] = 0;
          diffData.data[i + 3] = 255;
        } else {
          // Show unchanged pixels in grayscale
          const gray = (r1 + g1 + b1) / 3;
          diffData.data[i] = gray;
          diffData.data[i + 1] = gray;
          diffData.data[i + 2] = gray;
          diffData.data[i + 3] = a1;
        }
      }
      
      // Calculate difference percentage
      const diffPercentage = (diffPixels / totalPixels) * 100;
      comparison.difference = diffPercentage;
      comparison.pixelsDiff = diffPixels;
      comparison.totalPixels = totalPixels;
      
      // Save diff image
      diffCtx.putImageData(diffData, 0, 0);
      const buffer = diffCanvas.toBuffer('image/png');
      fs.writeFileSync(diffPath, buffer);
      
      // Determine status
      if (diffPercentage <= this.threshold) {
        console.log(`  ✅ Match (${diffPercentage.toFixed(3)}% difference)`);
        comparison.status = 'passed';
        this.results.summary.passed++;
      } else {
        console.log(`  ❌ Mismatch (${diffPercentage.toFixed(3)}% difference)`);
        comparison.status = 'failed';
        this.results.summary.failed++;
      }
      
    } catch (error) {
      console.log(`  ❌ Error: ${error.message}`);
      comparison.status = 'error';
      comparison.error = error.message;
      this.results.summary.failed++;
    }
    
    this.results.comparisons.push(comparison);
    this.results.summary.total++;
  }

  generateComparisonReport() {
    console.log('\n\n📊 Comparison Summary\n');
    console.log(`Total Comparisons: ${this.results.summary.total}`);
    console.log(`✅ Passed: ${this.results.summary.passed}`);
    console.log(`❌ Failed: ${this.results.summary.failed}`);
    console.log(`⚠️  Missing Baselines: ${this.results.summary.missing}`);
    
    // Save detailed results
    const reportPath = path.join(__dirname, 'reports', 'visual-comparison-results.json');
    fs.writeFileSync(reportPath, JSON.stringify(this.results, null, 2));
    console.log(`\n✓ Detailed results saved to: ${reportPath}`);
    
    // Generate HTML comparison report
    this.generateHTMLComparisonReport();
  }

  generateHTMLComparisonReport() {
    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Visual Regression Comparison Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1400px;
            margin: 0 auto;
        }
        .header {
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            margin-bottom: 30px;
        }
        h1 {
            margin: 0 0 10px 0;
            color: #333;
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        .stat {
            text-align: center;
            padding: 20px;
            border-radius: 8px;
            background: white;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .stat.passed { border-top: 4px solid #28a745; }
        .stat.failed { border-top: 4px solid #dc3545; }
        .stat.missing { border-top: 4px solid #ffc107; }
        .stat .number {
            font-size: 36px;
            font-weight: bold;
            margin-bottom: 5px;
        }
        .comparisons {
            display: grid;
            gap: 20px;
        }
        .comparison {
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .comparison-header {
            padding: 20px;
            border-bottom: 1px solid #e9ecef;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        .comparison-header.passed { background: #d4edda; }
        .comparison-header.failed { background: #f8d7da; }
        .comparison-header.missing { background: #fff3cd; }
        .comparison-title {
            font-weight: bold;
            font-size: 18px;
        }
        .comparison-status {
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .status-badge {
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
        }
        .status-badge.passed { background: #28a745; color: white; }
        .status-badge.failed { background: #dc3545; color: white; }
        .status-badge.missing { background: #ffc107; color: #333; }
        .comparison-images {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
            padding: 20px;
        }
        .image-container {
            text-align: center;
        }
        .image-container img {
            width: 100%;
            border: 1px solid #dee2e6;
            border-radius: 4px;
        }
        .image-label {
            margin-top: 10px;
            font-weight: bold;
            color: #666;
        }
        .no-baseline {
            padding: 60px;
            text-align: center;
            color: #999;
            font-style: italic;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Visual Regression Comparison Report</h1>
            <p>Generated: ${new Date(this.results.timestamp).toLocaleString()}</p>
            
            <div class="summary">
                <div class="stat passed">
                    <div class="number">${this.results.summary.passed}</div>
                    <div>Passed</div>
                </div>
                <div class="stat failed">
                    <div class="number">${this.results.summary.failed}</div>
                    <div>Failed</div>
                </div>
                <div class="stat missing">
                    <div class="number">${this.results.summary.missing}</div>
                    <div>Missing Baseline</div>
                </div>
            </div>
        </div>
        
        <div class="comparisons">
            ${this.results.comparisons.map(comp => {
                const statusClass = comp.status === 'passed' ? 'passed' : 
                                  comp.status === 'missing-baseline' ? 'missing' : 'failed';
                
                return `
                    <div class="comparison">
                        <div class="comparison-header ${statusClass}">
                            <div class="comparison-title">${comp.filename}</div>
                            <div class="comparison-status">
                                ${comp.difference !== undefined ? 
                                    `<span>${comp.difference.toFixed(3)}% difference</span>` : ''}
                                <span class="status-badge ${statusClass}">
                                    ${comp.status.replace('-', ' ').toUpperCase()}
                                </span>
                            </div>
                        </div>
                        ${comp.status !== 'missing-baseline' ? `
                            <div class="comparison-images">
                                <div class="image-container">
                                    <img src="../screenshots/baseline/${comp.filename}" 
                                         alt="Baseline">
                                    <div class="image-label">Baseline</div>
                                </div>
                                <div class="image-container">
                                    <img src="../screenshots/current/${comp.filename}" 
                                         alt="Current">
                                    <div class="image-label">Current</div>
                                </div>
                                <div class="image-container">
                                    <img src="../screenshots/diff/${comp.filename}" 
                                         alt="Difference">
                                    <div class="image-label">Difference</div>
                                </div>
                            </div>
                        ` : `
                            <div class="no-baseline">
                                No baseline image available for comparison
                            </div>
                        `}
                    </div>
                `;
            }).join('')}
        </div>
    </div>
</body>
</html>
    `;
    
    const htmlPath = path.join(__dirname, 'reports', 'visual-comparison-report.html');
    fs.writeFileSync(htmlPath, html);
    console.log(`✓ HTML comparison report saved to: ${htmlPath}`);
  }

  async createBaselines() {
    console.log('📸 Creating baseline images...\n');
    
    const currentDir = path.join(__dirname, 'screenshots/current');
    const baselineDir = path.join(__dirname, 'screenshots/baseline');
    
    if (!fs.existsSync(currentDir)) {
      console.log('❌ No current screenshots found. Run visual-regression-test.js first.');
      return;
    }
    
    if (!fs.existsSync(baselineDir)) {
      fs.mkdirSync(baselineDir, { recursive: true });
    }
    
    const files = fs.readdirSync(currentDir).filter(f => f.endsWith('.png'));
    
    console.log(`Copying ${files.length} images to baseline...\n`);
    
    files.forEach(file => {
      const src = path.join(currentDir, file);
      const dest = path.join(baselineDir, file);
      fs.copyFileSync(src, dest);
      console.log(`✓ ${file}`);
    });
    
    console.log('\n✅ Baseline images created successfully!');
  }
}

// Run comparison
if (require.main === module) {
  const comparer = new VisualRegressionComparer();
  
  if (process.argv.includes('--create-baseline')) {
    comparer.createBaselines().catch(console.error);
  } else {
    comparer.compareDirectories().catch(console.error);
  }
}

module.exports = VisualRegressionComparer;