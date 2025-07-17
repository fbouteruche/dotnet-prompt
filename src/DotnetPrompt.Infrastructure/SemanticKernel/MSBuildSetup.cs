using Microsoft.Build.Locator;

namespace DotnetPrompt.Infrastructure.SemanticKernel;

/// <summary>
/// Static class for thread-safe MSBuild Locator initialization
/// CRITICAL: Must be called before any MSBuildWorkspace creation
/// </summary>
public static class MSBuildSetup
{
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new();

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
                // Check if MSBuild is already registered by another component
                if (MSBuildLocator.IsRegistered)
                {
                    _isInitialized = true;
                    return;
                }
                
                // Register the default MSBuild installation
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Any())
                {
                    var defaultInstance = instances.OrderByDescending(x => x.Version).First();
                    MSBuildLocator.RegisterInstance(defaultInstance);
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                }
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to initialize MSBuild. Ensure .NET SDK is installed and accessible. " +
                    "This typically indicates that the .NET SDK is not properly installed or the " +
                    "MSBuild tools are not available in the expected locations.", ex);
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