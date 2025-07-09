using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Mcp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Infrastructure.Mcp;

/// <summary>
/// Unit tests for MCP connection type detection
/// </summary>
public class McpConnectionTypeDetectorTests
{
    private readonly McpConnectionTypeDetector _detector = new();

    [Fact]
    public void DetermineConnectionType_ExplicitStdio_ReturnsStdio()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Stdio,
            Server = "test-mcp"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Stdio);
    }

    [Fact]
    public void DetermineConnectionType_ExplicitSse_ReturnsSse()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Sse,
            Server = "test-mcp"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Sse);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithEndpoint_ReturnsSse()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "test-mcp",
            Endpoint = "https://api.example.com/mcp"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Sse);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithHttpsUrl_ReturnsSse()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "https://mcp.example.com/v1"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Sse);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithAuthToken_ReturnsSse()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "test-mcp",
            AuthToken = "bearer-token"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Sse);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithTimeout_ReturnsSse()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "test-mcp",
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Sse);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithLocalPackage_ReturnsStdio()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "@modelcontextprotocol/server-filesystem"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Stdio);
    }

    [Fact]
    public void DetermineConnectionType_AutoWithMcpSuffix_ReturnsStdio()
    {
        // Arrange
        var config = new McpServerConfig
        {
            ConnectionType = McpConnectionType.Auto,
            Server = "filesystem-mcp"
        };

        // Act
        var result = _detector.DetermineConnectionType(config);

        // Assert
        result.Should().Be(McpConnectionType.Stdio);
    }
}