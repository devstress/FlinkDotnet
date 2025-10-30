using System;
using System.Collections.Generic;

namespace FlinkDotNet.DataStream;

/// <summary>
/// Base class for Process Table Functions (PTFs) - the most powerful UDF type in Flink Table API.
/// PTFs provide direct access to managed state, timers, event-time, and table changelogs.
/// </summary>
/// <typeparam name="TInput">Input row type</typeparam>
/// <typeparam name="TOutput">Output row type</typeparam>
public abstract class ProcessTableFunction<TInput, TOutput>
{
    /// <summary>
    /// Opens the function for processing. Override to initialize state.
    /// </summary>
    /// <param name="context">Function context providing access to state and configuration</param>
    protected virtual void Open(FunctionContext context)
    {
        // Default implementation - override to initialize state
    }

    /// <summary>
    /// Processes a single input row. This is the main processing method.
    /// </summary>
    /// <param name="context">Processing context with access to timers and output</param>
    /// <param name="input">Input row to process</param>
    public abstract void Eval(ProcessingContext context, TInput input);

    /// <summary>
    /// Called when a timer fires. Override to handle timer callbacks.
    /// </summary>
    /// <param name="context">Processing context</param>
    /// <param name="timerContext">Timer-specific context with timestamp information</param>
    public virtual void OnTimer(ProcessingContext context, OnTimerContext timerContext)
    {
        // Default implementation - override to handle timers
    }

    /// <summary>
    /// Closes the function and releases resources. Override to cleanup state.
    /// </summary>
    protected virtual void Close()
    {
        // Default implementation - override to cleanup
    }

    /// <summary>
    /// Gets the output type for this function
    /// </summary>
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static - Cannot be static as it uses generic type parameter TOutput
    protected Type OutputType => typeof(TOutput);
#pragma warning restore S2325
}

/// <summary>
/// Context for PTF processing, providing access to timers and output collection.
/// </summary>
public class ProcessingContext
{
    /// <summary>
    /// Gets or sets the current event timestamp
    /// </summary>
    public long Timestamp
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the current watermark
    /// </summary>
    public long CurrentWatermark
    {
        get; set;
    }

    private readonly List<object> _outputBuffer = [];
    private readonly List<long> _eventTimeTimers = [];
    private readonly List<long> _processingTimeTimers = [];

    /// <summary>
    /// Collects an output row to emit from the function
    /// </summary>
    /// <param name="output">Output row to emit</param>
    public void Collect(object output)
    {
        ArgumentNullException.ThrowIfNull(output);

        this._outputBuffer.Add(output);
    }

    /// <summary>
    /// Registers an event-time timer that will fire when watermark passes the specified time
    /// </summary>
    /// <param name="timestamp">Timer timestamp in milliseconds</param>
    public void RegisterEventTimeTimer(long timestamp) => this._eventTimeTimers.Add(timestamp);

    /// <summary>
    /// Registers a processing-time timer that will fire after the specified duration
    /// </summary>
    /// <param name="timestamp">Timer timestamp in milliseconds</param>
    public void RegisterProcessingTimeTimer(long timestamp) => this._processingTimeTimers.Add(timestamp);

    /// <summary>
    /// Deletes an event-time timer
    /// </summary>
    /// <param name="timestamp">Timer timestamp to delete</param>
    public void DeleteEventTimeTimer(long timestamp) => this._eventTimeTimers.Remove(timestamp);

    /// <summary>
    /// Deletes a processing-time timer
    /// </summary>
    /// <param name="timestamp">Timer timestamp to delete</param>
    public void DeleteProcessingTimeTimer(long timestamp) => this._processingTimeTimers.Remove(timestamp);

    /// <summary>
    /// Gets all collected output rows
    /// </summary>
    public IReadOnlyList<object> GetOutput() => this._outputBuffer.AsReadOnly();

    /// <summary>
    /// Gets all registered event-time timers
    /// </summary>
    public IReadOnlyList<long> GetEventTimeTimers() => this._eventTimeTimers.AsReadOnly();

