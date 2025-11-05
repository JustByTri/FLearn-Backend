using System;

namespace BLL.Settings
{
 public class AzureOpenAISettings
 {
 public string Endpoint { get; set; } = string.Empty; // e.g. https://<res>.cognitiveservices.azure.com/
 public string ApiKey { get; set; } = string.Empty; // Azure OpenAI key
 public string ChatDeployment { get; set; } = string.Empty; // chat model deployment
 public string TranscriptionDeployment { get; set; } = string.Empty; // whisper deployment name (optional)
 public string ApiVersion { get; set; } = "2025-01-01-preview"; // default version
 public int MaxOutputTokens { get; set; } =8192;
 public double Temperature { get; set; } =0.3;
 }
}
