# Docker Configuration Fix Report

## Issues Identified

### 1. **Missing curl in Runtime Image** ⚠️ CRITICAL
**Error:** Docker health checks were failing silently because the `mcr.microsoft.com/dotnet/aspnet:8.0` base image doesn't include `curl` by default.

**Symptoms:**
- Health check command `curl -f http://localhost:5000/health` would fail with "command not found"
- Container might be marked as unhealthy even when the app is running fine
- Kubernetes/Docker orchestration might keep restarting the container

**Fix:**
```dockerfile
# Install curl in runtime stage
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*
```

### 2. **Missing Data Directory** ⚠️ HIGH
**Error:** SQLite database directory `/data` wasn't created in the Docker image, causing database initialization failures.

**Symptoms:**
- Database connection errors on first run
- "No such file or directory" errors
- Bot fails to persist data

**Fix:**
```dockerfile
# Create required directories with proper permissions
RUN mkdir -p /app/logs /app/data && \
    chown -R appuser:appuser /app
```

### 3. **Incomplete Environment Variables** ⚠️ MEDIUM
**Error:** Docker Compose wasn't passing all required configuration through environment variables.

**Symptoms:**
- Cache configuration not connecting to Redis
- WebUI not starting properly
- Missing JWT secret for authentication
- Bot settings using incorrect defaults

**Fix:** Updated `docker-compose.yml` to include:
- `Cache__Enabled=true`
- `Cache__ConnectionString=redis:6379`
- `WebUI__Port=5000`
- `WebUI__EnableSwagger=true`
- All other required configuration variables

### 4. **Service Dependency Timing** ⚠️ MEDIUM
**Error:** App was starting before Redis was fully ready, causing cache connection failures.

**Symptoms:**
- Intermittent failures on container startup
- Redis connection errors in logs
- Cache falling back to in-memory mode

**Fix:**
```yaml
depends_on:
  redis:
    condition: service_healthy
```

### 5. **Health Check Start Period Too Short** ⚠️ LOW
**Error:** 5-second start period was too short for the bot to initialize Discord connection and web server.

**Symptoms:**
- Premature health check failures during startup
- Container marked unhealthy before fully started

**Fix:** Extended to 15 seconds:
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1
```

### 6. **Configuration Placeholder Issues** ⚠️ LOW
**Error:** `appsettings.json` had hardcoded values instead of using environment variable placeholders.

**Symptoms:**
- Configuration not properly overridden by Docker environment variables
- Connection strings pointing to wrong locations

**Fix:** Updated to use environment variable placeholders:
```json
"ConnectionString": "${ConnectionStrings__DefaultConnection}",
"ConnectionString": "${Cache__ConnectionString}",
```

## Files Modified

1. **Dockerfile**
   - Added curl installation
   - Created `/app/data` directory
   - Extended health check start period

2. **docker-compose.yml**
   - Added missing environment variables
   - Fixed Redis dependency to wait for health check
   - Extended health check start period

3. **.env.example**
   - Added all required configuration variables
   - Organized by category (Discord, Database, Cache, WebUI, Bot)
   - Added helpful comments

4. **appsettings.json**
   - Updated to use environment variable placeholders
   - Ensures Docker environment variables override defaults

## Testing Recommendations

Before deploying, test with:

```bash
# 1. Create .env file from example
cp .env.example .env

# 2. Edit .env and add your Discord bot token
nano .env

# 3. Build and run
docker compose build
docker compose up

# 4. Check health endpoint
curl http://localhost:5000/health

# 5. View logs
docker compose logs -f app
```

## Expected Behavior After Fixes

✅ Health checks will work properly (curl installed)
✅ Database will initialize in `/data/shadowrun.db`
✅ Redis connection will succeed (proper dependency ordering)
✅ All configuration will be properly loaded from environment variables
✅ Web UI will be accessible at http://localhost:5000
✅ API docs will be available at http://localhost:5000/api-docs
✅ Health check UI at http://localhost:5000/health-ui

## Additional Notes

- The bot uses a generic `Host` with a separate `WebUIService` that creates a `WebApplication`
- Health endpoints are properly configured at `/health`, `/health/ready`, `/health/live`, and `/healthz`
- Redis is optional - the app will fall back to in-memory cache if Redis is unavailable
- The non-root user `appuser` is used for security
- All logs are written to `/app/logs` and mounted to `./logs` on the host

## Common Issues & Solutions

**Issue:** Bot fails to start with Discord connection error
**Solution:** Verify `DISCORD_TOKEN` is set correctly in `.env` file

**Issue:** Redis connection errors in logs
**Solution:** Wait for Redis to fully start (health check should prevent this now)

**Issue:** Database permission errors
**Solution:** The Dockerfile now creates directories with proper ownership

**Issue:** Health checks still failing
**Solution:** Check logs with `docker compose logs app` - may be an application error
