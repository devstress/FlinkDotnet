# Gateway API

Base URL defaults to `http://localhost:8080`.

- POST `/api/v1/jobs/submit`
  - Body: `JobDefinition` (IR JSON)
  - Response: `JobSubmissionResult`
  - Notes:
    - For SQL jobs, set `source` to `{ "type": "sql", "statements": ["DDL/DML..."] }`. The sink is defined in SQL (e.g., `INSERT INTO`).

- GET `/api/v1/jobs/{flinkJobId}/status`
  - Response: `JobStatus`

- GET `/api/v1/jobs/{flinkJobId}/metrics`
  - Response: `JobMetrics`

- POST `/api/v1/jobs/{flinkJobId}/cancel`
  - Response: 200 on success

- GET `/api/v1/health`
  - Response: `OK`

Note: When the IR Runner jar is fully wired, `submit` ensures the jar is uploaded and runs it with the IR as an argument. Until then, submissions validate the IR and exercise the gateway pipeline.
