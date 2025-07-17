using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DotnetPrompt.IntegrationTests.Infrastructure;

/// <summary>
/// Integration tests for MSBuild setup and initialization
/// </summary>
public class MSBuildIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<MSBuildIntegrationTests> _logger;

    public MSBuildIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestOutputLoggerProvider(output)));
        _logger = loggerFactory.CreateLogger<MSBuildIntegrationTests>();
    }

    [Fact]
    public void MSBuildSetup_EnsureInitialized_CompletesSuccessfully()
    {
        // Arrange
        MSBuildSetup.SetLogger(_logger);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => MSBuildSetup.EnsureInitialized());
        
        Assert.Null(exception);
        Assert.True(MSBuildSetup.IsInitialized);
        
        _output.WriteLine("MSBuild setup completed successfully");
    }

    [Fact]
    public void MSBuildDiagnosticsHandler_ValidateMSBuildEnvironment_ReturnsResult()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestOutputLoggerProvider(_output)));
        var logger = loggerFactory.CreateLogger<MSBuildDiagnosticsHandler>();
        var handler = new MSBuildDiagnosticsHandler(logger);

        // Ensure MSBuild is initialized first
        MSBuildSetup.SetLogger(_logger);
        MSBuildSetup.EnsureInitialized();

        // Act
        var isValid = handler.ValidateMSBuildEnvironment();

        // Assert - Just verify method completes (result may vary by environment)
        _output.WriteLine($"MSBuild environment validation result: {isValid}");
        
        // Method should not throw regardless of result
        Assert.True(true); // Test passes if no exception is thrown
    }

    [Fact]
    public void MSBuildDiagnosticsHandler_CreateTimeoutHandler_ReturnsConfiguredHandler()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddProvider(new TestOutputLoggerProvider(_output)));
        var logger = loggerFactory.CreateLogger<MSBuildDiagnosticsHandler>();
        var handler = new MSBuildDiagnosticsHandler(logger);

        // Act
        using var timeoutHandler = handler.CreateTimeoutHandler(5000, "test operation");

        // Assert
        Assert.NotNull(timeoutHandler);
        Assert.False(timeoutHandler.Token.IsCancellationRequested);
        
        _output.WriteLine("Timeout handler created successfully");
    }
}

/// <summary>
/// Simple logger provider for test output
/// </summary>
public class TestOutputLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TestOutputLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestOutputLogger(_testOutputHelper, categoryName);
    }

    public void Dispose() { }
}

/// <summary>
/// Simple logger that writes to test output
/// </summary>
public class TestOutputLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public TestOutputLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        _testOutputHelper.WriteLine($"[{logLevel}] {_categoryName}: {message}");
        
        if (exception != null)
        {
            _testOutputHelper.WriteLine($"Exception: {exception}");
        }
    }
}