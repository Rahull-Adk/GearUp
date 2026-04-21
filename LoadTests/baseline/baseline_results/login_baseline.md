# Load Test Result — Login Endpoint

Tool: k6  
Environment: Local  
Scenario:
{ duration: '1m', target: 20 }
{ duration: '2m', target: 50 }
{ duration: '3m', target: 100 }
{ duration: '2m', target: 100 }
{ duration: '1m', target: 0 }


## Throughput

Total Requests: 14392
Requests per second: 26.64/s req/s

---

## Latency

Min latency: 93.9ms
Average latency: 2.1s
Median latency (p50): 1.98s
p90 latency: 4.04s
p95 latency: 4.39s
Max latency: 6.46s

---

## Error Rate

HTTP request failures: 0%  
Check failures: 0%

---

## Observations

- System handled 100 concurrent users without failures.
- Latency is high (p95 = 4.39s).
- Likely caused by unoptimized queries and hashings.
- Update: Every examining the traces, all the queries were taking less than 17ms to execute, so the problem is not slow queries.
- Update: After observing, I found out that hashing was taking 0.4 seconds, however, it is fine since the hashing algorithm used iterate many times.
----
