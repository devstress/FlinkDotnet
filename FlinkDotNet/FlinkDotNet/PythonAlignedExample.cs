//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using FlinkDotNet.Common;

namespace FlinkDotNet.Examples
{
    /// <summary>
    /// Example demonstrating the new Python-aligned API structure.
    /// This shows how FlinkDotNet now matches PyFlink patterns.
    /// </summary>
    public static class PythonAlignedExample
    {
        /// <summary>
        /// Python equivalent:
        /// env = StreamExecutionEnvironment.get_execution_environment()
        /// env.set_parallelism(4)
        /// ds = env.from_collection([1, 2, 3, 4, 5])
        /// ds.map(lambda x: x * 2).print()
        /// env.execute("Python Example")
        /// </summary>
        public static async Task PythonLikeUsage()
        {
            // Get execution environment (matches Python pattern)
            var env = Flink.GetExecutionEnvironment();
            
            // Configure environment (matches Python API)
            env.SetParallelism(4);
            
            // Create data stream from collection (matches Python from_collection)
            var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
            
            // Apply transformations (matches Python patterns)
            var transformedStream = dataStream
                .Map(x => x * 2)
                .Filter(x => x > 5);
            
            // Print results (matches Python print())
            transformedStream.Print();
            
            // Execute job (matches Python execute())
            await env.ExecuteAsync("Python-like Example");
        }

        /// <summary>
        /// Example showing configuration usage matching Python Configuration patterns.
        /// Python equivalent:
        /// config = Configuration()
        /// config.set_string("parallelism.default", "8")
        /// env = StreamExecutionEnvironment.get_execution_environment(config)
        /// </summary>
        public static async Task ConfigurationExample()
        {
            // Create configuration (matches Python Configuration())
            var config = Flink.CreateConfiguration();
            config.SetString("parallelism.default", "8");
            config.SetInteger("buffer.timeout", 100);
            
            // Get environment with configuration
            var env = Flink.GetExecutionEnvironment(config);
            
            // The environment now has the configured settings
            await env.ExecuteAsync("Configuration Example");
        }

        /// <summary>
        /// Example showing backward compatibility with existing FlinkJobBuilder.
        /// This preserves all existing functionality while adding Python alignment.
        /// </summary>
        public static async Task BackwardCompatibilityExample()
        {
            // Old API still works (backward compatibility)
            var job = Flink.JobBuilder
                .FromKafka("orders")
                .Where("Amount > 100")
                .GroupBy("Region")
                .Aggregate("SUM", "Amount")
                .ToKafka("high-value-orders");

            var result = await job.Submit("Legacy Example");
            Console.WriteLine($"Job submitted: {result.FlinkJobId}");
        }

        /// <summary>
        /// Example showing modular structure usage.
        /// Each module can be used independently, just like in Python Flink.
        /// </summary>
        public static void ModularStructureExample()
        {
            // FlinkDotNet.Common usage
            var config = new Configuration();
            var execConfig = new ExecutionConfig(config);
            execConfig.SetParallelism(4);

            // FlinkDotNet.DataStream usage - using factory method instead of constructor
            _ = Flink.GetExecutionEnvironment(config);
            
            // Each module has its own namespace and functionality
            Console.WriteLine($"Parallelism: {execConfig.Parallelism}");
            Console.WriteLine("Environment configured successfully");
        }
    }
}