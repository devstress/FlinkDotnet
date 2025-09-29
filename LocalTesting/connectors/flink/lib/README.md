# LocalTesting Flink Connector Library

Drop any optional Flink SQL or connector JARs in this directory when you need them for local runs. The LocalTesting host exposes this path to the Flink Job Gateway so the jars are bundled automatically when jobs are submitted.

If you target a real cluster you can copy these jars into `/opt/flink/lib` (or your distribution's equivalent) so they are available at runtime.

