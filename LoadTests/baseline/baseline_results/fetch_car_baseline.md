**# Load Test Result — Get All Cars Endpoint

Tool: k6  
Environment: Local  
Scenario:
{ duration: '1m', target: 20 }
{ duration: '2m', target: 50 }
{ duration: '3m', target: 100 }
{ duration: '2m', target: 100 }
{ duration: '1m', target: 0 }
Data: 600,000 cars



## Throughput

Total Requests: 1388
Requests per second:  2.47/s
 
---

## Latency

Min latency: 854.85ms
Average latency: 22.86s
Median latency (p50): 30.11s
p90 latency: 30.37s
p95 latency: 30.47s
Max latency: 31.47s

---

## Error Rate

HTTP request failures: 54.68% 759 out of 1388
Check failures: 54.70% 1518 out of 2775

---

## Observations

-- Enormous avg, p90 and p95, due to large dataset (600K) and lack of indexes and slow queries.
-- Terrible throughput and latency
-- Almost 50% error rate due to Db connection pool size.
----


## Next Optimization Steps

- Add database indexes
- Optimize slow queries
- Introduce caching layer