    /// <summary>
    /// Gets all registered processing-time timers
    /// </summary>
    public IReadOnlyList<long> GetProcessingTimeTimers() => this._processingTimeTimers.AsReadOnly();

    /// <summary>
    /// Clears the output buffer (for testing)
    /// </summary>
    public void ClearOutput() => this._outputBuffer.Clear();
}

/// <summary>
/// Timer-specific context providing timestamp information when a timer fires
/// </summary>
public class OnTimerContext
{
    /// <summary>
    /// Gets or sets the timestamp of the timer that fired
    /// </summary>
    public long TimerTimestamp
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the type of timer (event-time or processing-time)
    /// </summary>
    public TimerType TimerType
    {
        get; set;
    }
}

/// <summary>
/// Type of timer
/// </summary>
public enum TimerType
{
    /// <summary>
    /// Event-time timer based on watermarks
    /// </summary>
    EventTime,

    /// <summary>
    /// Processing-time timer based on system clock
    /// </summary>
    ProcessingTime
}

/// <summary>
/// Context for PTF initialization, providing access to state and configuration
/// </summary>
public class FunctionContext
{
    private readonly Dictionary<string, object> _state = [];

    /// <summary>
    /// Gets or creates a value state with the specified descriptor
    /// </summary>
    /// <typeparam name="T">State value type</typeparam>
    /// <param name="descriptor">State descriptor</param>
    /// <returns>Value state instance</returns>
    public IPtfValueState<T> GetState<T>(ValueStateDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!this._state.TryGetValue(descriptor.Name, out object? state))
        {
            state = new SimpleValueState<T>();
            this._state[descriptor.Name] = state;
        }

        return (IPtfValueState<T>) state;
    }

    /// <summary>
    /// Gets or creates a list state with the specified descriptor
    /// </summary>
    /// <typeparam name="T">State element type</typeparam>
    /// <param name="descriptor">State descriptor</param>
    /// <returns>List state instance</returns>
    public IPtfListState<T> GetListState<T>(ListStateDescriptor<T> descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (!this._state.TryGetValue(descriptor.Name, out object? state))
        {
            state = new SimpleListState<T>();
            this._state[descriptor.Name] = state;
        }

        return (IPtfListState<T>) state;
    }
}

/// <summary>
/// Simple synchronous value state for PTFs
/// </summary>
/// <typeparam name="T">State value type</typeparam>
public interface IPtfValueState<T>
{
    /// <summary>
    /// Gets the current value
    /// </summary>
    public T? Value();

    /// <summary>
    /// Updates the value
    /// </summary>
    public void Update(T value);

    /// <summary>
    /// Clears the state
    /// </summary>
    public void Clear();
}

/// <summary>
/// Simple synchronous list state for PTFs
/// </summary>
/// <typeparam name="T">State element type</typeparam>
public interface IPtfListState<T>
{
    /// <summary>
    /// Gets all elements
    /// </summary>
    public IEnumerable<T> Get();

    /// <summary>
    /// Adds an element
    /// </summary>
    public void Add(T value);

    /// <summary>
    /// Clears the state
    /// </summary>
    public void Clear();
}

/// <summary>
/// Simple in-memory value state implementation for PTFs
/// </summary>
internal class SimpleValueState<T> : IPtfValueState<T>
{
    private T? _value;
    private bool _hasValue;

    public T? Value() => this._hasValue ? this._value : default;

    public void Update(T value)
    {
        this._value = value;
        this._hasValue = true;
    }

    public void Clear()
    {
        this._value = default;
        this._hasValue = false;
    }
}

/// <summary>
/// Simple in-memory list state implementation for PTFs
/// </summary>
internal class SimpleListState<T> : IPtfListState<T>
{
    private readonly List<T> _list = [];

    public IEnumerable<T> Get() => this._list;

    public void Add(T value) => this._list.Add(value);

    public void Clear() => this._list.Clear();
}
