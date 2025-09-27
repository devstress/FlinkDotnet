# Deployment

Recommended production topology:
- Flink cluster (Kubernetes via Helm)
- Flink Job Gateway as a Deployment/Service
- Kafka as source/sink (managed or self-hosted)

Key notes:
- Build and publish the Gateway; it prebuilds and bundles `flink-ir-runner.jar` automatically.
- The Gateway uploads the runner jar to Flink on submission; no separate jar mounting step is required.
- Configure Gateway with `FLINK_CLUSTER_HOST` and `FLINK_CLUSTER_PORT`.
- Use `parallelism` in `JobMetadata` to scale jobs appropriately.

Security & reliability:
- Lock down Gateway with auth (API keys, mTLS) in front of cluster network.
- Enable Flink checkpoints and state backends suitable for durability.
- Add retries and idempotency around submissions (consider Temporal orchestration).
