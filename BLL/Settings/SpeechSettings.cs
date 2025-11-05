using System;
using System.Collections.Generic;

namespace BLL.Settings
{
 public class SpeechSettings
 {
 public string Region { get; set; } = string.Empty; 
 public string ApiKey { get; set; } = string.Empty; 
 public List<string> Languages { get; set; } = new() { "en-US", "ja-JP", "zh-CN" };
 }
}
