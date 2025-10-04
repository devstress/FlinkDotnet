# LocalTesting Flink Connector Library

This directory is mounted into Flink containers at `/opt/flink/usrlib/` and loaded via `FLINK_CLASSPATH`.

## Important: Version Compatibility

**The connector JARs in this directory MUST match your Flink cluster version precisely!**

Current Flink cluster version: **2.1.0-java17** (see `LocalTesting.FlinkSqlAppHost/Program.cs`)

## Required Connectors for SQL Jobs

For SQL-based jobs (Pattern 5 & 6), you need compatible Flink 2.x connectors:

- `flink-sql-connector-kafka` (version 4.x.x-2.0 or compatible with Flink 2.x)
- `flink-sql-json` (version 2.1.0)
- `flink-table-planner_2.12` (version 2.1.0)
- `flink-table-runtime` (version 2.1.0)

## Currently Installed Connectors

- `flink-sql-connector-kafka-4.0.1-2.0.jar` - **Compatible with Flink 2.0/2.1**

This is the latest official Flink SQL Kafka connector from Maven Central compatible with Flink 2.x series.

## DataStream API Jobs

DataStream API jobs (Patterns 1-4, 7) do NOT require these connectors - they use the `kafka-clients` library which is bundled in the FlinkIRRunner JAR.

## Installation

Download compatible connector JARs and place them in this directory. The LocalTesting Aspire host will automatically mount them into the Flink containers.

If targeting a production cluster, copy these JARs to `/opt/flink/lib` (or your distribution's equivalent).

## Version Notes

- **Flink 2.0 connectors** (4.0.x-2.0) are compatible with Flink 2.1.0
- Connector version follows pattern: `<connector-version>-<flink-major-version>`
- Always use connectors matching your Flink major version (2.x for Flink 2.1.0)
