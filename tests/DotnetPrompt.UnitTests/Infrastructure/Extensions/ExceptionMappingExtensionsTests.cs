using System.Diagnostics;
using DotnetPrompt.Infrastructure.Extensions;
using DotnetPrompt.Core;
using Microsoft.SemanticKernel;
using System.Security;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure.Extensions;

public class ExceptionMappingExtensionsTests
{
    [Fact]
    public void MapToExitCode_WithArgumentException_ReturnsInvalidArguments()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithArgumentNullException_ReturnsInvalidArguments()
    {
        // Arrange
        var exception = new ArgumentNullException("param");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.InvalidArguments, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithUnauthorizedAccessException_ReturnsAuthenticationError()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.AuthenticationError, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithSecurityException_ReturnsAuthenticationError()
    {
        // Arrange
        var exception = new SecurityException("Security violation");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.AuthenticationError, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithTimeoutException_ReturnsExecutionTimeout()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.ExecutionTimeout, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithTaskCanceledException_ReturnsExecutionTimeout()
    {
        // Arrange
        var exception = new TaskCanceledException("Task was canceled");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.ExecutionTimeout, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithFileNotFoundException_ReturnsValidationError()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.ValidationError, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithHttpRequestException_ReturnsNetworkError()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.NetworkError, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithKernelException_DelegatesToInnerException()
    {
        // Arrange
        var innerException = new UnauthorizedAccessException("Access denied");
        var kernelException = new KernelException("Kernel error", innerException);

        // Act
        var exitCode = kernelException.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.AuthenticationError, exitCode);
    }

    [Fact]
    public void MapToExitCode_WithGenericException_ReturnsGeneralError()
    {
        // Arrange
        var exception = new InvalidOperationException("Generic error");

        // Act
        var exitCode = exception.MapToExitCode();

        // Assert
        Assert.Equal(ExitCodes.GeneralError, exitCode);
    }
}