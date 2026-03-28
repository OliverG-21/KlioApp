namespace KlioApp.Core.Models;

public class AppSettings
{
    public string ServerUrl { get; set; } = "http://localhost:5005";
    public string ApiKey { get; set; } = string.Empty;
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "llava-phi3";
    public string LastLocalFolder { get; set; } = string.Empty;
}
