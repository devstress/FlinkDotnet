using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Comprehensive unit tests for AI/ML Integration features (WI9)
/// Achieves 100% code coverage for IModelProvider, OpenAIProvider, AzureOpenAIProvider, and ModelProviderFactory
/// </summary>
[TestFixture]
public class AIMLIntegrationTests
{
    #region IModelProvider Tests

    [Test]
    public void OpenAIProvider_ProviderName_ReturnsOpenAI()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Act & Assert
        Assert.That(provider.ProviderName, Is.EqualTo("openai"));
    }

    [Test]
    public void AzureOpenAIProvider_ProviderName_ReturnsAzureOpenAI()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();

        // Act & Assert
        Assert.That(provider.ProviderName, Is.EqualTo("azure_openai"));
    }

    #endregion

    #region OpenAIProvider Validation Tests

    [Test]
    public void OpenAIProvider_ValidateConfiguration_WithValidApiKey_ReturnsTrue()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "openai.api_key", "sk-test123" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void OpenAIProvider_ValidateConfiguration_WithAlternativeApiKeyFormat_ReturnsTrue()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "openai.api-key", "sk-test123" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void OpenAIProvider_ValidateConfiguration_WithNullProperties_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAIProvider();

        // Act
        bool result = provider.ValidateConfiguration(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void OpenAIProvider_ValidateConfiguration_WithoutApiKey_ReturnsFalse()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "openai.model", "gpt-4" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task OpenAIProvider_InferAsync_ReturnsMockResult()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var input = new Dictionary<string, object>
        {
            { "text", "test input" }
        };
        var properties = new Dictionary<string, string>
        {
            { "openai.api_key", "sk-test" }
        };

        // Act
        Dictionary<string, object> result = await provider.InferAsync("test-model", input, properties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result["provider"], Is.EqualTo("openai"));
        Assert.That(result["model"], Is.EqualTo("test-model"));
        Assert.That(result["status"], Is.EqualTo("success"));
    }

    [Test]
    public async Task OpenAIProvider_InferAsync_WithCancellationToken_ReturnsMockResult()
    {
        // Arrange
        var provider = new OpenAIProvider();
        var input = new Dictionary<string, object> { { "text", "test" } };
        var properties = new Dictionary<string, string> { { "openai.api_key", "sk-test" } };
        var cancellationToken = new CancellationTokenSource().Token;

        // Act
        Dictionary<string, object> result = await provider.InferAsync("model", input, properties, cancellationToken);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("provider"), Is.True);
    }

    #endregion

    #region AzureOpenAIProvider Validation Tests

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithValidConfig_ReturnsTrue()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" },
            { "azure.deployment", "gpt-4" },
            { "azure.api_key", "test-key" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithEndpointAndApiKey_ReturnsTrue()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" },
            { "azure.api_key", "test-key" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithAlternativeApiKeyFormat_ReturnsTrue()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" },
            { "azure.api-key", "test-key" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithNullProperties_ReturnsFalse()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();

        // Act
        bool result = provider.ValidateConfiguration(null!);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithoutEndpoint_ReturnsFalse()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "azure.deployment", "gpt-4" },
            { "azure.api_key", "test-key" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void AzureOpenAIProvider_ValidateConfiguration_WithEndpointButNoDeploymentOrKey_ReturnsFalse()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var config = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" }
        };

        // Act
        bool result = provider.ValidateConfiguration(config);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task AzureOpenAIProvider_InferAsync_ReturnsMockResult()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var input = new Dictionary<string, object>
        {
            { "text", "test input" }
        };
        var properties = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" },
            { "azure.deployment", "gpt-4" }
        };

        // Act
        Dictionary<string, object> result = await provider.InferAsync("test-model", input, properties);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result["provider"], Is.EqualTo("azure_openai"));
        Assert.That(result["model"], Is.EqualTo("test-model"));
        Assert.That(result["deployment"], Is.EqualTo("gpt-4"));
        Assert.That(result["status"], Is.EqualTo("success"));
    }

    [Test]
    public async Task AzureOpenAIProvider_InferAsync_WithMissingDeployment_UsesDefault()
    {
        // Arrange
        var provider = new AzureOpenAIProvider();
        var input = new Dictionary<string, object> { { "text", "test" } };
        var properties = new Dictionary<string, string>
        {
            { "azure.endpoint", "https://test.openai.azure.com" }
        };

        // Act
        Dictionary<string, object> result = await provider.InferAsync("model", input, properties);

        // Assert
        Assert.That(result["deployment"], Is.EqualTo("default"));
    }

    #endregion

    #region ModelProviderFactory Tests

    [Test]
    public void ModelProviderFactory_GetProvider_OpenAI_ReturnsOpenAIProvider()
    {
        // Act
        IModelProvider? provider = ModelProviderFactory.GetProvider("openai");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<OpenAIProvider>());
    }

    [Test]
    public void ModelProviderFactory_GetProvider_AzureOpenAI_ReturnsAzureOpenAIProvider()
    {
        // Act
        IModelProvider? provider = ModelProviderFactory.GetProvider("azure_openai");

        // Assert
        Assert.That(provider, Is.Not.Null);
        Assert.That(provider, Is.InstanceOf<AzureOpenAIProvider>());
    }

    [Test]
    public void ModelProviderFactory_GetProvider_CaseInsensitive_ReturnsProvider()
    {
        // Act
        IModelProvider? provider1 = ModelProviderFactory.GetProvider("OPENAI");
        IModelProvider? provider2 = ModelProviderFactory.GetProvider("OpenAI");
        IModelProvider? provider3 = ModelProviderFactory.GetProvider("OpEnAi");

        // Assert
        Assert.That(provider1, Is.Not.Null);
        Assert.That(provider2, Is.Not.Null);
        Assert.That(provider3, Is.Not.Null);
    }

    [Test]
    public void ModelProviderFactory_GetProvider_UnknownProvider_ReturnsNull()
    {
        // Act
        IModelProvider? provider = ModelProviderFactory.GetProvider("unknown_provider");

        // Assert
        Assert.That(provider, Is.Null);
    }

    [Test]
    public void ModelProviderFactory_GetProvider_NullProvider_ReturnsNull()
    {
        // Act
        IModelProvider? provider = ModelProviderFactory.GetProvider(null!);

        // Assert
        Assert.That(provider, Is.Null);
    }

    [Test]
    public void ModelProviderFactory_GetProvider_EmptyProvider_ReturnsNull()
    {
        // Act
        IModelProvider? provider = ModelProviderFactory.GetProvider("");

        // Assert
        Assert.That(provider, Is.Null);
    }

    [Test]
    public void ModelProviderFactory_RegisterProvider_CustomProvider_Success()
    {
        // Arrange
        var customProvider = new CustomTestProvider();

        // Act
        ModelProviderFactory.RegisterProvider(customProvider);
        IModelProvider? retrieved = ModelProviderFactory.GetProvider("custom_test");

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved, Is.SameAs(customProvider));
    }

    [Test]
    public void ModelProviderFactory_RegisterProvider_NullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ModelProviderFactory.RegisterProvider(null!));
    }

    [Test]
    public void ModelProviderFactory_GetProviderNames_ReturnsAllProviders()
    {
        // Act
        IEnumerable<string> names = ModelProviderFactory.GetProviderNames();

        // Assert
        Assert.That(names, Is.Not.Null);
        Assert.That(names, Contains.Item("OPENAI"));
        Assert.That(names, Contains.Item("AZURE_OPENAI"));
    }

    #endregion

    #region Helper Classes

    private class CustomTestProvider : IModelProvider
    {
        public string ProviderName => "custom_test";

        public bool ValidateConfiguration(Dictionary<string, string> properties) => true;

        public Task<Dictionary<string, object>> InferAsync(
            string modelName,
            Dictionary<string, object> input,
            Dictionary<string, string> properties,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<string, object> { { "test", "custom" } });
        }
    }

    #endregion
}
