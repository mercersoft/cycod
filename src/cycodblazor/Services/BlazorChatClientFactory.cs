using Microsoft.Extensions.AI;
using Cycodlib.Abstractions;

namespace cycodblazor.Services;

/// <summary>
/// Simplified chat client factory for Blazor WebAssembly that checks browser-stored configuration
/// and provides helpful error messages without heavy dependencies
/// </summary>
public static class BlazorChatClientFactory
{
    public static IChatClient CreateChatClient(IStorageProvider storageProvider)
    {
        // Check for configuration in browser storage instead of environment variables
        // This is done synchronously by blocking on async calls (acceptable for initialization)
        
        var anthropicKey = GetConfigValue(storageProvider, "ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(anthropicKey))
        {
            throw new NotSupportedException("Anthropic API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        var githubToken = GetConfigValue(storageProvider, "GITHUB_TOKEN"); 
        if (!string.IsNullOrEmpty(githubToken))
        {
            throw new NotSupportedException("GitHub Copilot is not supported in Blazor WebAssembly due to authentication requirements.");
        }

        var geminiKey = GetConfigValue(storageProvider, "GOOGLE_GEMINI_API_KEY");
        if (!string.IsNullOrEmpty(geminiKey))
        {
            throw new NotSupportedException("Google Gemini API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }
        
        var awsAccessKey = GetConfigValue(storageProvider, "AWS_BEDROCK_ACCESS_KEY");
        var awsSecretKey = GetConfigValue(storageProvider, "AWS_BEDROCK_SECRET_KEY");
        if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
        {
            throw new NotSupportedException("AWS Bedrock is not supported in Blazor WebAssembly due to authentication requirements.");
        }

        var azureKey = GetConfigValue(storageProvider, "AZURE_OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(azureKey))
        {
            throw new NotSupportedException("Azure OpenAI is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        var openaiKey = GetConfigValue(storageProvider, "OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(openaiKey))
        {
            throw new NotSupportedException("OpenAI API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        // If no configuration is found, throw the detailed error message
        var message = @"Error: No valid configuration found.

To configure Anthropic, run:
- cycod config set ANTHROPIC_API_KEY your-api-key
- cycod config set ANTHROPIC_MODEL_NAME model-name (optional)

To configure AWS Bedrock, run:
- cycod config set AWS_BEDROCK_ACCESS_KEY your-access-key
- cycod config set AWS_BEDROCK_SECRET_KEY your-secret-key
- cycod config set AWS_BEDROCK_REGION us-east-1 (optional)
- cycod config set AWS_BEDROCK_MODEL_ID model-id (optional)

To configure Azure OpenAI, run:
- cycod config set AZURE_OPENAI_API_KEY your-api-key
- cycod config set AZURE_OPENAI_ENDPOINT your-endpoint
- cycod config set AZURE_OPENAI_CHAT_DEPLOYMENT your-deployment

To configure GitHub Copilot, run:
- cycod config set GITHUB_TOKEN your-token
- cycod config set COPILOT_API_ENDPOINT endpoint (optional)
- cycod config set COPILOT_INTEGRATION_ID id (optional)
- cycod config set COPILOT_EDITOR_VERSION version (optional)
- cycod config set COPILOT_MODEL_NAME model (optional)

To configure Google Gemini, run:
- cycod config set GOOGLE_GEMINI_API_KEY your-api-key
- cycod config set GOOGLE_GEMINI_MODEL_ID model-id (optional)

To configure OpenAI, run:
- cycod config set OPENAI_API_KEY your-api-key
- cycod config set OPENAI_ENDPOINT your-endpoint (optional)
- cycod config set OPENAI_CHAT_MODEL_NAME model-name (optional)

Run 'cycod config list' to see current configuration.
Run 'cycod config --help' for more information.";

        throw new InvalidOperationException(message);
    }

    private static string? GetConfigValue(IStorageProvider storageProvider, string key)
    {
        try
        {
            // Read config value from browser storage synchronously
            // Using the same key format as the config system would use
            var task = storageProvider.ReadTextAsync($"config/{key}");
            task.Wait(); // Block on async call for simplicity
            return task.Result;
        }
        catch
        {
            return null;
        }
    }
}