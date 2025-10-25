using FlinkDotNet.Common.Logging;
using System.IO.Abstractions.TestingHelpers;
using Moq;
using System.IO.Abstractions;

namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Branch coverage tests for LoggerFactory to achieve 100% branch coverage
/// </summary>
[TestFixture]
public class LoggerFactoryBranchCoverageTests
{
    [Test]
    public void CreateLogger_WithNullEnvironmentVariable_UsesDefaultPath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("LOG_FILE_PATH", null);
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("test-logs");

        // Act
        var logger = LoggerFactory.CreateLogger(mockFileSystem);

        // Assert
        Assert.That(logger, Is.Not.Null);
    }

    [Test]
    public void CleanupOldLogFiles_WithOldFile_DeletesFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("test-logs");
        
        // Create an old log file (older than 1 day) with proper date
        var oldDate = DateTime.UtcNow.AddDays(-2);
        var oldLogPath = mockFileSystem.Path.Combine("test-logs", "FlinkDotnet.log.20200101");
        mockFileSystem.AddFile(oldLogPath, new MockFileData("old log content")
        {
            LastWriteTime = oldDate
        });

        // Act
        var logger = LoggerFactory.CreateLogger(mockFileSystem);

        // Assert
        Assert.That(logger, Is.Not.Null);
        // File should be deleted because it's old
        Assert.That(mockFileSystem.File.Exists(oldLogPath), Is.False);
    }

    [Test]
    public void CleanupOldLogFiles_WithRecentFile_KeepsFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.Directory.CreateDirectory("test-logs");
        
        // Create a recent log file (less than 1 day old)
        var recentDate = DateTime.UtcNow.AddHours(-12);
        var recentLogPath = mockFileSystem.Path.Combine("test-logs", "FlinkDotnet.log.20991231");
        mockFileSystem.AddFile(recentLogPath, new MockFileData("recent log content")
        {
            LastWriteTime = recentDate
        });

        // Act
        var logger = LoggerFactory.CreateLogger(mockFileSystem);

        // Assert
        Assert.That(logger, Is.Not.Null);
        // File should still exist because it's recent
        Assert.That(mockFileSystem.File.Exists(recentLogPath), Is.True);
    }

    [Test]
    public void CleanupOldLogFiles_WhenDirectoryDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        // Don't create the directory

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => LoggerFactory.CreateLogger(mockFileSystem));
    }

    [Test]
    public void CleanupOldLogFiles_WhenGetFilesThrows_DoesNotThrow()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockDirectory = new Mock<IDirectory>();
        var mockPath = new Mock<IPath>();
        var mockFile = new Mock<IFile>();

        mockPath.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string path1, string path2) => $"{path1}/{path2}");

        mockDirectory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        mockDirectory.Setup(d => d.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        mockFileSystem.Setup(fs => fs.Directory).Returns(mockDirectory.Object);
        mockFileSystem.Setup(fs => fs.Path).Returns(mockPath.Object);
        mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);

        // Act & Assert - Should not throw despite GetFiles throwing
        Assert.DoesNotThrow(() => LoggerFactory.CreateLogger(mockFileSystem.Object));
    }

    [Test]
    public void CleanupOldLogFiles_WhenFileInfoThrows_DoesNotThrow()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockDirectory = new Mock<IDirectory>();
        var mockPath = new Mock<IPath>();
        var mockFile = new Mock<IFile>();
        var mockFileInfoFactory = new Mock<IFileInfoFactory>();

        mockPath.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string path1, string path2) => $"{path1}/{path2}");

        mockDirectory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        mockDirectory.Setup(d => d.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new[] { "test-logs/old.log" });

        mockFileInfoFactory.Setup(f => f.New(It.IsAny<string>()))
            .Throws(new System.IO.IOException("File info error"));

        mockFileSystem.Setup(fs => fs.Directory).Returns(mockDirectory.Object);
        mockFileSystem.Setup(fs => fs.Path).Returns(mockPath.Object);
        mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);
        mockFileSystem.Setup(fs => fs.FileInfo).Returns(mockFileInfoFactory.Object);

        // Act & Assert - Should not throw despite FileInfo throwing
        Assert.DoesNotThrow(() => LoggerFactory.CreateLogger(mockFileSystem.Object));
    }

    [Test]
    public void CleanupOldLogFiles_WhenDeleteThrows_DoesNotThrow()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockDirectory = new Mock<IDirectory>();
        var mockPath = new Mock<IPath>();
        var mockFile = new Mock<IFile>();
        var mockFileInfoFactory = new Mock<IFileInfoFactory>();
        var mockFileInfo = new Mock<IFileInfo>();

        mockPath.Setup(p => p.Combine(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string path1, string path2) => $"{path1}/{path2}");

        mockDirectory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        mockDirectory.Setup(d => d.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new[] { "test-logs/old.log" });

        mockFileInfo.Setup(f => f.LastWriteTimeUtc).Returns(DateTime.UtcNow.AddDays(-2));
        mockFileInfoFactory.Setup(f => f.New(It.IsAny<string>()))
            .Returns(mockFileInfo.Object);

        mockFile.Setup(f => f.Delete(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Cannot delete"));

        mockFileSystem.Setup(fs => fs.Directory).Returns(mockDirectory.Object);
        mockFileSystem.Setup(fs => fs.Path).Returns(mockPath.Object);
        mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);
        mockFileSystem.Setup(fs => fs.FileInfo).Returns(mockFileInfoFactory.Object);

        // Act & Assert - Should not throw despite Delete throwing
        Assert.DoesNotThrow(() => LoggerFactory.CreateLogger(mockFileSystem.Object));
    }
}
