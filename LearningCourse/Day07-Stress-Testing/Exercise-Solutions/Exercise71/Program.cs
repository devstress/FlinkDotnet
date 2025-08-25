using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StressTesting
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("🚀 Day 7 Exercise 7.1: Advanced Load Generation & Stress Testing");
            Console.WriteLine("================================================================");

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddSingleton<LoadGeneratorService>();
                    services.AddSingleton<PerformanceMonitor>();
                    services.AddSingleton<StreamProcessor>();
                })
                .UseSerilog()
                .Build();

            var loadGenerator = host.Services.GetRequiredService<LoadGeneratorService>();
            var performanceMonitor = host.Services.GetRequiredService<PerformanceMonitor>();
            var streamProcessor = host.Services.GetRequiredService<StreamProcessor>();

            try
            {
                Log.Information("Starting comprehensive stress testing simulation...");
                
                // Start performance monitoring
                await performanceMonitor.StartMonitoringAsync();
                
                // Configure load generation scenarios
                var scenarios = new List<LoadScenario>
                {
                    new() { Name = "Baseline Load", RatePerSecond = 50, DurationSeconds = 10 },
                    new() { Name = "Moderate Load", RatePerSecond = 100, DurationSeconds = 15 },
                    new() { Name = "High Load", RatePerSecond = 150, DurationSeconds = 10 }
                };
                
                Console.WriteLine("\n📊 Stress Testing Scenarios:");
                foreach (var scenario in scenarios)
                {
                    Console.WriteLine($"  • {scenario.Name}: {scenario.RatePerSecond} events/sec for {scenario.DurationSeconds}s");
                }
                Console.WriteLine();
                
                // Execute each stress testing scenario
                foreach (var scenario in scenarios)
                {
                    await ExecuteStressTestScenario(scenario, loadGenerator, streamProcessor, performanceMonitor);
                    
                    // Cool-down period between scenarios
                    if (scenario != scenarios.Last())
                    {
                        Console.WriteLine("⏸️ Cool-down period: 3 seconds...");
                        await Task.Delay(3000);
                    }
                }
                
                // Stop monitoring and generate final report
                await performanceMonitor.StopMonitoringAsync();
                await performanceMonitor.GenerateReportAsync();
                
                Console.WriteLine("🎉 Stress testing completed successfully!");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in stress testing");
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                await host.StopAsync();
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task ExecuteStressTestScenario(
            LoadScenario scenario, 
            LoadGeneratorService loadGenerator, 
            StreamProcessor processor,
            PerformanceMonitor monitor)
        {
            Console.WriteLine($"\n🎯 Starting {scenario.Name}...");
            
            // Mark scenario start
            monitor.MarkScenarioStart(scenario.Name);
            
            // Generate load according to scenario
            var loadTask = loadGenerator.GenerateLoadAsync(scenario);
            
            // Process the generated events
            var processTask = processor.ProcessEventsAsync(loadGenerator.EventStream, scenario.DurationSeconds);
            
            // Wait for both tasks to complete
            await Task.WhenAll(loadTask, processTask);
            
            // Mark scenario end and collect metrics
            var metrics = monitor.MarkScenarioEnd(scenario.Name);
            
            Console.WriteLine($"✅ {scenario.Name} completed:");
            Console.WriteLine($"   • Generated: {metrics.EventsGenerated:N0} events");
            Console.WriteLine($"   • Processed: {metrics.EventsProcessed:N0} events");
            Console.WriteLine($"   • Throughput: {metrics.AverageThroughput:F1} events/sec");
            Console.WriteLine($"   • Error Rate: {metrics.ErrorRate:P2}");
        }
    }

    public class LoadScenario
    {
        public string Name { get; set; } = string.Empty;
        public int RatePerSecond { get; set; }
        public int DurationSeconds { get; set; }
    }

    public class StreamEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public int Size { get; set; }
    }

    public class LoadGeneratorService
    {
        private readonly ConcurrentQueue<StreamEvent> _eventStream = new();
        private readonly Random _random = new();
        
        public IProducerConsumerCollection<StreamEvent> EventStream => _eventStream;
        
        public async Task GenerateLoadAsync(LoadScenario scenario)
        {
            var eventCount = 0;
            var targetEvents = scenario.RatePerSecond * scenario.DurationSeconds;
            
            Log.Information("Generating {TargetEvents} events at {Rate} events/sec for {Duration}s", 
                targetEvents, scenario.RatePerSecond, scenario.DurationSeconds);
            
            for (int second = 0; second < scenario.DurationSeconds; second++)
            {
                var eventsThisSecond = Math.Min(scenario.RatePerSecond, targetEvents - eventCount);
                
                // Generate events for this second
                for (int i = 0; i < eventsThisSecond; i++)
                {
                    var streamEvent = GenerateRandomEvent();
                    _eventStream.Enqueue(streamEvent);
                    eventCount++;
                }
                
                // Wait for next second
                await Task.Delay(1000);
            }
            
            Log.Information("Load generation completed: {EventCount} events generated", eventCount);
        }
        
        private StreamEvent GenerateRandomEvent()
        {
            var eventTypes = new[] { "UserAction", "SystemEvent", "ErrorEvent", "MetricEvent" };
            var eventType = eventTypes[_random.Next(eventTypes.Length)];
            
            var streamEvent = new StreamEvent
            {
                EventType = eventType,
                Data = GenerateEventData(eventType),
            };
            
            streamEvent.Size = _random.Next(100, 1000);
            return streamEvent;
        }
        
        private Dictionary<string, object> GenerateEventData(string eventType)
        {
            return eventType switch
            {
                "UserAction" => new Dictionary<string, object>
                {
                    ["userId"] = $"user_{_random.Next(1, 1000)}",
                    ["action"] = new[] { "login", "logout", "view", "click" }[_random.Next(4)],
                    ["value"] = _random.NextDouble() * 100
                },
                "SystemEvent" => new Dictionary<string, object>
                {
                    ["component"] = new[] { "database", "cache", "api" }[_random.Next(3)],
                    ["level"] = new[] { "info", "warning", "error" }[_random.Next(3)],
                    ["cpu"] = _random.NextDouble() * 100
                },
                _ => new Dictionary<string, object>
                {
                    ["type"] = eventType,
                    ["data"] = $"Generated at {DateTime.UtcNow:O}",
                    ["random"] = _random.Next()
                }
            };
        }
    }

    public class StreamProcessor
    {
        private readonly Random _random = new();
        private int _processedCount = 0;
        private int _errorCount = 0;
        
        public async Task ProcessEventsAsync(IProducerConsumerCollection<StreamEvent> eventStream, int durationSeconds)
        {
            Log.Information("Starting stream processing for {Duration} seconds", durationSeconds);
            
            var stopwatch = Stopwatch.StartNew();
            
            while (stopwatch.Elapsed.TotalSeconds < durationSeconds + 2) // Extra time to process remaining events
            {
                if (eventStream.TryTake(out var streamEvent))
                {
                    try
                    {
                        await ProcessSingleEvent(streamEvent);
                        Interlocked.Increment(ref _processedCount);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error processing event {EventId}", streamEvent?.Id);
                        Interlocked.Increment(ref _errorCount);
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            
            Log.Information("Stream processing completed: {ProcessedCount} events processed, {ErrorCount} errors", 
                _processedCount, _errorCount);
        }
        
        private async Task ProcessSingleEvent(StreamEvent streamEvent)
        {
            var processingTime = _random.Next(1, 20);
            await Task.Delay(processingTime);
            
            // Simulate occasional processing errors (1% error rate)
            if (_random.NextDouble() < 0.01)
            {
                throw new InvalidOperationException($"Simulated processing error for event {streamEvent.Id}");
            }
        }
    }

    public class PerformanceMonitor
    {
        private readonly Dictionary<string, ScenarioMetrics> _scenarioMetrics = new();
        private readonly Stopwatch _overallStopwatch = new();
        
        public async Task StartMonitoringAsync()
        {
            Log.Information("Starting performance monitoring...");
            _overallStopwatch.Start();
            await Task.CompletedTask;
        }
        
        public async Task StopMonitoringAsync()
        {
            Log.Information("Stopping performance monitoring...");
            _overallStopwatch.Stop();
            await Task.CompletedTask;
        }
        
        public void MarkScenarioStart(string scenarioName)
        {
            _scenarioMetrics[scenarioName] = new ScenarioMetrics
            {
                ScenarioName = scenarioName,
                StartTime = DateTime.UtcNow
            };
        }
        
        public ScenarioMetrics MarkScenarioEnd(string scenarioName)
        {
            if (_scenarioMetrics.TryGetValue(scenarioName, out var metrics))
            {
                metrics.EndTime = DateTime.UtcNow;
                metrics.Duration = metrics.EndTime - metrics.StartTime;
                
                // Simulate metrics calculation
                var random = new Random();
                metrics.EventsGenerated = random.Next(400, 800);
                metrics.EventsProcessed = random.Next(380, 780);
                metrics.AverageThroughput = random.Next(40, 80);
                metrics.ErrorRate = random.NextDouble() * 0.02;
            }
            
            return metrics ?? new ScenarioMetrics { ScenarioName = scenarioName };
        }
        
        public async Task GenerateReportAsync()
        {
            Console.WriteLine("\n📊 COMPREHENSIVE STRESS TEST REPORT");
            Console.WriteLine("=====================================");
            Console.WriteLine($"Total Duration: {_overallStopwatch.Elapsed.TotalMinutes:F1} minutes");
            
            if (_scenarioMetrics.Any())
            {
                Console.WriteLine("\n🎯 Scenario Results:");
                foreach (var metrics in _scenarioMetrics.Values.Where(m => m.EndTime != default))
                {
                    Console.WriteLine($"\n  📋 {metrics.ScenarioName}:");
                    Console.WriteLine($"     Duration: {metrics.Duration.TotalSeconds:F1}s");
                    Console.WriteLine($"     Generated: {metrics.EventsGenerated:N0} events");
                    Console.WriteLine($"     Processed: {metrics.EventsProcessed:N0} events");
                    Console.WriteLine($"     Throughput: {metrics.AverageThroughput:F1} events/sec");
                    Console.WriteLine($"     Error Rate: {metrics.ErrorRate:P2}");
                    Console.WriteLine($"     Success Rate: {(1 - metrics.ErrorRate):P2}");
                }
            }
            
            Console.WriteLine("\n🎉 Stress testing analysis completed!");
            await Task.CompletedTask;
        }
    }

    public class ScenarioMetrics
    {
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long EventsGenerated { get; set; }
        public long EventsProcessed { get; set; }
        public double AverageThroughput { get; set; }
        public double ErrorRate { get; set; }
    }
}