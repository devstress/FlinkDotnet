# LocalTesting Flink Connector Library

This directory is mounted into Flink containers at `/opt/flink/usrlib/` and loaded via `FLINK_CLASSPATH`.

## Important: Version Compatibility

**The connector JARs in this directory MUST match your Flink cluster version precisely!**

Current Flink cluster version: **2.1.0-java17** (see `LocalTesting.FlinkSqlAppHost/Program.cs`)

## Required Connectors for SQL Jobs

For SQL-based jobs (Pattern 5 & 6), you need compatible Flink 2.1 connectors:

- `flink-sql-connector-kafka` (version 3.x.x-2.1 or compatible)
- `flink-sql-json` (version 2.1.0)
- `flink-table-planner_2.12` (version 2.1.0)
- `flink-table-runtime` (version 2.1.0)

**Note**: As of early 2025, Flink 2.1 compatible SQL connectors (version 3.x.x-2.1) are not yet publicly available. We are temporarily using the Flink 1.20 connector (version 3.3.0-1.20) which may have compatibility issues.

## Currently Installed Connectors

- `flink-sql-connector-kafka-3.3.0-1.20.jar` - **WARNING: Flink 1.20 version, may not be fully compatible with Flink 2.1.0**

## DataStream API Jobs

DataStream API jobs (Patterns 1-4, 7) do NOT require these connectors - they use the `kafka-clients` library which is bundled in the FlinkIRRunner JAR.

## Installation

Download compatible connector JARs and place them in this directory. The LocalTesting Aspire host will automatically mount them into the Flink containers.

If targeting a production cluster, copy these JARs to `/opt/flink/lib` (or your distribution's equivalent).

## Known Issue

SQL pattern tests may fail due to version incompatibility between Flink 2.1.0 and the Flink 1.20 SQL connector. This will be resolved once Flink 2.1 compatible connectors (version 3.x.x-2.1) are released.
