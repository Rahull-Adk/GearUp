# Load Test Result — Search Car Endpoint

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

Total Requests: 1268
Requests per second: 2.2/s

---


## Latency

Min latency: 20.71ms
Average latency: 11.28s
Median latency (p50): 6.63s
p90 latency: 30.14sf
p95 latency: 30.23s
Max latency: 30.82s

---

## Error Rate

HTTP request failures: 23.94% 718 out of 2998 
Check failures: 49.13% 2945 out of 5994

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
