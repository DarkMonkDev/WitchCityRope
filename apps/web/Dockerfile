# apps/web/Dockerfile

###########################################
# Base Node.js Image
###########################################
FROM node:20-alpine AS base
WORKDIR /app

# Install dependencies needed for node-gyp and native modules
RUN apk add --no-cache \
    python3 \
    make \
    g++ \
    libc6-compat

# Copy workspace package files
COPY package*.json ./
COPY tsconfig*.json ./

# Copy workspace packages (for monorepo dependencies)
COPY packages/ ./packages/

###########################################
# Dependencies Stage
###########################################
FROM base AS deps

# Install all workspace dependencies (including monorepo packages)
RUN npm install

###########################################
# Development Stage (used by ./dev.sh and docker-compose.dev.yml)
###########################################
FROM base AS development

# Install curl for healthcheck
RUN apk add --no-cache curl

# Copy dependencies from deps stage
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/packages ./packages

# Copy app source code
COPY apps/web ./apps/web

# Set working directory to the web app
WORKDIR /app/apps/web

# Expose development port and HMR port
EXPOSE 5173 24678

# Development environment variables
ENV NODE_ENV=development
ENV VITE_HOST=0.0.0.0
ENV VITE_PORT=5173

# Start Vite development server with HMR configuration
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173"]

###########################################
# Build Stage
###########################################
FROM base AS builder

# Copy dependencies from deps stage (includes packages/ and all node_modules)
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/packages ./packages

# Copy app source code
COPY apps/web ./apps/web

# Use container-specific tsconfig for builds if it exists (from app directory)
RUN if [ -f apps/web/tsconfig.container.json ]; then cp apps/web/tsconfig.container.json apps/web/tsconfig.json; fi

# Build arguments for environment configuration
ARG VITE_API_BASE_URL
ARG VITE_APP_TITLE="WitchCityRope"
ARG VITE_APP_VERSION="1.0.0"

# Set build environment
ENV NODE_ENV=production
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_TITLE=$VITE_APP_TITLE
ENV VITE_APP_VERSION=$VITE_APP_VERSION

# Build application using npm workspace command from root
RUN npm run build --workspace=@witchcityrope/web

# Verify build output
RUN ls -la apps/web/dist/

###########################################
# Production Stage
###########################################
FROM nginx:alpine AS production

# Install security tools
RUN apk add --no-cache curl

# Copy custom nginx configuration from web app directory
COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built application from builder stage
COPY --from=builder /app/apps/web/dist /usr/share/nginx/html

# Create nginx user for security
RUN addgroup -S nginx_app && adduser -S nginx_app -G nginx_app

# Set ownership and permissions
RUN chown -R nginx_app:nginx_app /usr/share/nginx/html
RUN chown -R nginx_app:nginx_app /var/cache/nginx
RUN chown -R nginx_app:nginx_app /var/log/nginx

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

###########################################
# Test Stage (for CI/CD)
###########################################
FROM base AS test

# Copy dependencies from deps stage (includes packages/)
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/packages ./packages

# Copy app source code
COPY apps/web ./apps/web

# Use container-specific tsconfig for builds if it exists
RUN if [ -f apps/web/tsconfig.container.json ]; then cp apps/web/tsconfig.container.json apps/web/tsconfig.json; fi

# Run tests using workspace command from root
RUN npm run test --workspace=@witchcityrope/web -- --run --coverage

# Run linting using workspace command from root
RUN npm run lint --workspace=@witchcityrope/web

# Run type checking from web app directory
RUN cd apps/web && npx tsc --noEmit