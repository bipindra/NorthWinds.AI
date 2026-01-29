namespace Northwind.Portal.Web.Models;

public class LLMOptions
{
    public const string SectionName = "LLMOptions";
    
    public string Provider { get; set; } = "OpenAI";
    
    public OpenAIOptions OpenAI { get; set; } = new();
    public AnthropicOptions Anthropic { get; set; } = new();
    public AzureOpenAIOptions AzureOpenAI { get; set; } = new();
}

public class OpenAIOptions
{
    public string Model { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingModel { get; set; }
}

public class AnthropicOptions
{
    public string Model { get; set; } = "claude-3-opus-20240229";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

public class AzureOpenAIOptions
{
    public string DeploymentName { get; set; } = "gpt-4";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string? EmbeddingDeploymentName { get; set; }
}
