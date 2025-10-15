# Backpressure Test: What To Check Locally

When running locally with Aspire (Docker Desktop or Podman required), verify the following from the dashboards and logs:

## 1. Aspire Dashboard
- Resources for Kafka (and UI) are running
- Ports match your configuration

## 2. Kafka UI
- Topic(s) are created and partitions assigned
- Consumer group shows activity and lag metrics appear

## 3. Flink Web UI (if used)
- Job(s) are visible and healthy
- TaskManagers show no persistent backpressure warnings under normal load

## 4. Test Console Output
- Topics created successfully
- Producer/consumer steps progressing without unhandled errors
- Final summary printed with messages processed and throughput

These checks help validate the local environment without relying on screenshots or environment‑specific metrics.
