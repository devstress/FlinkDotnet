# Observability

This project surfaces a concise job metrics view via the Gateway and can be integrated with Prometheus.

- Metrics overview (Gateway):
  - RecordsIn / RecordsOut (aggregated from vertices)
  - Parallelism (max across vertices)
  - Checkpoints (completed count) and LastCheckpoint time
  - Backpressure level (worst across vertices, from Flink REST backpressure sampling)

Prometheus setup on Flink (example `flink-conf.yaml`):
```
metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter
metrics.reporter.prom.port: 9250-9260
metrics.reporter.prom.filter.label: true
```

Expose metrics ports for JobManager and TaskManagers and configure Prometheus to scrape them.

Gateway mapping:
- `recordsIn` ← sum of `numRecordsIn`
- `recordsOut` ← sum of `numRecordsOut`
- `parallelism` ← max of vertex `parallelism`
- `checkpoints` and `lastCheckpoint` ← `/v1/jobs/{jobId}/checkpoints`
- `customMetrics.backpressureLevel` ← max severity from `/v1/jobs/{jobId}/vertices/{vertexId}/backpressure`

In LocalTesting, tests validate health, submission, produce/consume, and metrics presence.
For backpressure visualization in Grafana, scrape Flink Prometheus metrics and add panels for task/operator busy time; the Gateway’s backpressureLevel helps quick triage.
