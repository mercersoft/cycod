using Microsoft.Extensions.AI;

namespace cycodblazor.Services;

/// <summary>
/// Simplified chat client factory for Blazor WebAssembly that checks environment variables
/// and provides helpful error messages without heavy dependencies
/// </summary>
public static class BlazorChatClientFactory
{
    public static IChatClient CreateChatClient()
    {
        // Check for environment variables directly without using EnvironmentHelpers
        // to avoid configuration system dependencies that don't work in WebAssembly
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN")))
        {
            throw new NotSupportedException("GitHub Copilot is not supported in Blazor WebAssembly due to authentication requirements.");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
        {
            throw new NotSupportedException("Anthropic API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY")))
        {
            throw new NotSupportedException("Google Gemini API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }
        
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_BEDROCK_ACCESS_KEY")) && 
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_BEDROCK_SECRET_KEY")))
        {
            throw new NotSupportedException("AWS Bedrock is not supported in Blazor WebAssembly due to authentication requirements.");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")))
        {
            throw new NotSupportedException("Azure OpenAI is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
        {
            throw new NotSupportedException("OpenAI API is not supported in Blazor WebAssembly due to CORS restrictions.");
        }

        // If no environment variables are found, throw the detailed error message
        var message = @"Error: No valid environment variables found.

To use Anthropic, please set:
- ANTHROPIC_API_KEY
- ANTHROPIC_MODEL_NAME (optional)

To use AWS Bedrock, please set:
- AWS_BEDROCK_ACCESS_KEY
- AWS_BEDROCK_SECRET_KEY
- AWS_BEDROCK_REGION (optional, default: us-east-1)
- AWS_BEDROCK_MODEL_ID (optional, default: anthropic.claude-3-7-sonnet-20250219-v1:0)

To use Azure OpenAI, please set:
- AZURE_OPENAI_API_KEY
- AZURE_OPENAI_ENDPOINT
- AZURE_OPENAI_CHAT_DEPLOYMENT

To use GitHub Copilot with token authentication, please set:
- GITHUB_TOKEN
- COPILOT_API_ENDPOINT (optional)
- COPILOT_INTEGRATION_ID (optional)
- COPILOT_EDITOR_VERSION (optional)
- COPILOT_MODEL_NAME (optional)

To use Google Gemini, please set:
- GOOGLE_GEMINI_API_KEY
- GOOGLE_GEMINI_MODEL_ID (optional, default: gemini-2.5-flash-preview-04-17)

To use OpenAI, please set:
- OPENAI_API_KEY
- OPENAI_ENDPOINT (optional)
- OPENAI_CHAT_MODEL_NAME (optional)";

        throw new InvalidOperationException(message);
    }
}