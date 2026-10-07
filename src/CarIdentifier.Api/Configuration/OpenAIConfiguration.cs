using System.ComponentModel.DataAnnotations;

namespace CarIdentifier.Api.Configuration;

public sealed class OpenAIConfiguration
{
        public static string SectionName => "OpenAI";

        [Required]
        public string Model { get; set; } = string.Empty;

        [Required]
        public string ApiKey { get; set; } = string.Empty;
}