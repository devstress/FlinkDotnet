using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class AdvancedFunctionsTests
{
    #region OutputTag Tests

    [Test]
    public void OutputTag_Constructor_StoresId()
    {
        var tag = new OutputTag<string>("test-tag");

        Assert.That(tag.Id, Is.EqualTo("test-tag"));
    }

    [Test]
    public void OutputTag_Constructor_ThrowsOnNullId()
    {
        Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
    }

    [Test]
    public void OutputTag_Equals_ReturnsTrueForSameId()
    {
        var tag1 = new OutputTag<string>("test-tag");
        var tag2 = new OutputTag<string>("test-tag");

        Assert.That(tag1.Equals(tag2), Is.True);
    }

    [Test]
    public void OutputTag_Equals_ReturnsFalseForDifferentId()
    {
        var tag1 = new OutputTag<string>("test-tag-1");
        var tag2 = new OutputTag<string>("test-tag-2");

        Assert.That(tag1.Equals(tag2), Is.False);
    }

    [Test]
    public void OutputTag_Equals_ReturnsFalseForDifferentType()
    {
        var tag1 = new OutputTag<string>("test-tag");
        var tag2 = new OutputTag<int>("test-tag");

        Assert.That(tag1.Equals(tag2), Is.False);
    }

    [Test]
    public void OutputTag_Equals_ReturnsFalseForNull()
    {
        var tag = new OutputTag<string>("test-tag");

        Assert.That(tag.Equals(null), Is.False);
    }

    [Test]
    public void OutputTag_Equals_ReturnsFalseForNonOutputTag()
    {
        var tag = new OutputTag<string>("test-tag");

        Assert.That(tag.Equals("test-tag"), Is.False);
    }

    [Test]
    public void OutputTag_GetHashCode_ReturnsSameForSameId()
    {
        var tag1 = new OutputTag<string>("test-tag");
        var tag2 = new OutputTag<string>("test-tag");

        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }

    [Test]
    public void OutputTag_GetHashCode_ReturnsDifferentForDifferentId()
    {
        var tag1 = new OutputTag<string>("test-tag-1");
        var tag2 = new OutputTag<string>("test-tag-2");

        Assert.That(tag1.GetHashCode(), Is.Not.EqualTo(tag2.GetHashCode()));
    }

    #endregion

    #region TimeDomain Tests

    [Test]
    public void TimeDomain_HasEventTimeValue()
    {
        var timeDomain = TimeDomain.EventTime;
        Assert.That(timeDomain, Is.EqualTo(TimeDomain.EventTime));
    }

    [Test]
    public void TimeDomain_HasProcessingTimeValue()
    {
        var timeDomain = TimeDomain.ProcessingTime;
        Assert.That(timeDomain, Is.EqualTo(TimeDomain.ProcessingTime));
    }

    [Test]
    public void TimeDomain_EventTimeAndProcessingTimeAreDifferent()
    {
        Assert.That(TimeDomain.EventTime, Is.Not.EqualTo(TimeDomain.ProcessingTime));
    }

    #endregion

    #region IAsyncFunction Tests - Default Implementation

    [Test]
    public void IAsyncFunction_TimeoutAsync_DefaultImplementation_CompletesWithEmptyArray()
    {
        IAsyncFunction<string, string> testAsyncFunc = new TestAsyncFunction();
        var resultFuture = new TestResultFuture<string>();

        var task = testAsyncFunc.TimeoutAsync("test-input", resultFuture);

        Assert.That(task.IsCompleted, Is.True);
        Assert.That(resultFuture.CompletedResults, Is.Empty);
    }

    private class TestAsyncFunction : IAsyncFunction<string, string>
    {
        public Task AsyncInvokeAsync(string input, IResultFuture<string> resultFuture)
        {
            resultFuture.Complete(new[] { input.ToUpper() });
            return Task.CompletedTask;
        }

        // TimeoutAsync uses default implementation
    }

    private class TestResultFuture<T> : IResultFuture<T>
    {
        public IEnumerable<T>? CompletedResults { get; private set; }
        public Exception? CompletedException { get; private set; }

        public void Complete(IEnumerable<T> results)
        {
            CompletedResults = results;
        }

        public void CompleteExceptionally(Exception exception)
        {
            CompletedException = exception;
        }
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void IProcessFunction_CanBeImplemented()
    {
        IProcessFunction<string, int> processFunc = new TestProcessFunction();
        Assert.That(processFunc, Is.Not.Null);
    }

    [Test]
    public void IKeyedProcessFunction_CanBeImplemented()
    {
        IKeyedProcessFunction<string, int, int> keyedProcessFunc = new TestKeyedProcessFunction();
        Assert.That(keyedProcessFunc, Is.Not.Null);
    }

    [Test]
    public void ICoProcessFunction_CanBeImplemented()
    {
        ICoProcessFunction<string, int, string> coProcessFunc = new TestCoProcessFunction();
        Assert.That(coProcessFunc, Is.Not.Null);
    }

    [Test]
    public void IProcessWindowFunction_CanBeImplemented()
    {
        IProcessWindowFunction<string, int, string> windowFunc = new TestProcessWindowFunction();
        Assert.That(windowFunc, Is.Not.Null);
    }

    [Test]
    public void IJoinFunction_CanBeImplemented()
    {
        IJoinFunction<string, int, string> joinFunc = new TestJoinFunction();
        Assert.That(joinFunc, Is.Not.Null);
    }

    [Test]
    public void IFlatJoinFunction_CanBeImplemented()
    {
        IFlatJoinFunction<string, int, string> flatJoinFunc = new TestFlatJoinFunction();
        Assert.That(flatJoinFunc, Is.Not.Null);
    }

    [Test]
    public void ICoGroupFunction_CanBeImplemented()
    {
        ICoGroupFunction<string, int, string> coGroupFunc = new TestCoGroupFunction();
        Assert.That(coGroupFunc, Is.Not.Null);
    }

    // Test implementations
    private class TestProcessFunction : IProcessFunction<string, int>
    {
        public Task ProcessElementAsync(string value, IProcessContext ctx, ICollector<int> @out)
        {
            @out.Collect(value.Length);
            return Task.CompletedTask;
        }

        public Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<int> @out)
        {
            return Task.CompletedTask;
        }
    }

    private class TestKeyedProcessFunction : IKeyedProcessFunction<string, int, int>
    {
        public Task ProcessElementAsync(int value, IKeyedProcessContext<string> ctx, ICollector<int> @out)
        {
            @out.Collect(value * 2);
            return Task.CompletedTask;
        }

        public Task OnTimerAsync(long timestamp, IKeyedOnTimerContext<string> ctx, ICollector<int> @out)
        {
            return Task.CompletedTask;
        }
    }

    private class TestCoProcessFunction : ICoProcessFunction<string, int, string>
    {
        public Task ProcessElement1Async(string value, IProcessContext ctx, ICollector<string> @out)
        {
            @out.Collect(value);
            return Task.CompletedTask;
        }

        public Task ProcessElement2Async(int value, IProcessContext ctx, ICollector<string> @out)
        {
            @out.Collect(value.ToString());
            return Task.CompletedTask;
        }

        public Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<string> @out)
        {
            return Task.CompletedTask;
        }
    }

    private class TestProcessWindowFunction : IProcessWindowFunction<string, int, string>
    {
        public Task ProcessAsync(string key, IEnumerable<string> elements, IWindowContext ctx, ICollector<int> @out)
        {
            @out.Collect(elements.Count());
            return Task.CompletedTask;
        }
    }

    private class TestJoinFunction : IJoinFunction<string, int, string>
    {
        public string Join(string first, int second)
        {
            return $"{first}-{second}";
        }
    }

    private class TestFlatJoinFunction : IFlatJoinFunction<string, int, string>
    {
        public IEnumerable<string> Join(string first, int second)
        {
            return new[] { $"{first}-{second}" };
        }
    }

    private class TestCoGroupFunction : ICoGroupFunction<string, int, string>
    {
        public IEnumerable<string> CoGroup(IEnumerable<string> first, IEnumerable<int> second)
        {
            return new[] { $"{first.Count()}-{second.Count()}" };
        }
    }

    #endregion
}
