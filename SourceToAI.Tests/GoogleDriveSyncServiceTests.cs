using Moq;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Services.Integration;
using System.Threading.Tasks;
using Xunit;

namespace SourceToAI.Tests;

public class GoogleDriveSyncServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEnabledIsFalse_ShouldNotCallGoogleDriveClient()
    {
        // Arrange
        var settings = new GoogleDriveSyncSettings { Enabled = false, TargetFolder = "Code" };
        var mockClient = new Mock<IGoogleDriveClient>();
        var service = new GoogleDriveSyncService(settings, mockClient.Object);

        // Act
        await service.ExecuteAsync("TestSolution", "C:\\test\\output");

        // Assert
        mockClient.Verify(c => c.ReplaceSolutionFolderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabledIsTrue_ShouldCallGoogleDriveClient()
    {
        // Arrange
        var settings = new GoogleDriveSyncSettings { Enabled = true, TargetFolder = "TestFolder" };
        var mockClient = new Mock<IGoogleDriveClient>();
        mockClient.Setup(c => c.ReplaceSolutionFolderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync("dummy_folder_id");

        var service = new GoogleDriveSyncService(settings, mockClient.Object);

        var solutionName = "TestSolution";
        var outputDirectory = "C:\\test\\output";

        // Act
        await service.ExecuteAsync(solutionName, outputDirectory);

        // Assert
        mockClient.Verify(c => c.ReplaceSolutionFolderAsync(settings.TargetFolder, solutionName, outputDirectory), Times.Once);
    }
}