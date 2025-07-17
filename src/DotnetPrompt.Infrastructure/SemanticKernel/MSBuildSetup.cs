using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Static class for thread-safe MSBuild Locator initialization
/// CRITICAL: Must be called before any MSBuildWorkspace creation
/// </summary>
public static class MSBuildSetup
{
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new();
    private static ILogger? _logger;
    
    /// <summary>
    /// Sets the logger for MSBuild setup operations
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ensures MSBuild Locator is initialized exactly once in a thread-safe manner.
    /// This method MUST be called before creating any MSBuildWorkspace instances.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when MSBuild initialization fails</exception>
    public static void EnsureInitialized()
    {
        if (_isInitialized) return;
        
        lock (_lockObject)
        {
            if (_isInitialized) return;
            
            try
            {
                _logger?.LogDebug("Starting MSBuild Locator initialization");
                
                // Check if MSBuild is already registered by another component
                if (MSBuildLocator.IsRegistered)
                {
                    _logger?.LogDebug("MSBuild Locator already registered by another component");
                    _isInitialized = true;
                    return;
                }
                
                // Query available MSBuild instances
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                _logger?.LogDebug("Found {InstanceCount} MSBuild instances", instances.Count);
                
                if (instances.Any())
                {
                    // Use the highest version MSBuild instance
                    var defaultInstance = instances.OrderByDescending(x => x.Version).First();
                    _logger?.LogInformation("Registering MSBuild instance: {Name} v{Version} at {Path}", 
                        defaultInstance.Name, defaultInstance.Version, defaultInstance.MSBuildPath);
                    
                    MSBuildLocator.RegisterInstance(defaultInstance);
                }
                else
                {
                    _logger?.LogInformation("No specific MSBuild instances found, using RegisterDefaults()");
                    MSBuildLocator.RegisterDefaults();
                }
                
                _isInitialized = true;
                _logger?.LogInformation("MSBuild Locator initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize MSBuild Locator");
                throw new InvalidOperationException(
                    "Failed to initialize MSBuild. Ensure .NET SDK is installed and accessible. " +
                    "This typically indicates that the .NET SDK is not properly installed or the " +
                    "MSBuild tools are not available in the expected locations. " +
                    "Try running 'dotnet --list-sdks' to verify SDK installation.", ex);
            }
        }
    }

    /// <summary>
    /// Gets the initialization status without attempting to initialize
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Forces re-initialization (primarily for testing scenarios)
    /// WARNING: Use with caution as MSBuildLocator can only be registered once per process
    /// </summary>
    internal static void Reset()
    {
        lock (_lockObject)
        {
            _isInitialized = false;
        }
    }
}