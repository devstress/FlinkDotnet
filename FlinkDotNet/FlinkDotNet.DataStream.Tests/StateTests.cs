using System;
using FlinkDotNet.DataStream.State;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StateDescriptorTests
    {
        #region ValueStateDescriptor Tests

        [Test]
        public void ValueStateDescriptor_Constructor_WithValidName_CreatesDescriptor()
        {
            // Arrange
            string name = "test-value-state";

            // Act
            var descriptor = new ValueStateDescriptor<int>(name);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo(name));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void ValueStateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ValueStateDescriptor<string>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void ValueStateDescriptor_WithDifferentTypes_CreatesCorrectTypeInfo()
        {
            // Arrange & Act
            var intDescriptor = new ValueStateDescriptor<int>("int-state");
            var stringDescriptor = new ValueStateDescriptor<string>("string-state");
            var doubleDescriptor = new ValueStateDescriptor<double>("double-state");

            // Assert
            Assert.That(intDescriptor.ValueType, Is.EqualTo(typeof(int)));
            Assert.That(stringDescriptor.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(doubleDescriptor.ValueType, Is.EqualTo(typeof(double)));
        }

        #endregion

        #region ListStateDescriptor Tests

        [Test]
        public void ListStateDescriptor_Constructor_WithValidName_CreatesDescriptor()
        {
            // Arrange
            string name = "test-list-state";

            // Act
            var descriptor = new ListStateDescriptor<string>(name);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo(name));
            Assert.That(descriptor.ElementType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void ListStateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ListStateDescriptor<int>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void ListStateDescriptor_WithDifferentTypes_CreatesCorrectTypeInfo()
        {
            // Arrange & Act
            var intDescriptor = new ListStateDescriptor<int>("int-list");
            var stringDescriptor = new ListStateDescriptor<string>("string-list");

            // Assert
            Assert.That(intDescriptor.ElementType, Is.EqualTo(typeof(int)));
            Assert.That(stringDescriptor.ElementType, Is.EqualTo(typeof(string)));
        }

        #endregion

        #region MapStateDescriptor Tests

        [Test]
        public void MapStateDescriptor_Constructor_WithValidName_CreatesDescriptor()
        {
            // Arrange
            string name = "test-map-state";

            // Act
            var descriptor = new MapStateDescriptor<string, int>(name);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo(name));
            Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void MapStateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MapStateDescriptor<string, int>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void MapStateDescriptor_WithDifferentTypes_CreatesCorrectTypeInfo()
        {
            // Arrange & Act
            var descriptor1 = new MapStateDescriptor<string, int>("map1");
            var descriptor2 = new MapStateDescriptor<int, string>("map2");
            var descriptor3 = new MapStateDescriptor<string, double>("map3");

            // Assert
            Assert.That(descriptor1.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor1.ValueType, Is.EqualTo(typeof(int)));
            Assert.That(descriptor2.KeyType, Is.EqualTo(typeof(int)));
            Assert.That(descriptor2.ValueType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor3.KeyType, Is.EqualTo(typeof(string)));
            Assert.That(descriptor3.ValueType, Is.EqualTo(typeof(double)));
        }

        #endregion

        #region ReducingStateDescriptor Tests

        [Test]
        public void ReducingStateDescriptor_Constructor_WithValidParameters_CreatesDescriptor()
        {
            // Arrange
            string name = "test-reducing-state";
            var reduceFunction = new TestReduceFunction();

            // Act
            var descriptor = new ReducingStateDescriptor<int>(name, reduceFunction);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo(name));
            Assert.That(descriptor.ReduceFunction, Is.SameAs(reduceFunction));
        }

        [Test]
        public void ReducingStateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var reduceFunction = new TestReduceFunction();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>(null!, reduceFunction));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void ReducingStateDescriptor_Constructor_WithNullFunction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>("test", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("reduceFunction"));
        }

        #endregion

        #region AggregatingStateDescriptor Tests

        [Test]
        public void AggregatingStateDescriptor_Constructor_WithValidParameters_CreatesDescriptor()
        {
            // Arrange
            string name = "test-aggregating-state";
            var aggregateFunction = new TestAggregateFunction();

            // Act
            var descriptor = new AggregatingStateDescriptor<int, int, int>(name, aggregateFunction);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
            Assert.That(descriptor.Name, Is.EqualTo(name));
            Assert.That(descriptor.AggregateFunction, Is.SameAs(aggregateFunction));
        }

        [Test]
        public void AggregatingStateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var aggregateFunction = new TestAggregateFunction();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AggregatingStateDescriptor<int, int, int>(null!, aggregateFunction));
            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [Test]
        public void AggregatingStateDescriptor_Constructor_WithNullFunction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new AggregatingStateDescriptor<int, int, int>("test", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("aggregateFunction"));
        }

        #endregion

        #region StateDescriptor Base Class Tests

        [Test]
        public void StateDescriptor_Name_IsReadOnly()
        {
            // Arrange
            var descriptor = new ValueStateDescriptor<int>("test-name");

            // Act
            var name = descriptor.Name;

            // Assert
            Assert.That(name, Is.EqualTo("test-name"));
            // Verify Name property is get-only (no set accessor)
            Assert.That(typeof(StateDescriptor).GetProperty("Name")!.CanWrite, Is.False);
        }

        [Test]
        public void StateDescriptors_WithSameName_AreIndependent()
        {
            // Arrange
            string sameName = "shared-name";

            // Act
            var descriptor1 = new ValueStateDescriptor<int>(sameName);
            var descriptor2 = new ListStateDescriptor<string>(sameName);

            // Assert
            Assert.That(descriptor1.Name, Is.EqualTo(sameName));
            Assert.That(descriptor2.Name, Is.EqualTo(sameName));
            // Verify they are different descriptor types
            Assert.That(descriptor1.GetType(), Is.Not.EqualTo(descriptor2.GetType()));
        }

        #endregion

        #region Helper Test Classes

        private class TestReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2)
            {
                return value1 + value2;
            }
        }

        private class TestAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int acc1, int acc2) => acc1 + acc2;
        }

        #endregion
    }

    [TestFixture]
    public class EmbeddedRocksDBStateBackendTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_Default_CreatesBackendWithDefaultSettings()
        {
            // Act
            var backend = new EmbeddedRocksDBStateBackend();

            // Assert
            Assert.That(backend, Is.Not.Null);
            Assert.That(backend.GetName(), Is.EqualTo("EmbeddedRocksDBStateBackend"));
            Assert.That(backend.SupportsIncrementalCheckpointing(), Is.True);
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.DEFAULT));
            Assert.That(backend.GetDbStoragePath(), Is.Null);
        }

        [Test]
        public void Constructor_WithIncrementalCheckpointingTrue_EnablesIncrementalCheckpointing()
        {
            // Act
            var backend = new EmbeddedRocksDBStateBackend(enableIncrementalCheckpointing: true);

            // Assert
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
        }

        [Test]
        public void Constructor_WithIncrementalCheckpointingFalse_DisablesIncrementalCheckpointing()
        {
            // Act
            var backend = new EmbeddedRocksDBStateBackend(enableIncrementalCheckpointing: false);

            // Assert
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.False);
        }

        #endregion

        #region SetPredefinedOptions Tests

        [Test]
        public void SetPredefinedOptions_WithDefaultOption_SetsPredefinedOptions()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = backend.SetPredefinedOptions(RocksDBPredefinedOptions.DEFAULT);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.DEFAULT));
        }

        [Test]
        public void SetPredefinedOptions_WithSpinningDiskOptimized_SetsPredefinedOptions()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED));
        }

        [Test]
        public void SetPredefinedOptions_WithFlashSsdOptimized_SetsPredefinedOptions()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = backend.SetPredefinedOptions(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
        }

        [Test]
        public void SetPredefinedOptions_WithSpinningDiskOptimizedHighMem_SetsPredefinedOptions()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
        }

        [Test]
        public void SetPredefinedOptions_MultipleCallsWithDifferentOptions_OverridesPreviousValue()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED);
            backend.SetPredefinedOptions(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED);

            // Assert
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
        }

        #endregion

        #region SetDbStoragePath Tests

        [Test]
        public void SetDbStoragePath_WithValidPath_SetsStoragePath()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();
            string path = "/tmp/rocksdb-data";

            // Act
            var result = backend.SetDbStoragePath(path);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetDbStoragePath(), Is.EqualTo(path));
        }

        [Test]
        public void SetDbStoragePath_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => backend.SetDbStoragePath(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
            Assert.That(ex.Message, Does.Contain("RocksDB storage path cannot be null or empty"));
        }

        [Test]
        public void SetDbStoragePath_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => backend.SetDbStoragePath(""));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
            Assert.That(ex.Message, Does.Contain("RocksDB storage path cannot be null or empty"));
        }

        [Test]
        public void SetDbStoragePath_WithWhitespacePath_ThrowsArgumentException()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => backend.SetDbStoragePath("   "));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
            Assert.That(ex.Message, Does.Contain("RocksDB storage path cannot be null or empty"));
        }

        [Test]
        public void SetDbStoragePath_MultipleCallsWithDifferentPaths_OverridesPreviousValue()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            backend.SetDbStoragePath("/tmp/path1");
            backend.SetDbStoragePath("/tmp/path2");

            // Assert
            Assert.That(backend.GetDbStoragePath(), Is.EqualTo("/tmp/path2"));
        }

        #endregion

        #region EnableIncrementalCheckpointing Tests

        [Test]
        public void EnableIncrementalCheckpointing_WithTrue_EnablesIncrementalCheckpointing()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend(false);

            // Act
            var result = backend.EnableIncrementalCheckpointing(true);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
        }

        [Test]
        public void EnableIncrementalCheckpointing_WithFalse_DisablesIncrementalCheckpointing()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend(true);

            // Act
            var result = backend.EnableIncrementalCheckpointing(false);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.False);
        }

        [Test]
        public void EnableIncrementalCheckpointing_WithoutParameter_EnablesIncrementalCheckpointing()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend(false);

            // Act
            var result = backend.EnableIncrementalCheckpointing();

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
        }

        [Test]
        public void EnableIncrementalCheckpointing_MultipleCallsWithDifferentValues_OverridesPreviousValue()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend(true);

            // Act
            backend.EnableIncrementalCheckpointing(false);
            backend.EnableIncrementalCheckpointing(true);

            // Assert
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_AllConfigurationMethods_ReturnsSameInstance()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = backend
                .SetPredefinedOptions(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED)
                .SetDbStoragePath("/tmp/rocksdb")
                .EnableIncrementalCheckpointing(true);

            // Assert
            Assert.That(result, Is.SameAs(backend));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
            Assert.That(backend.GetDbStoragePath(), Is.EqualTo("/tmp/rocksdb"));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
        }

        [Test]
        public void MethodChaining_ComplexConfiguration_AllSettingsApplied()
        {
            // Arrange & Act
            var backend = new EmbeddedRocksDBStateBackend()
                .SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM)
                .SetDbStoragePath("/data/flink/rocksdb")
                .EnableIncrementalCheckpointing(false);

            // Assert
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
            Assert.That(backend.GetDbStoragePath(), Is.EqualTo("/data/flink/rocksdb"));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.False);
        }

        #endregion

        #region IStateBackend Interface Tests

        [Test]
        public void GetName_ReturnsCorrectName()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var name = backend.GetName();

            // Assert
            Assert.That(name, Is.EqualTo("EmbeddedRocksDBStateBackend"));
        }

        [Test]
        public void SupportsIncrementalCheckpointing_AlwaysReturnsTrue()
        {
            // Arrange
            var backend1 = new EmbeddedRocksDBStateBackend(true);
            var backend2 = new EmbeddedRocksDBStateBackend(false);

            // Act & Assert
            Assert.That(backend1.SupportsIncrementalCheckpointing(), Is.True);
            Assert.That(backend2.SupportsIncrementalCheckpointing(), Is.True);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Test]
        public void GetDbStoragePath_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var path = backend.GetDbStoragePath();

            // Assert
            Assert.That(path, Is.Null);
        }

        [Test]
        public void Configuration_WithAllDefaults_IsValid()
        {
            // Arrange & Act
            var backend = new EmbeddedRocksDBStateBackend();

            // Assert
            Assert.That(backend.GetName(), Is.EqualTo("EmbeddedRocksDBStateBackend"));
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.DEFAULT));
            Assert.That(backend.GetDbStoragePath(), Is.Null);
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
            Assert.That(backend.SupportsIncrementalCheckpointing(), Is.True);
        }

        [Test]
        public void Configuration_WithAllCustomSettings_IsValid()
        {
            // Arrange & Act
            var backend = new EmbeddedRocksDBStateBackend(false)
                .SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED)
                .SetDbStoragePath("/custom/path/rocksdb");

            // Assert
            Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED));
            Assert.That(backend.GetDbStoragePath(), Is.EqualTo("/custom/path/rocksdb"));
            Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.False);
        }

        #endregion

        #region RocksDBPredefinedOptions Enum Tests

        [Test]
        public void RocksDBPredefinedOptions_AllEnumValues_AreAccessible()
        {
            // Act & Assert - Verify all enum values exist and can be accessed
            var defaultValue = RocksDBPredefinedOptions.DEFAULT;
            var spinningDiskValue = RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED;
            var flashSsdValue = RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED;
            var highMemValue = RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM;

            // Verify values are defined
            Assert.That(System.Enum.IsDefined(typeof(RocksDBPredefinedOptions), defaultValue), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(RocksDBPredefinedOptions), spinningDiskValue), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(RocksDBPredefinedOptions), flashSsdValue), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(RocksDBPredefinedOptions), highMemValue), Is.True);
        }

        [Test]
        public void RocksDBPredefinedOptions_EnumValues_AreDistinct()
        {
            // Assert - Each enum value should be different
            Assert.That(RocksDBPredefinedOptions.DEFAULT, Is.Not.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED));
            Assert.That(RocksDBPredefinedOptions.DEFAULT, Is.Not.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
            Assert.That(RocksDBPredefinedOptions.DEFAULT, Is.Not.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
            Assert.That(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED, Is.Not.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
            Assert.That(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED, Is.Not.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
            Assert.That(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED, Is.Not.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
        }

        #endregion
    }

    [TestFixture]
    public class HashMapStateBackendTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_Default_CreatesBackendWithCorrectConfiguration()
        {
            // Act
            var backend = new HashMapStateBackend();

            // Assert
            Assert.That(backend, Is.Not.Null);
            Assert.That(backend.GetName(), Is.EqualTo("HashMapStateBackend"));
            Assert.That(backend.SupportsIncrementalCheckpointing(), Is.False);
        }

        #endregion

        #region IStateBackend Interface Tests

        [Test]
        public void GetName_ReturnsCorrectName()
        {
            // Arrange
            var backend = new HashMapStateBackend();

            // Act
            var name = backend.GetName();

            // Assert
            Assert.That(name, Is.EqualTo("HashMapStateBackend"));
        }

        [Test]
        public void SupportsIncrementalCheckpointing_AlwaysReturnsFalse()
        {
            // Arrange
            var backend = new HashMapStateBackend();

            // Act
            var supportsIncremental = backend.SupportsIncrementalCheckpointing();

            // Assert
            Assert.That(supportsIncremental, Is.False);
        }

        #endregion

        #region Multiple Instances Tests

        [Test]
        public void MultipleInstances_AreIndependent()
        {
            // Act
            var backend1 = new HashMapStateBackend();
            var backend2 = new HashMapStateBackend();

            // Assert
            Assert.That(backend1, Is.Not.SameAs(backend2));
            Assert.That(backend1.GetName(), Is.EqualTo(backend2.GetName()));
            Assert.That(backend1.SupportsIncrementalCheckpointing(), Is.EqualTo(backend2.SupportsIncrementalCheckpointing()));
        }

        #endregion
    }

    [TestFixture]
    public class StateBackendComparisonTests
    {
        [Test]
        public void EmbeddedRocksDBStateBackend_SupportsIncrementalCheckpointing_HashMapStateBackendDoesNot()
        {
            // Arrange
            var rocksdbBackend = new EmbeddedRocksDBStateBackend();
            var hashmapBackend = new HashMapStateBackend();

            // Act & Assert
            Assert.That(rocksdbBackend.SupportsIncrementalCheckpointing(), Is.True);
            Assert.That(hashmapBackend.SupportsIncrementalCheckpointing(), Is.False);
        }

        [Test]
        public void StateBackends_HaveDifferentNames()
        {
            // Arrange
            var rocksdbBackend = new EmbeddedRocksDBStateBackend();
            var hashmapBackend = new HashMapStateBackend();

            // Act & Assert
            Assert.That(rocksdbBackend.GetName(), Is.Not.EqualTo(hashmapBackend.GetName()));
            Assert.That(rocksdbBackend.GetName(), Is.EqualTo("EmbeddedRocksDBStateBackend"));
            Assert.That(hashmapBackend.GetName(), Is.EqualTo("HashMapStateBackend"));
        }

        [Test]
        public void StateBackends_ImplementIStateBackendInterface()
        {
            // Arrange
            var rocksdbBackend = new EmbeddedRocksDBStateBackend();
            var hashmapBackend = new HashMapStateBackend();

            // Act & Assert
            Assert.That(rocksdbBackend, Is.InstanceOf<IStateBackend>());
            Assert.That(hashmapBackend, Is.InstanceOf<IStateBackend>());
        }
    }
}
