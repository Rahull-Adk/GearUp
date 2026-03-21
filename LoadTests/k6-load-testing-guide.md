# k6 Load Testing Guide for GearUp

This guide walks you through running k6 load tests against the GearUp API and capturing OpenTelemetry metrics from the Aspire Dashboard.

---

## Prerequisites

1. **Install k6**
   ```bash
   # Ubuntu/Debian
   sudo gpg -k
   sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
   echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
   sudo apt-get update
   sudo apt-get install k6
   
   # macOS
   brew install k6
   
   # Windows (using Chocolatey)
   choco install k6
   ```

2. **Start the GearUp stack with Aspire Dashboard**
   ```bash
   cd /home/rahull/RiderProjects/GearUp
   docker compose up -d
   ```

3. **Verify services are running**
   - API: http://localhost:5255/health
   - Aspire Dashboard UI: http://localhost:18888
   - OTLP endpoint: localhost:4317 (gRPC) or localhost:4318 (HTTP)

---

## Running Load Tests

### Basic Test Run

The script automatically logs in using test credentials before running the load test:

```bash
cd /home/rahull/RiderProjects/GearUp
k6 run script.js
```

**Default Configuration:**
- VUs: 50 virtual users
- Duration: 60 seconds
- Endpoint: `GET /api/v1/posts` (feed)
- User: `chris52@example.com`
- Password: `Password123!`

### Custom Configuration

Override defaults using environment variables:

```bash
# Change user credentials
k6 run -e TEST_USER=john@example.com -e TEST_PASSWORD=MyPassword123 script.js

# Change base URL (for testing against Docker container)
k6 run -e BASE_URL=http://localhost:5255 script.js

# Adjust load parameters
k6 run --vus 100 --duration 120s script.js
```

### Load Test Patterns

#### Ramp-Up Test
Gradually increase load to find breaking point:

```bash
k6 run --vus 10 --duration 30s --vus 50 --duration 60s --vus 100 --duration 90s script.js
```

#### Spike Test
Sudden traffic surge:

```bash
k6 run --vus 5 --duration 30s --vus 200 --duration 10s --vus 5 --duration 30s script.js
```

#### Stress Test
Push beyond normal capacity:

```bash
k6 run --vus 200 --duration 300s script.js
```

---

## Capturing OpenTelemetry Metrics

### 1. Access Aspire Dashboard

Open http://localhost:18888 in your browser.

### 2. View Traces

Navigate to **Traces** tab:
- Filter by `GearUp` service
- Look for `GET /api/v1/posts` spans
- Click individual traces to see:
  - Total request duration
  - EF Core database query spans
  - Child operation spans

**Key Metrics to Capture:**
- Average span duration
- p95 / p99 latency
- Slowest queries (DB spans)
- N+1 query patterns

### 3. View Metrics

Navigate to **Metrics** tab:
- Select `GearUp` service
- Key metrics:
  - `http.server.request.duration` (p50, p95, p99)
  - `http.server.active_requests`
  - Database connection pool metrics
  - Runtime metrics (CPU, memory, GC)

### 4. Capture Baseline

**Before optimizations:**
1. Run load test: `k6 run script.js`
2. Note k6 summary output (p95, p99, RPS, error %)
3. Screenshot Aspire Dashboard metrics
4. Export slowest traces (copy trace IDs)
5. Document in `LoadTests/posts-feed-baseline.md`

**Sample Documentation Format:**
```markdown
## Baseline - Before Optimization

**Date:** 2026-02-28
**Load:** 50 VUs / 60s

### k6 Results
- RPS: 234 req/s
- p95 latency: 450ms
- p99 latency: 890ms
- Error rate: 0%

### Aspire Dashboard - Traces
- Average DB query time: 180ms
- Slowest span: `SELECT * FROM Posts` (350ms)
- Trace ID: `abc123...`

### Aspire Dashboard - Metrics
- `http.server.request.duration` p95: 445ms
- Active requests peak: 48

### Identified Bottleneck
- Missing index on `Posts.CreatedAt`
- N+1 queries loading related `Cars` and `Users`
```

---

