using DotnetPrompt.Core;
using Microsoft.SemanticKernel;
using System.Net;
using System.Security;

namespace DotnetPrompt.Infrastructure.Extensions;

/// <summary>
/// Utility for mapping SK exceptions to appropriate exit codes
/// </summary>
public static class ExceptionMappingExtensions
{
    /// <summary>
    /// Maps SK and related exceptions to appropriate exit codes using existing DotnetPrompt.Core.ExitCodes
    /// </summary>
    /// <param name="exception">The exception to map</param>
    /// <returns>The appropriate exit code</returns>
    public static int MapToExitCode(this Exception exception)
    {
        return exception switch
        {
            // SK-specific exceptions
            KernelException kernelEx => MapKernelException(kernelEx),
            
            // Authentication and authorization errors (moved before SecurityException)
            UnauthorizedAccessException => ExitCodes.AuthenticationError,
            SecurityException => ExitCodes.AuthenticationError,
            
            // Network-related errors
            HttpRequestException httpEx => MapHttpException(httpEx),
            TaskCanceledException => ExitCodes.ExecutionTimeout,
            TimeoutException => ExitCodes.ExecutionTimeout,
            
            // Configuration errors
            InvalidOperationException invalidOp when invalidOp.Message.Contains("configuration") => ExitCodes.ConfigurationError,
            
            // Validation errors (specific first, then general)
            ArgumentNullException => ExitCodes.InvalidArguments,
            ArgumentOutOfRangeException => ExitCodes.InvalidArguments,
            ArgumentException => ExitCodes.InvalidArguments,
            
            // File system errors
            FileNotFoundException => ExitCodes.ValidationError,
            DirectoryNotFoundException => ExitCodes.ValidationError,
            IOException when exception.Message.Contains("access") => ExitCodes.PermissionError,
            
            // General errors
            _ => ExitCodes.GeneralError
        };
    }

    private static int MapKernelException(KernelException kernelException)
    {
        return kernelException.InnerException switch
        {
            HttpRequestException httpEx => MapHttpException(httpEx),
            UnauthorizedAccessException => ExitCodes.AuthenticationError,
            TaskCanceledException => ExitCodes.ExecutionTimeout,
            TimeoutException => ExitCodes.ExecutionTimeout,
            ArgumentException => ExitCodes.WorkflowValidationError,
            InvalidOperationException => ExitCodes.ConfigurationError,
            _ => ExitCodes.GeneralError
        };
    }

    private static int MapHttpException(HttpRequestException httpException)
    {
        // Try to extract status code from message or data
        var message = httpException.Message.ToLowerInvariant();
        
        return message switch
        {
            var m when m.Contains("401") || m.Contains("unauthorized") => ExitCodes.AuthenticationError,
            var m when m.Contains("403") || m.Contains("forbidden") => ExitCodes.PermissionError,
            var m when m.Contains("404") || m.Contains("not found") => ExitCodes.ValidationError,
            var m when m.Contains("429") || m.Contains("rate limit") => ExitCodes.NetworkError,
            var m when m.Contains("timeout") => ExitCodes.ExecutionTimeout,
            _ => ExitCodes.NetworkError
        };
    }
}