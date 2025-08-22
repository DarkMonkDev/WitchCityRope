#!/usr/bin/env node

/**
 * Persistent Browser Server for Browser Tools MCP
 * This server runs continuously and handles browser operations for MCP
 */

const { spawn } = require('child_process');
const fs = require('fs').promises;
const path = require('path');
const net = require('net');

// Configuration
const CONFIG = {
  port: process.env.BROWSER_SERVER_PORT || 9222,
  host: process.env.BROWSER_SERVER_HOST || '127.0.0.1',
  pidFile: path.join(__dirname, 'browser-server.pid'),
  logFile: path.join(__dirname, 'browser-server.log'),
  errorFile: path.join(__dirname, 'browser-server.error.log'),
  browserToolsPath: path.join(__dirname, '..', 'node_modules', '.bin', 'browser-tools-mcp'),
  maxRetries: 5,
  retryDelay: 5000
};

// Logging functions
async function log(message) {
  const timestamp = new Date().toISOString();
  const logMessage = `[${timestamp}] ${message}\n`;
  console.log(logMessage.trim());
  await fs.appendFile(CONFIG.logFile, logMessage).catch(console.error);
}

async function logError(error) {
  const timestamp = new Date().toISOString();
  const errorMessage = `[${timestamp}] ERROR: ${error}\n${error.stack || ''}\n`;
  console.error(errorMessage.trim());
  await fs.appendFile(CONFIG.errorFile, errorMessage).catch(console.error);
}

// Check if port is available
function isPortAvailable(port, host) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.once('error', () => resolve(false));
    server.once('listening', () => {
      server.close();
      resolve(true);
    });
    server.listen(port, host);
  });
}

// Write PID file
async function writePidFile() {
  await fs.writeFile(CONFIG.pidFile, process.pid.toString());
}

// Clean up PID file on exit
async function cleanup() {
  try {
    await fs.unlink(CONFIG.pidFile);
  } catch (error) {
    // Ignore if file doesn't exist
  }
}

// Start browser tools server
async function startBrowserTools() {
  const env = {
    ...process.env,
    NODE_ENV: process.env.NODE_ENV || 'production',
    CHROME_PATH: process.env.CHROME_PATH || '/mnt/c/Program Files/Google/Chrome/Application/chrome.exe',
    BROWSER_SERVER_PORT: CONFIG.port,
    BROWSER_SERVER_HOST: CONFIG.host,
    // Ensure Chrome launches with incognito mode
    CHROME_LAUNCH_CMD: `powershell.exe -Command "& 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe' --remote-debugging-port=${CONFIG.port} --incognito --no-first-run --no-default-browser-check"`
  };

  return new Promise((resolve, reject) => {
    const browserProcess = spawn('node', [CONFIG.browserToolsPath], {
      env,
      stdio: ['pipe', 'pipe', 'pipe'],
      detached: false
    });

    browserProcess.stdout.on('data', async (data) => {
      await log(`Browser Tools: ${data.toString().trim()}`);
    });

    browserProcess.stderr.on('data', async (data) => {
      await logError(`Browser Tools Error: ${data.toString().trim()}`);
    });

    browserProcess.on('error', async (error) => {
      await logError(`Failed to start browser tools: ${error}`);
      reject(error);
    });

    browserProcess.on('exit', async (code, signal) => {
      await log(`Browser tools exited with code ${code} and signal ${signal}`);
      reject(new Error(`Browser tools exited unexpectedly`));
    });

    // Give it time to start
    setTimeout(() => resolve(browserProcess), 2000);
  });
}

// Main server loop
async function runServer() {
  let retries = 0;
  let browserProcess = null;

  while (retries < CONFIG.maxRetries) {
    try {
      // Check if port is available
      const portAvailable = await isPortAvailable(CONFIG.port, CONFIG.host);
      if (!portAvailable) {
        await logError(`Port ${CONFIG.port} is already in use`);
        process.exit(1);
      }

      await log(`Starting browser server on ${CONFIG.host}:${CONFIG.port}`);
      await writePidFile();

      // Start browser tools
      browserProcess = await startBrowserTools();
      await log('Browser tools started successfully');

      // Reset retry counter on successful start
      retries = 0;

      // Wait for process to exit
      await new Promise((resolve) => {
        browserProcess.on('exit', resolve);
      });

    } catch (error) {
      await logError(error);
      retries++;
      
      if (retries < CONFIG.maxRetries) {
        await log(`Retrying in ${CONFIG.retryDelay / 1000} seconds... (${retries}/${CONFIG.maxRetries})`);
        await new Promise(resolve => setTimeout(resolve, CONFIG.retryDelay));
      } else {
        await log('Max retries reached. Exiting.');
        process.exit(1);
      }
    }
  }
}

// Handle shutdown signals
process.on('SIGTERM', async () => {
  await log('Received SIGTERM, shutting down gracefully...');
  await cleanup();
  process.exit(0);
});

process.on('SIGINT', async () => {
  await log('Received SIGINT, shutting down gracefully...');
  await cleanup();
  process.exit(0);
});

process.on('uncaughtException', async (error) => {
  await logError(`Uncaught exception: ${error}`);
  await cleanup();
  process.exit(1);
});

process.on('unhandledRejection', async (reason, promise) => {
  await logError(`Unhandled rejection at: ${promise}, reason: ${reason}`);
});

// Start the server
(async () => {
  await log('Browser server starting...');
  await runServer();
})();