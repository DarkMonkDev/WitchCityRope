import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig(({ mode }) => {
  const isProduction = mode === 'production'
  const isDevelopment = mode === 'development'
  
  return {
    plugins: [
      react({
        // Enable Fast Refresh in development
        fastRefresh: isDevelopment,
      })
    ],
    
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
        '@components': path.resolve(__dirname, './src/components'),
        '@pages': path.resolve(__dirname, './src/pages'),
        '@hooks': path.resolve(__dirname, './src/hooks'),
        '@utils': path.resolve(__dirname, './src/utils'),
        '@services': path.resolve(__dirname, './src/services'),
        '@types': path.resolve(__dirname, './src/types'),
      },
    },
    
    // Development server configuration
    server: {
      host: '0.0.0.0', // Required for container access
      port: 5174, // Updated to match command line port
      strictPort: false, // Allow port flexibility for development
      
      // HMR Configuration for containers - Fixed port alignment
      hmr: {
        port: 24679, // Changed HMR port to avoid conflicts
        host: '0.0.0.0', // Allow external HMR connections for Docker
        clientPort: 24679, // Updated client port to match HMR port
      },
      
      // File watching configuration for containers
      watch: {
        usePolling: true, // Required for Docker volume mounts
        interval: 1000, // Increased polling interval for stability in containers
        ignored: ['**/node_modules/**', '**/dist/**', '**/.git/**'],
        followSymlinks: false, // Better performance in containers
      },
      
      // API proxy configuration - Container-aware
      proxy: {
        '/api': {
          // Use container DNS when running in Docker, localhost for local dev
          target: process.env.DOCKER_ENV === 'true' 
            ? 'http://api:8080'  // Container-to-container communication
            : (process.env.VITE_API_BASE_URL || 'http://localhost:5653'), // Host communication
          changeOrigin: true,
          secure: false,
          timeout: 30000, // 30 second timeout for API calls
          configure: (proxy, options) => {
            // Enhanced proxy logging for containers
            proxy.on('error', (err, req, res) => {
              console.error('Proxy error:', {
                error: err.message,
                url: req.url,
                target: options.target,
                timestamp: new Date().toISOString()
              });
            });
            
            proxy.on('proxyReq', (proxyReq, req, res) => {
              console.log('Proxy request:', {
                method: req.method,
                url: req.url,
                target: options.target,
                timestamp: new Date().toISOString()
              });
            });
            
            proxy.on('proxyRes', (proxyRes, req, res) => {
              console.log('Proxy response:', {
                status: proxyRes.statusCode,
                url: req.url,
                target: options.target,
                timestamp: new Date().toISOString()
              });
            });
          },
        },
      },
    },
    
    // Build configuration
    build: {
      outDir: 'dist',
      sourcemap: !isProduction,
      
      // Optimization settings
      rollupOptions: {
        output: {
          // Manual chunk splitting for better caching
          manualChunks: {
            vendor: ['react', 'react-dom'],
            router: ['react-router-dom'],
            ui: ['@chakra-ui/react'],
            query: ['@tanstack/react-query'],
            forms: ['react-hook-form', 'zod'],
          },
        },
      },
      
      // Bundle size analysis
      reportCompressedSize: isProduction,
      chunkSizeWarningLimit: 1000,
    },
    
    // Development optimizations
    optimizeDeps: {
      include: [
        'react',
        'react-dom',
        'react-router-dom',
        '@chakra-ui/react',
        '@tanstack/react-query',
      ],
    },
    
    // Environment variable handling
    define: {
      __APP_VERSION__: JSON.stringify(process.env.VITE_APP_VERSION || '1.0.0'),
    },
  }
})