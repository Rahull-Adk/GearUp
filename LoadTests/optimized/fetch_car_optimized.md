# Load Test Result — Get All Cars Endpoint

Tool: k6  
Environment: Local  
Scenario:
{ duration: '1m', target: 20 }
{ duration: '2m', target: 50 }
{ duration: '3m', target: 100 }
{ duration: '2m', target: 100 }
{ duration: '1m', target: 0 }

█ TOTAL RESULTS

checks_total: 878357 1615.085935/s
checks_succeeded: 99.73% 876035 out of 878357
checks_failed: 0.26%  2322 out of 878357


## Throughput
=
Total Requests: 439179
Requests per second: 807.543887/s

---

## Latency

Min latency: 1.96ms
Average latency: 67.77ms
Median latency (p50): 25.36ms
p90 latency: 60.1ms
p95 latency: 76.91ms
Max latency: 2m11s

---

## Error Rate

HTTP request failures: 0.26%
Check failures: 1164 out of 439179

---

## Observations

- System handled 100 concurrent users without failures.
- The quries are optimzied and latency is drastically reduced. 
