# =============================================================================
# GroceryHub — Multi-stage Production Dockerfile for Web Frontend
# Pinned Node 22 Alpine build -> nginxinc/nginx-unprivileged:1.27-alpine
# Non-root unprivileged user `nginx` (UID 101)
# =============================================================================

# -----------------------------------------------------------------------------
# Stage 1: Build Frontend Assets
# -----------------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies
COPY package.json package-lock.json ./
RUN npm ci || npm install

# Copy full frontend source tree
COPY . ./

# Build TypeScript + Vite SPA into /app/dist
RUN npm run build

# -----------------------------------------------------------------------------
# Stage 2: Serve via Hardened Unprivileged Nginx
# -----------------------------------------------------------------------------
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime

# Remove default nginx welcome configuration
RUN rm -rf /etc/nginx/conf.d/* /usr/share/nginx/html/*

# Copy custom reverse proxy & security configuration
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

# Copy production bundle from build stage
COPY --from=build --chown=101:101 /app/dist /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=3s --retries=3 \
    CMD wget -q --spider http://localhost:8080/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
