# FlinkDotNet TODO Roadmap

This is the authoritative checklist for stabilizing FlinkDotNet’s end‑to‑end experience (Kafka ⇄ Flink) and docs. Keep this file updated as work completes.

Legend: 
- [x] Completed
- [ ] Pending
- [~] In progress / under review

---

## 1) Core Architecture Decision
- [x] Adopt IR Runner Jar architecture: .NET DSL generates IR; a prebuilt Java/Scala Runner jar builds and runs the Flink job from IR.
- [ ] Define IR versioning policy and backward compatibility strategy.

## 2) IR Schema (JobDefinition + Operations)
- [x] Confirm base IR model in `Flink.JobBuilder` (sources/ops/sinks/metadata).
- [ ] Freeze v1.0 IR schema with explicit JSON schema file (`docs/ir-schema-v1.json`).
- [ ] Add IR validators (topic required, window sizing, timer bounds, async timeout ranges, etc.).
- [ ] Add test fixtures for IR round‑trip (serialize/deserialize) and validation errors.

## 3) IR Runner Jar (Java/Scala)
- [ ] New module `Flink.IRRunner` that:
  - [ ] Accepts IR via file path or base64 argument.
  - [ ] Builds DataStream topology for:
    - [ ] Kafka source/sink (earliest/latest offsets).
    - [ ] Map / Filter operations.
    - [ ] Timer (processing time) operation.
    - [ ] Tumbling/Sliding windows on keyed streams.
  - [ ] Produces consolidated metrics (numRecordsIn/Out, parallelism, checkpoints).
  - [ ] Includes shaded Kafka connectors (fat jar) for Flink 2.x.
- [ ] Provide `flink-ir-runner.jar` in CI artifacts and releases.

## 4) Flink Job Gateway (ASP.NET Core)
- [x] Basic service present with health endpoints.
- [ ] Implement submit pipeline:
  - [ ] Upload/ensure Runner jar (`/jars/upload` → cache jarId).
  - [ ] Run jar (`/jars/{jarId}/run`) with entry class and IR argument.
  - [ ] Return `FlinkJobId` with job mapping and submission timestamp.
- [ ] Implement cancel (`/v1/jobs/{id}/cancel`).
- [ ] Implement status (`/v1/jobs/{id}/status`) via Flink REST overview endpoints.
- [ ] Implement metrics (`/v1/jobs/{id}/metrics`) with a concise summary payload.
- [ ] Config via env: `FLINK_CLUSTER_HOST`, `FLINK_CLUSTER_PORT`, timeouts, retries.

## 5) FlinkDotNet SDK (C#) – DSL + Client
- [x] Preserve current DSL/IR generation in `Flink.JobBuilder`.
- [ ] Add guardrails/validation (pre‑submit checks) with useful messages.
- [ ] Expand ops: Async HTTP/db, state ops, side outputs, retry (map to Runner capabilities).
- [ ] Add `FlinkDotNet` facade helpers for typical Kafka→Kafka, Kafka→Console pipelines.

## 6) LocalTesting (Aspire AppHost + Tests)
- [x] AppHost includes Kafka + Flink (JM/TM) + Flink Job Gateway.
- [x] New integration tests: `LocalTesting/LocalTesting.IntegrationTests`
  - [x] Proves gateway health, Kafka readiness, IR generation.
  - [x] Category("observability") for CI filtering.
- [x] LocalTesting.sln solution structure created for build validation.
- [ ] Make LocalTesting integration test work end‑to‑end with FlinkDotNet + Flink + Kafka:
  - [ ] Wire Gateway submit to use IR Runner jar, get real FlinkJobId.
  - [ ] Produce to input topic, consume from output topic, assert counts > 0.
  - [ ] Fetch Flink metrics (records in/out, parallelism, checkpoints) and assert presence.
  - [ ] Stabilize test timings with readiness probes and backoff.

## 7) GitHub Workflows
- [x] Observability workflow updated to run LocalTesting integration tests:
  - File: `.github/workflows/observability-tests.yml`
  - Builds and runs `LocalTesting/LocalTesting.IntegrationTests` with category filter.
- [ ] Add CI job to build `flink-ir-runner.jar` on Linux with Java 17 and publish artifact.
- [ ] Add matrix to run LocalTesting integration on Linux and Windows runners.

## 8) Documentation Overhaul
- [ ] `docs/README.md` – Architecture and 5‑minute Quick Start.
- [ ] `docs/quickstart.md` – Running LocalTesting integration test locally.
- [ ] `docs/dsl-guide.md` – Full DSL (source/ops/sinks) with examples and limitations.
- [ ] `docs/gateway-api.md` – Submit/cancel/status/metrics REST API with examples.
- [ ] `docs/runner.md` – Runner internals, UDF registry, connectors, metrics.
- [ ] `docs/observability.md` – Metric mapping and Prometheus setup.
- [ ] `docs/deployment.md` – K8s manifests/Helm; production topology.
- [ ] `docs/temporal.md` – Optional orchestration using Temporal workflows.
- [ ] `docs/troubleshooting.md` – Common failures and fixes.

## 9) Optional: Temporal Orchestration (Production)
- [ ] Temporal Workflow: Submit → Monitor → Cancel with backoff and idempotency (keyed by IR hash + options).
- [ ] Activities: ValidateIR, EnsureRunnerJar (upload/cache), RunJob, MonitorJob, CancelJob.
- [ ] Artifact caching: cache Runner jar id; dedupe submissions; retries with jitter.
- [ ] Integration tests (mock Flink REST) to validate workflow paths and compensations.
- [ ] Note: This follows the proven pattern where a workflow engine coordinates large numbers of Flink jobs reliably.

## 10) Release Plan
- [ ] Versioning: SemVer for `FlinkDotNet` (SDK) and IR schema versioning.
- [ ] Release artifacts:
  - [ ] NuGet: `FlinkDotNet`, `Flink.JobBuilder`.
  - [ ] GitHub Release: `flink-ir-runner.jar` with checksums.
- [ ] Changelog and upgrade guides.

---

## Completed (recent)

- [x] Restore BackPressureKafkaTesting to original state (isolation maintained).
- [x] LocalTesting AppHost: Kafka + Flink + Gateway (aspire) configured.
- [x] LocalTesting integration test added (`LocalTesting.IntegrationTests`) proving:
  - [x] Kafka readiness + topic creation
  - [x] Gateway health interaction
  - [x] FlinkDotNet IR generation and submission attempt
- [x] Observability workflow updated to target LocalTesting integration tests directly.

---

## Notes / Next Steps
- The LocalTesting integration test currently validates Gateway health and IR generation and attempts submission. Once the IR Runner jar exists and Gateway submission is wired to Flink REST jar-run, flip the test to require successful submission + end‑to‑end consumption and metrics assertions.
- IR schema must be finalized (“v1.0”) before Runner and Gateway harden their JSON contracts.
- Keep this TODO updated as items land; link PR numbers and dates beside each checkbox when applicable.
