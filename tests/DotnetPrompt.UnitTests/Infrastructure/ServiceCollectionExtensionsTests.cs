using DotnetPrompt.Core.Interfaces;
using DotnetPrompt.Infrastructure;
using DotnetPrompt.Infrastructure.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Xunit;

namespace DotnetPrompt.UnitTests.Infrastructure;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSemanticKernelOrchestrator_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSemanticKernelOrchestrator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify HandlebarsPromptTemplateFactory is registered
        Assert.NotNull(serviceProvider.GetService<IPromptTemplateFactory>());
        
        // Verify orchestrator is registered
        Assert.NotNull(serviceProvider.GetService<IWorkflowOrchestrator>());
        
        // Verify kernel factory is registered
        Assert.NotNull(serviceProvider.GetService<IKernelFactory>());
    }

    [Fact]
    public void AddSemanticKernelOrchestrator_RegistersHandlebarsFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSemanticKernelOrchestrator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IPromptTemplateFactory>();
        
        Assert.NotNull(factory);
        Assert.IsType<HandlebarsPromptTemplateFactory>(factory);
    }

    [Fact]
    public void AddSemanticKernelOrchestrator_RegistersBasicKernelFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSemanticKernelOrchestrator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var kernelFactory = serviceProvider.GetService<IKernelFactory>();
        
        Assert.NotNull(kernelFactory);
        Assert.IsType<BasicKernelFactory>(kernelFactory);
    }

    [Fact]
    public void AddSemanticKernelOrchestrator_RegistersSemanticKernelOrchestrator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSemanticKernelOrchestrator();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var orchestrator = serviceProvider.GetService<IWorkflowOrchestrator>();
        
        Assert.NotNull(orchestrator);
        Assert.IsType<SemanticKernelOrchestrator>(orchestrator);
    }

    [Fact]
    public void AddAiProviderServices_RegistersOrchestratorAndPlugins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAiProviderServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify orchestrator is registered
        Assert.NotNull(serviceProvider.GetService<IWorkflowOrchestrator>());
        
        // Verify plugins are registered (but not WorkflowExecutorPlugin)
        Assert.NotNull(serviceProvider.GetService<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.FileOperationsPlugin>());
        Assert.NotNull(serviceProvider.GetService<DotnetPrompt.Infrastructure.SemanticKernel.Plugins.ProjectAnalysisPlugin>());
        
        // Verify filters are registered
        Assert.NotNull(serviceProvider.GetService<IFunctionInvocationFilter>());
    }

    [Fact]
    public void AddConfigurationServices_RegistersConfigurationService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddConfigurationServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        Assert.NotNull(serviceProvider.GetService<IConfigurationService>());
    }

    [Fact]
    public void AddInfrastructureServices_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddInfrastructureServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        
        // Verify configuration services
        Assert.NotNull(serviceProvider.GetService<IConfigurationService>());
        
        // Verify AI provider services
        Assert.NotNull(serviceProvider.GetService<IWorkflowOrchestrator>());
        Assert.NotNull(serviceProvider.GetService<IKernelFactory>());
        Assert.NotNull(serviceProvider.GetService<IPromptTemplateFactory>());
    }

    [Fact]
    public void ServiceRegistration_ProperLifetimes_AreRespected()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSemanticKernelOrchestrator();

        // Assert
        var serviceDescriptors = services.ToList();
        
        // Verify HandlebarsPromptTemplateFactory is singleton
        var handlebarsDescriptor = serviceDescriptors.First(s => s.ServiceType == typeof(IPromptTemplateFactory));
        Assert.Equal(ServiceLifetime.Singleton, handlebarsDescriptor.Lifetime);
        
        // Verify SemanticKernelOrchestrator is scoped
        var orchestratorDescriptor = serviceDescriptors.First(s => s.ServiceType == typeof(IWorkflowOrchestrator));
        Assert.Equal(ServiceLifetime.Scoped, orchestratorDescriptor.Lifetime);
        
        // Verify BasicKernelFactory is singleton
        var kernelFactoryDescriptor = serviceDescriptors.First(s => s.ServiceType == typeof(IKernelFactory));
        Assert.Equal(ServiceLifetime.Singleton, kernelFactoryDescriptor.Lifetime);
    }
}