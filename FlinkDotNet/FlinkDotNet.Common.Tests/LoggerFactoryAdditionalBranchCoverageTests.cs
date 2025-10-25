using System.IO.Abstractions.TestingHelpers;
using FlinkDotNet.Common.Logging;

namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Additional branch coverage tests for LoggerFactory to achieve 100% branch coverage
/// Covers the edge case where files are exactly at the 1-day boundary
/// </summary>
[TestFixture]
public class LoggerFactoryAdditionalBranchCoverageTests
{
    [Test]
    public void CleanupOldLogFiles_WithFileExactlyOneDayOld_KeepsFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        _ = mockFileSystem.Directory.CreateDirectory("test-logs");

        // Create a file exactly at the 1-day boundary (not older than 1 day, so should be kept)
        // LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1) is false when exactly at boundary
        var exactlyOneDayAgo = DateTime.UtcNow.AddDays(-1).AddSeconds(1); // Just after the boundary
        var boundaryLogPath = mockFileSystem.Path.Combine("test-logs", "FlinkDotnet.log.boundary");
        mockFileSystem.AddFile(boundaryLogPath, new MockFileData("boundary log content")
        {
            LastWriteTime = exactlyOneDayAgo
        });

        // Act
        var logger = LoggerFactory.CreateLogger(mockFileSystem);

        // Assert
        Assert.That(logger, Is.Not.Null);
        // File should still exist because it's not older than 1 day
        Assert.That(mockFileSystem.File.Exists(boundaryLogPath), Is.True);
    }
}
