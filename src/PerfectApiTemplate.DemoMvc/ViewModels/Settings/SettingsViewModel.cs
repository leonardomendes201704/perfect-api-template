using System.ComponentModel.DataAnnotations;

namespace PerfectApiTemplate.DemoMvc.ViewModels.Settings;

public sealed class SettingsViewModel
{
    [Required]
    [Url]
    public string ApiBaseUrl { get; set; } = string.Empty;
}

