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

Total Requests: 114926
Requests per second: 212.737174/s

---

## Latency

Min latency: 7.46ms
Average latency: 81.67ms
Median latency (p50): 74.08ms
p90 latency: 155.51ms
p95 latency: 175.45ms
Max latency: 325.27ms

---

## Error Rate

HTTP request failures: 0%  
Check failures: 0%

---

## Observations

- System handled 100 concurrent users without failures.
- The quries are optimzied and latency is drastically reduced. 
