using DotnetPrompt.Core.Models;
using DotnetPrompt.Infrastructure.Mcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using FluentAssertions;

namespace DotnetPrompt.UnitTests.Infrastructure.Mcp;

/// <summary>
/// Unit tests for MCP configuration service
/// </summary>
public class McpConfigurationServiceTests
{
    private readonly McpConfigurationService _service;

    public McpConfigurationServiceTests()
    {
        var detector = new McpConnectionTypeDetector();
        _service = new McpConfigurationService(detector, NullLogger<McpConfigurationService>.Instance);
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_NoMcpConfig_ReturnsEmpty()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions()
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_EmptyMcpConfig_ReturnsEmpty()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>()
            }
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_ValidLocalServer_ProcessesCorrectly()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new McpServerConfig
                    {
                        Server = "filesystem-mcp",
                        Version = "1.0.0",
                        Config = new Dictionary<string, object> { ["root_path"] = "./test" }
                    }
                }
            }
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert
        result.Should().HaveCount(1);
        var config = result.First();
        config.Server.Should().Be("filesystem-mcp");
        config.Version.Should().Be("1.0.0");
        config.ConnectionType.Should().Be(McpConnectionType.Stdio);
        config.Config.Should().ContainKey("root_path");
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_ValidRemoteServer_ProcessesCorrectly()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new McpServerConfig
                    {
                        Server = "cloud-analytics",
                        Version = "2.0.0",
                        Endpoint = "https://api.example.com/mcp",
                        AuthToken = "test-token",
                        Config = new Dictionary<string, object> { ["api_version"] = "v2" }
                    }
                }
            }
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert
        result.Should().HaveCount(1);
        var config = result.First();
        config.Server.Should().Be("cloud-analytics");
        config.ConnectionType.Should().Be(McpConnectionType.Sse);
        config.Endpoint.Should().Be("https://api.example.com/mcp");
        config.AuthToken.Should().Be("test-token");
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_EnvironmentVariableResolution_ResolvesCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_MCP_TOKEN", "resolved-token");
        
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new McpServerConfig
                    {
                        Server = "test-server",
                        ConnectionType = McpConnectionType.Stdio, // Explicit stdio to avoid auto-detection issues
                        AuthToken = "${TEST_MCP_TOKEN}",
                        Config = new Dictionary<string, object> { ["key"] = "$TEST_MCP_TOKEN" }
                    }
                }
            }
        };

        try
        {
            // Act
            var result = await _service.ParseMcpConfigurationAsync(workflow);

            // Assert
            result.Should().HaveCount(1);
            var config = result.First();
            config.AuthToken.Should().Be("resolved-token");
            config.Config["key"].Should().Be("resolved-token");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_MCP_TOKEN", null);
        }
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_InvalidConfiguration_ContinuesWithOthers()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new McpServerConfig(), // Invalid - no server name
                    new McpServerConfig
                    {
                        Server = "valid-server",
                        Version = "1.0.0"
                    }
                }
            }
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert - Should continue processing valid configurations
        result.Should().HaveCount(1);
        result.First().Server.Should().Be("valid-server");
    }

    [Fact]
    public async Task ParseMcpConfigurationAsync_MixedLocalAndRemote_ProcessesBoth()
    {
        // Arrange
        var workflow = new DotpromptWorkflow
        {
            Extensions = new DotpromptExtensions
            {
                Mcp = new List<McpServerConfig>
                {
                    new McpServerConfig
                    {
                        Server = "@modelcontextprotocol/server-filesystem",
                        Version = "1.0.0"
                    },
                    new McpServerConfig
                    {
                        Server = "analytics-api",
                        Endpoint = "https://mcp.company.com/v1",
                        AuthToken = "bearer-token"
                    }
                }
            }
        };

        // Act
        var result = await _service.ParseMcpConfigurationAsync(workflow);

        // Assert
        result.Should().HaveCount(2);
        
        var localServer = result.First(c => c.ConnectionType == McpConnectionType.Stdio);
        localServer.Server.Should().Be("@modelcontextprotocol/server-filesystem");
        
        var remoteServer = result.First(c => c.ConnectionType == McpConnectionType.Sse);
        remoteServer.Server.Should().Be("analytics-api");
        remoteServer.Endpoint.Should().Be("https://mcp.company.com/v1");
    }
}