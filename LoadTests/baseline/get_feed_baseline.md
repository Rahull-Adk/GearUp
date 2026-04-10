# Load Test Result — Get Feed Endpoint

Tool: k6  
Environment: Local  
Scenario:
    { duration: '1m', target: 20 }
    { duration: '2m', target: 50 }
    { duration: '3m', target: 100 }
    { duration: '2m', target: 100 }
    { duration: '1m', target: 0 }


## Throughput

Total Requests: 14278
Requests per second: 26.42 req/s  

---

## Latency

Min latency: 73.24ms
Average latency: 2.23s
Median latency (p50): 1.68s
p90 latency: 5.09s  
p95 latency: 6.34s
Max latency: 8.94s  

---

## Error Rate

HTTP request failures: 0%  
Check failures: 0%

---

## Observations

- System handled 100 concurrent users without failures.
- Latency is high (p95 = 6.34s).
- Likely caused by unoptimized queries or missing caching.

---

## Next Optimization Steps

- Add database indexes
- Optimize slow queries
- Introduce caching layer
