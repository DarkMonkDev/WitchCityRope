const chokidar = require('chokidar');
const { exec } = require('child_process');
const fs = require('fs').promises;
const path = require('path');

class WitchCityRopeUIMonitor {
  constructor() {
    this.baseUrl = 'https://localhost:5652';
    this.screenshotDir = './ui-snapshots';
    this.watchPaths = [
      'src/WitchCityRope.Web/Features',
      'src/WitchCityRope.Web/Pages',
      'src/WitchCityRope.Web/wwwroot/css',
      'src/WitchCityRope.Web/Components'
    ];
    this.recentChanges = [];
  }

  async init() {
    console.log('🎭 WitchCityRope UI Monitor Starting...');
    await fs.mkdir(this.screenshotDir, { recursive: true });
    await fs.mkdir(path.join(this.screenshotDir, 'metadata'), { recursive: true });
    this.startWatching();
    console.log(`📸 Monitoring changes and capturing to ${this.screenshotDir}`);
  }

  startWatching() {
    const watcher = chokidar.watch(this.watchPaths, {
      ignored: /(^|[\/\\])\..|(bin|obj|node_modules)/,
      persistent: true,
      ignoreInitial: true
    });

    watcher
      .on('change', async (filepath) => {
        console.log(`📝 File changed: ${filepath}`);
        this.recentChanges.push({
          file: filepath,
          timestamp: new Date().toISOString(),
          type: 'modified'
        });
        
        // Keep only last 10 changes
        if (this.recentChanges.length > 10) {
          this.recentChanges.shift();
        }
        
        const pages = this.determinePagesFromFile(filepath);
        for (const page of pages) {
          await this.captureSnapshot(page, filepath);
        }
      })
      .on('add', (filepath) => {
        console.log(`➕ File added: ${filepath}`);
        this.recentChanges.push({
          file: filepath,
          timestamp: new Date().toISOString(),
          type: 'added'
        });
      })
      .on('unlink', (filepath) => {
        console.log(`➖ File removed: ${filepath}`);
        this.recentChanges.push({
          file: filepath,
          timestamp: new Date().toISOString(),
          type: 'removed'
        });
      });
  }

  determinePagesFromFile(filepath) {
    const pages = [];
    
    // Map file paths to relevant pages
    if (filepath.includes('Layout') || filepath.includes('MainLayout')) {
      // Layout changes affect all pages
      pages.push('/', '/events', '/dashboard', '/admin');
    } else if (filepath.includes('Auth') || filepath.includes('Login') || filepath.includes('Register')) {
      pages.push('/login', '/register');
    } else if (filepath.includes('Event')) {
      pages.push('/events', '/events/1', '/admin/events');
    } else if (filepath.includes('Vetting')) {
      pages.push('/vetting/apply', '/admin/vetting');
    } else if (filepath.includes('Dashboard')) {
      pages.push('/dashboard');
    } else if (filepath.includes('Admin')) {
      pages.push('/admin', '/admin/events', '/admin/vetting', '/admin/users');
    } else if (filepath.includes('CheckIn')) {
      pages.push('/events/1/checkin');
    } else if (filepath.includes('Profile') || filepath.includes('Settings')) {
      pages.push('/profile', '/settings');
    } else if (filepath.includes('.css')) {
      // CSS changes could affect any page
      pages.push('/');
    } else {
      // Default to homepage
      pages.push('/');
    }
    
    return [...new Set(pages)]; // Remove duplicates
  }

  async captureSnapshot(page, triggerFile) {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const pageName = page === '/' ? 'home' : page.substring(1).replace(/\//g, '-');
    const filename = `${pageName}-${timestamp}`;
    
    console.log(`📸 Capturing ${this.baseUrl}${page}`);
    
    const metadata = {
      url: `${this.baseUrl}${page}`,
      page: page,
      timestamp: new Date().toISOString(),
      triggerFile: triggerFile,
      filename: `${filename}.png`,
      recentChanges: this.recentChanges.slice(-5), // Last 5 changes
      captureReason: this.getCaptureReason(triggerFile)
    };
    
    // Save metadata
    await fs.writeFile(
      path.join(this.screenshotDir, 'metadata', `${filename}.json`),
      JSON.stringify(metadata, null, 2)
    );
    
    // Log for MCP integration
    console.log(`✅ Ready for screenshot: ${JSON.stringify({
      url: metadata.url,
      filename: metadata.filename,
      reason: metadata.captureReason
    })}`);
  }

  getCaptureReason(filepath) {
    if (filepath.includes('.razor')) return 'Razor component changed';
    if (filepath.includes('.cs')) return 'C# code changed';
    if (filepath.includes('.css')) return 'Styles updated';
    if (filepath.includes('.js')) return 'JavaScript changed';
    return 'File modified';
  }

  async generateReport() {
    const files = await fs.readdir(path.join(this.screenshotDir, 'metadata'));
    const snapshots = [];
    
    for (const file of files) {
      if (file.endsWith('.json')) {
        const content = await fs.readFile(
          path.join(this.screenshotDir, 'metadata', file),
          'utf-8'
        );
        snapshots.push(JSON.parse(content));
      }
    }
    
    // Group by page
    const byPage = snapshots.reduce((acc, snap) => {
      if (!acc[snap.page]) acc[snap.page] = [];
      acc[snap.page].push(snap);
      return acc;
    }, {});
    
    // Generate HTML report
    const html = `
<!DOCTYPE html>
<html>
<head>
  <title>WitchCityRope UI Change Report</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    .page-section { margin-bottom: 30px; border: 1px solid #ddd; padding: 15px; }
    .snapshot { margin: 10px 0; padding: 10px; background: #f5f5f5; }
    .timestamp { color: #666; font-size: 0.9em; }
    .trigger { color: #880124; font-weight: bold; }
  </style>
</head>
<body>
  <h1>🎭 WitchCityRope UI Change Report</h1>
  <p>Generated: ${new Date().toISOString()}</p>
  
  ${Object.entries(byPage).map(([page, snaps]) => `
    <div class="page-section">
      <h2>Page: ${page}</h2>
      ${snaps.map(snap => `
        <div class="snapshot">
          <div class="timestamp">${snap.timestamp}</div>
          <div class="trigger">Triggered by: ${snap.triggerFile}</div>
          <div>Reason: ${snap.captureReason}</div>
          <div>Screenshot: ${snap.filename}</div>
        </div>
      `).join('')}
    </div>
  `).join('')}
</body>
</html>
    `;
    
    await fs.writeFile(
      path.join(this.screenshotDir, 'change-report.html'),
      html
    );
    
    console.log(`📊 Report generated: ${path.join(this.screenshotDir, 'change-report.html')}`);
  }
}

// Handle graceful shutdown
process.on('SIGINT', async () => {
  console.log('\n👋 Shutting down UI Monitor...');
  const monitor = new WitchCityRopeUIMonitor();
  await monitor.generateReport();
  process.exit(0);
});

// Start monitoring
const monitor = new WitchCityRopeUIMonitor();
monitor.init().catch(console.error);

module.exports = WitchCityRopeUIMonitor;