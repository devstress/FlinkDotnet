using System;
using System.Reflection;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Targeted tests to improve coverage for data classes that aren't being properly instrumented.
    /// These tests explicitly access all properties to ensure coverage tools detect the usage.
    /// </summary>
    [TestFixture]
    public class CoverageImprovementForDataClassesTests
    {
        #region JobExecutionResult Property Coverage
        
        [Test]
        public void JobExecutionResult_AllProperties_AreCovered()
        {
            // Create instance and set all properties
            var result = new JobExecutionResult
            {
                JobName = "TestJob",
                Success = true,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(5),
                Error = "Test error"
            };
            
            // Access all properties to ensure coverage
            Assert.That(result.JobName, Is.EqualTo("TestJob"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.EndTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.Error, Is.EqualTo("Test error"));
            
            // Use reflection to ensure all properties are accessed
            var type = typeof(JobExecutionResult);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(result);
                Assert.That(value, Is.Not.Null.Or.Empty, $"Property {prop.Name} should have a value");
            }
        }
        
        #endregion
        
        #region JobStatus Property Coverage
        
        [Test]
        public void JobStatus_AllProperties_AreCovered()
        {
            // Create instance and set all properties
            var status = new JobStatus
            {
                FlinkJobId = "job-123",
                JobName = "TestJob",
                State = "RUNNING",
                Parallelism = 4,
                MaxParallelism = 128,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Error = null
            };
            
            // Access all properties
            Assert.That(status.FlinkJobId, Is.EqualTo("job-123"));
            Assert.That(status.JobName, Is.EqualTo("TestJob"));
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Parallelism, Is.EqualTo(4));
            Assert.That(status.MaxParallelism, Is.EqualTo(128));
            Assert.That(status.StartTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(status.EndTime, Is.Not.Null);
            Assert.That(status.Error, Is.Null);
            
            // Use reflection
            var type = typeof(JobStatus);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(status);
                // EndTime and Error can be null
                if (prop.Name != "EndTime" && prop.Name != "Error")
                {
                    Assert.That(value, Is.Not.Null, $"Property {prop.Name} should not be null");
                }
            }
        }
        
        #endregion
        
        #region SavepointResult Property Coverage
        
        [Test]
        public void SavepointResult_AllProperties_AreCovered()
        {
            // Create instance and set all properties
            var result = new SavepointResult
            {
                SavepointPath = "/tmp/savepoints/sp-123",
                Success = true,
                TriggerId = "trigger-456",
                Error = null
            };
            
            // Access all properties
            Assert.That(result.SavepointPath, Is.EqualTo("/tmp/savepoints/sp-123"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
            Assert.That(result.Error, Is.Null);
            
            // Use reflection
            var type = typeof(SavepointResult);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(result);
                // Error can be null, Success is bool
                if (prop.Name != "Error")
                {
                    if (prop.PropertyType == typeof(bool))
                    {
                        Assert.That(value, Is.Not.Null, $"Property {prop.Name} should not be null");
                    }
                    else
                    {
                        Assert.That(value, Is.Not.Null.And.Not.Empty, $"Property {prop.Name} should have a value");
                    }
                }
            }
        }
        
        #endregion
        
        #region StopWithSavepointResult Property Coverage
        
        [Test]
        public void StopWithSavepointResult_AllProperties_AreCovered()
        {
            // Create instance and set all properties
            var result = new StopWithSavepointResult
            {
                SavepointPath = "/tmp/savepoints/stop-sp-123",
                Success = true,
                TriggerId = "stop-trigger-456",
                Drained = true,
                Error = null
            };
            
            // Access all properties
            Assert.That(result.SavepointPath, Is.EqualTo("/tmp/savepoints/stop-sp-123"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("stop-trigger-456"));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.Null);
            
            // Use reflection
            var type = typeof(StopWithSavepointResult);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(result);
                // Error can be null
                if (prop.Name != "Error")
                {
                    Assert.That(value, Is.Not.Null, $"Property {prop.Name} should have a value");
                }
            }
        }
        
        #endregion
        
        #region ModelDescription Property Coverage
        
        [Test]
        public void ModelDescription_AllProperties_AreCovered()
        {
            // Create instance with init properties
            var description = new ModelDescription
            {
                ModelName = "test-model",
                Provider = "openai",
                InputSchema = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["input"] = "STRING"
                },
                OutputSchema = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["output"] = "STRING"
                },
                Properties = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["api_key"] = "test-key"
                }
            };
            
            // Access all properties
            Assert.That(description.ModelName, Is.EqualTo("test-model"));
            Assert.That(description.Provider, Is.EqualTo("openai"));
            Assert.That(description.InputSchema, Has.Count.EqualTo(1));
            Assert.That(description.OutputSchema, Has.Count.EqualTo(1));
            Assert.That(description.Properties, Has.Count.EqualTo(1));
            
            // Use reflection
            var type = typeof(ModelDescription);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(description);
                Assert.That(value, Is.Not.Null, $"Property {prop.Name} should not be null");
            }
        }
        
        #endregion
        
        #region SinkWriterContext Property Coverage
        
        [Test]
        public void SinkWriterContext_AllProperties_AreCovered()
        {
            // Create instance with init properties
            var context = new SinkWriterContext
            {
                SubtaskId = 0,
                NumberOfParallelSubtasks = 4,
                AttemptNumber = 0,
                Properties = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["key"] = "value"
                }
            };
            
            // Access all properties
            Assert.That(context.SubtaskId, Is.EqualTo(0));
            Assert.That(context.NumberOfParallelSubtasks, Is.EqualTo(4));
            Assert.That(context.AttemptNumber, Is.EqualTo(0));
            Assert.That(context.Properties, Has.Count.EqualTo(1));
            
            // Use reflection
            var type = typeof(SinkWriterContext);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(context);
                Assert.That(value, Is.Not.Null, $"Property {prop.Name} should not be null");
            }
        }
        
        #endregion
        
        #region RocksDBOptions Property Coverage
        
        [Test]
        public void RocksDBOptions_AllProperties_AreCovered()
        {
            // Create instance with init properties
            var options = new RocksDBOptions
            {
                MaxBackgroundJobs = 4,
                MaxWriteBufferNumber = 2,
                WriteBufferSize = 64 * 1024 * 1024,
                BlockCacheSize = 128 * 1024 * 1024,
                UseBloomFilter = true,
                CompactionStyle = "LEVEL",
                Properties = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["compression"] = "snappy"
                }
            };
            
            // Access all properties
            Assert.That(options.MaxBackgroundJobs, Is.EqualTo(4));
            Assert.That(options.MaxWriteBufferNumber, Is.EqualTo(2));
            Assert.That(options.WriteBufferSize, Is.EqualTo(64 * 1024 * 1024));
            Assert.That(options.BlockCacheSize, Is.EqualTo(128 * 1024 * 1024));
            Assert.That(options.UseBloomFilter, Is.True);
            Assert.That(options.CompactionStyle, Is.EqualTo("LEVEL"));
            Assert.That(options.Properties, Has.Count.EqualTo(1));
            
            // Use reflection to ensure all properties are accessed
            var type = typeof(RocksDBOptions);
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(options);
                Assert.That(value, Is.Not.Null, $"Property {prop.Name} should not be null");
            }
        }
        
        #endregion
        
        #region State Descriptor Coverage with Proper Instantiation
        
        [Test]
        public void ValueStateDescriptor_PropertyAccess_IsCovered()
        {
            var descriptor = new ValueStateDescriptor<string>("test-state");
            
            // Access the Name property through the base class
            StateDescriptor baseDescriptor = descriptor;
            Assert.That(baseDescriptor.Name, Is.EqualTo("test-state"));
            
            // Access the ValueType property
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(string)));
            
            // Use reflection
            var value = typeof(ValueStateDescriptor<string>).GetProperty("ValueType")?.GetValue(descriptor);
            Assert.That(value, Is.EqualTo(typeof(string)));
        }
        
        [Test]
        public void ListStateDescriptor_PropertyAccess_IsCovered()
        {
            var descriptor = new ListStateDescriptor<int>("list-state");
            
            // Access properties
            Assert.That(descriptor.Name, Is.EqualTo("list-state"));
            Assert.That(descriptor.ElementType, Is.EqualTo(typeof(int)));
            
            // Use reflection
            var value = typeof(ListStateDescriptor<int>).GetProperty("ElementType")?.GetValue(descriptor);
            Assert.That(value, Is.EqualTo(typeof(int)));
        }
        
        [Test]
        public void MapStateDescriptor_PropertyAccess_IsCovered()
        {
            var descriptor = new MapStateDescriptor<string, int>("map-state");
            
            // Access properties
            Assert.That(descriptor.Name, Is.EqualTo("map-state"));
            Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
            
            // Use reflection
            var keyType = typeof(MapStateDescriptor<string, int>).GetProperty("KeyType")?.GetValue(descriptor);
            var valueType = typeof(MapStateDescriptor<string, int>).GetProperty("ValueType")?.GetValue(descriptor);
            Assert.That(keyType, Is.EqualTo(typeof(string)));
            Assert.That(valueType, Is.EqualTo(typeof(int)));
        }
        
        [Test]
        public void ReducingStateDescriptor_PropertyAccess_IsCovered()
        {
            var reduceFunc = new TestReduceFunction();
            var descriptor = new ReducingStateDescriptor<int>("reducing-state", reduceFunc);
            
            // Access properties
            Assert.That(descriptor.Name, Is.EqualTo("reducing-state"));
            Assert.That(descriptor.ReduceFunction, Is.SameAs(reduceFunc));
            
            // Use reflection
            var func = typeof(ReducingStateDescriptor<int>).GetProperty("ReduceFunction")?.GetValue(descriptor);
            Assert.That(func, Is.SameAs(reduceFunc));
        }
        
        [Test]
        public void AggregatingStateDescriptor_PropertyAccess_IsCovered()
        {
            var aggregateFunc = new TestAggregateFunction();
            var descriptor = new AggregatingStateDescriptor<int, int, int>("aggregating-state", aggregateFunc);
            
            // Access properties
            Assert.That(descriptor.Name, Is.EqualTo("aggregating-state"));
            Assert.That(descriptor.AggregateFunction, Is.SameAs(aggregateFunc));
            
            // Use reflection
            var func = typeof(AggregatingStateDescriptor<int, int, int>).GetProperty("AggregateFunction")?.GetValue(descriptor);
            Assert.That(func, Is.SameAs(aggregateFunc));
        }
        
        #endregion
        
        #region Helper Classes
        
        private class TestReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2) => value1 + value2;
        }
        
        private class TestAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }
        
        #endregion
    }
}
