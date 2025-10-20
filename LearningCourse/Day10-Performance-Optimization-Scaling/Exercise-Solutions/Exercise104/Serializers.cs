using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using MessagePack;

namespace Exercise104;

/// <summary>
/// Serialization performance tester for different formats
/// </summary>
public class SerializationTester
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Test JSON serialization performance
    /// </summary>
    public static (byte[] Data, double SerializeMs, double DeserializeMs) TestJson(ThroughputEvent evt)
    {
        var stopwatch = Stopwatch.StartNew();
        var json = JsonSerializer.Serialize(evt, JsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        stopwatch.Stop();
        var serializeMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Restart();
        _ = JsonSerializer.Deserialize<ThroughputEvent>(json, JsonOptions);
        stopwatch.Stop();
        var deserializeMs = stopwatch.Elapsed.TotalMilliseconds;

        return (data, serializeMs, deserializeMs);
    }

    /// <summary>
    /// Test Binary serialization performance
    /// </summary>
    public static (byte[] Data, double SerializeMs, double DeserializeMs) TestBinary(ThroughputEvent evt)
    {
        var stopwatch = Stopwatch.StartNew();
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // Manual binary serialization for performance
        writer.Write(evt.Id);
        writer.Write(evt.Timestamp);
        writer.Write(evt.UserId);
        writer.Write(evt.EventType);
        writer.Write(evt.Value);
        writer.Write(evt.Metadata.Count);
        foreach (var kvp in evt.Metadata)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }
        
        var data = ms.ToArray();
        stopwatch.Stop();
        var serializeMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Restart();
        using var readMs = new MemoryStream(data);
        using var reader = new BinaryReader(readMs);
        
        var deserialized = new ThroughputEvent
        {
            Id = reader.ReadString(),
            Timestamp = reader.ReadInt64(),
            UserId = reader.ReadString(),
            EventType = reader.ReadString(),
            Value = reader.ReadDouble()
        };
        
        var metadataCount = reader.ReadInt32();
        for (int i = 0; i < metadataCount; i++)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            deserialized.Metadata[key] = value;
        }
        
        stopwatch.Stop();
        var deserializeMs = stopwatch.Elapsed.TotalMilliseconds;

        return (data, serializeMs, deserializeMs);
    }

    /// <summary>
    /// Test MessagePack serialization performance
    /// </summary>
    public static (byte[] Data, double SerializeMs, double DeserializeMs) TestMessagePack(ThroughputEvent evt)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = MessagePackSerializer.Serialize(evt);
        stopwatch.Stop();
        var serializeMs = stopwatch.Elapsed.TotalMilliseconds;

        stopwatch.Restart();
        _ = MessagePackSerializer.Deserialize<ThroughputEvent>(data);
        stopwatch.Stop();
        var deserializeMs = stopwatch.Elapsed.TotalMilliseconds;

        return (data, serializeMs, deserializeMs);
    }

    /// <summary>
    /// Test serialization with compression
    /// </summary>
    public static (byte[] CompressedData, double CompressionRatio, double CompressMs) TestCompression(
        byte[] data, CompressionType compressionType)
    {
        if (compressionType == CompressionType.None)
        {
            return (data, 1.0, 0);
        }

        var stopwatch = Stopwatch.StartNew();
        using var outputMs = new MemoryStream();
        
        if (compressionType == CompressionType.GZip)
        {
            using (var gzipStream = new GZipStream(outputMs, CompressionLevel.Fastest))
            {
                gzipStream.Write(data, 0, data.Length);
            }
        }
        
        var compressedData = outputMs.ToArray();
        stopwatch.Stop();
        
        var ratio = data.Length > 0 ? (double)data.Length / compressedData.Length : 1.0;
        
        return (compressedData, ratio, stopwatch.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Decompress data
    /// </summary>
    public static byte[] Decompress(byte[] compressedData, CompressionType compressionType)
    {
        if (compressionType == CompressionType.None)
        {
            return compressedData;
        }

        using var inputMs = new MemoryStream(compressedData);
        using var outputMs = new MemoryStream();
        
        if (compressionType == CompressionType.GZip)
        {
            using var gzipStream = new GZipStream(inputMs, CompressionMode.Decompress);
            gzipStream.CopyTo(outputMs);
        }
        
        return outputMs.ToArray();
    }

    /// <summary>
    /// Batch serialize events
    /// </summary>
    public static byte[] BatchSerialize(List<ThroughputEvent> events, SerializationFormat format)
    {
        return format switch
        {
            SerializationFormat.Json => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(events, JsonOptions)),
            SerializationFormat.MessagePack => MessagePackSerializer.Serialize(events),
            SerializationFormat.Binary => SerializeBinaryBatch(events),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    /// <summary>
    /// Batch deserialize events
    /// </summary>
    public static List<ThroughputEvent> BatchDeserialize(byte[] data, SerializationFormat format)
    {
        return format switch
        {
            SerializationFormat.Json => JsonSerializer.Deserialize<List<ThroughputEvent>>(
                Encoding.UTF8.GetString(data), JsonOptions) ?? new List<ThroughputEvent>(),
            SerializationFormat.MessagePack => MessagePackSerializer.Deserialize<List<ThroughputEvent>>(data),
            SerializationFormat.Binary => DeserializeBinaryBatch(data),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private static byte[] SerializeBinaryBatch(List<ThroughputEvent> events)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write(events.Count);
        foreach (var evt in events)
        {
            writer.Write(evt.Id);
            writer.Write(evt.Timestamp);
            writer.Write(evt.UserId);
            writer.Write(evt.EventType);
            writer.Write(evt.Value);
            writer.Write(evt.Metadata.Count);
            foreach (var kvp in evt.Metadata)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }
        
        return ms.ToArray();
    }

    private static List<ThroughputEvent> DeserializeBinaryBatch(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        
        var count = reader.ReadInt32();
        var events = new List<ThroughputEvent>(count);
        
        for (int i = 0; i < count; i++)
        {
            var evt = new ThroughputEvent
            {
                Id = reader.ReadString(),
                Timestamp = reader.ReadInt64(),
                UserId = reader.ReadString(),
                EventType = reader.ReadString(),
                Value = reader.ReadDouble()
            };
            
            var metadataCount = reader.ReadInt32();
            for (int j = 0; j < metadataCount; j++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                evt.Metadata[key] = value;
            }
            
            events.Add(evt);
        }
        
        return events;
    }
}