## Workflow: Baseline → Optimize → Re-test

### Step 1: Run Baseline Test

```bash
# Start services
docker compose up -d

# Wait for services to be healthy (30s)
sleep 30

# Run baseline
k6 run script.js > baseline-results.txt
```

### Step 2: Capture Baseline Metrics

1. Open Aspire Dashboard: http://localhost:18888
2. Review **Traces** for slow spans
3. Review **Metrics** for p95/p99
4. Document findings in `LoadTests/posts-feed-baseline.md`

### Step 3: Identify Bottleneck

Common issues to look for:
- Missing database indexes
- N+1 queries (multiple DB spans per request)
- Slow EF Core queries (look for `SELECT *` or complex joins)
- Unoptimized LINQ queries
- Missing caching

### Step 4: Apply ONE Fix

Examples:
- Add database index
- Use `.Include()` to eager load relations
- Add response caching
- Optimize LINQ query shape
- Add Redis caching for feed

### Step 5: Re-test

```bash
# Rebuild and restart services
docker compose down
docker compose up -d --build

# Wait for healthy state
sleep 30

# Run post-optimization test
k6 run script.js > optimized-results.txt
```

### Step 6: Compare Results

Update your documentation file with the comparison:

```markdown
## After Optimization

**Fix Applied:** Added composite index on `Posts(CreatedAt DESC, UserId)`

### k6 Results (Δ vs baseline)
- RPS: 487 req/s (+108%)
- p95 latency: 215ms (-52%)
- p99 latency: 380ms (-57%)
- Error rate: 0%

### Aspire Dashboard - Traces
- Average DB query time: 45ms (-75%)
- Slowest span: `SELECT * FROM Posts` (80ms)

### Conclusion
The composite index significantly improved query performance. The feed endpoint can now handle 2x the load with 50% lower latency.
```

---

## Troubleshooting

### Login Fails (401)

Check that test user exists:
```bash
docker exec -it gearup-db mysql -u root -p${MYSQL_ROOT_PASSWORD} -e "USE gearup; SELECT Email, IsEmailVerified FROM Users WHERE Email = 'chris52@example.com';"
```

Ensure user is email-verified (seeded users should be verified by default).

### No Telemetry in Aspire Dashboard

1. Check OTLP exporter configuration in `ServiceExtensions.cs`:
   ```csharp
   .AddOtlpExporter()
   ```

2. Verify environment variable (should be auto-detected):
   ```bash
   docker exec gearup-api env | grep OTEL
   ```

3. Check Docker networking:
   ```bash
   docker exec gearup-api ping aspire-dashboard -c 2
   ```

4. Review API logs for OTLP connection errors:
   ```bash
   docker logs gearup-api | grep -i otel
   ```

### Rate Limiting (429 Too Many Requests)

Rate limiting is disabled in Development mode. Ensure `ASPNETCORE_ENVIRONMENT=Development` in your `.env` or `docker-compose.yml`.

If testing production mode, adjust rate limiter in `ServiceExtensions.cs`:
```csharp
PermitLimit = 1000,  // Increase from 100
Window = TimeSpan.FromMinutes(1)
```

---

## Best Practices

1. **Warm up before measuring** - Run a quick 10s test first to warm up EF Core and connections
2. **One change at a time** - Only apply one optimization per test cycle
3. **Use realistic load** - Start with 50 VUs, then scale up to find breaking point
4. **Document everything** - Capture before/after metrics with timestamps
5. **Check database state** - Ensure consistent data volume across tests
6. **Monitor database** - Watch MySQL slow query log alongside Aspire traces

---

## Next Steps

1. Test additional endpoints:
   - `POST /api/v1/posts` (create)
   - `GET /api/v1/posts/{id}` (single post)
   - `POST /api/v1/posts/{id}/like` (toggle like)
   - `GET /api/v1/cars` (car search)

2. Create dedicated test scripts for each endpoint

3. Set up automated performance regression testing in CI/CD

4. Implement distributed tracing for SignalR real-time operations

---

## References

- [k6 Documentation](https://k6.io/docs/)
- [Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [GearUp Load Testing Methodology](./readme.md)

