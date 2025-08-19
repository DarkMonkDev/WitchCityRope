#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

async function checkApiHealth() {
    console.log('📡 Checking API availability...');
    
    try {
        const response = await fetch('http://localhost:5653/health');
        if (!response.ok) {
            throw new Error(`API health check failed: ${response.status}`);
        }
        console.log('✅ API is running and healthy');
        return true;
    } catch (error) {
        console.error('❌ API is not running or not healthy:', error.message);
        console.error('Please start the API first using: ./dev.sh or cd apps/api && dotnet run');
        return false;
    }
}

async function generateTypes() {
    console.log('🔄 Generating TypeScript types from API...');
    
    // Ensure API is running
    const apiHealthy = await checkApiHealth();
    if (!apiHealthy) {
        process.exit(1);
    }

    // Ensure generated directory exists
    const generatedDir = path.join(__dirname, '../src/generated');
    if (!fs.existsSync(generatedDir)) {
        fs.mkdirSync(generatedDir, { recursive: true });
    }

    // Generate types using NSwag
    console.log('🏗️ Generating types with NSwag...');
    const nswagConfigPath = path.join(__dirname, 'nswag.json');
    
    return new Promise((resolve, reject) => {
        const nswag = spawn('npx', ['nswag', 'run', nswagConfigPath], {
            cwd: path.join(__dirname, '..'),
            stdio: 'inherit'
        });

        nswag.on('close', (code) => {
            if (code !== 0) {
                reject(new Error(`NSwag generation failed with code ${code}`));
            } else {
                console.log('✅ NSwag generation completed successfully');
                resolve();
            }
        });

        nswag.on('error', (error) => {
            reject(error);
        });
    });
}

async function postProcessTypes() {
    console.log('🔧 Post-processing generated types...');
    
    try {
        const postProcessScript = path.join(__dirname, 'post-process.js');
        
        return new Promise((resolve, reject) => {
            const postProcess = spawn('node', [postProcessScript], {
                cwd: path.join(__dirname, '..'),
                stdio: 'inherit'
            });

            postProcess.on('close', (code) => {
                if (code !== 0) {
                    reject(new Error(`Post-processing failed with code ${code}`));
                } else {
                    resolve();
                }
            });

            postProcess.on('error', (error) => {
                reject(error);
            });
        });
    } catch (error) {
        console.warn('⚠️ Post-processing skipped:', error.message);
    }
}

async function validateTypes() {
    console.log('✅ Validating generated types...');
    
    return new Promise((resolve, reject) => {
        const tsc = spawn('npx', ['tsc', '--noEmit'], {
            cwd: path.join(__dirname, '..'),
            stdio: 'inherit'
        });

        tsc.on('close', (code) => {
            if (code !== 0) {
                console.warn('⚠️ TypeScript validation found issues, but continuing...');
                // Don't fail on TypeScript errors during generation
                resolve();
            } else {
                console.log('✅ TypeScript validation passed');
                resolve();
            }
        });

        tsc.on('error', (error) => {
            console.warn('⚠️ TypeScript validation skipped:', error.message);
            resolve();
        });
    });
}

async function main() {
    try {
        await generateTypes();
        await postProcessTypes();
        await validateTypes();
        
        console.log('🎉 Type generation completed successfully!');
        console.log('📁 Generated types are available in src/generated/api-client.ts');
    } catch (error) {
        console.error('❌ Type generation failed:', error.message);
        process.exit(1);
    }
}

// Allow running directly
if (require.main === module) {
    main();
}

module.exports = { generateTypes, checkApiHealth };