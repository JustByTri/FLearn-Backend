using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Language
{
    public class AllowedLangAttribute : ValidationAttribute
    {
        private static readonly string[] AllowedLangs = { "en", "ja", "zh" };

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string lang && AllowedLangs.Contains(lang.ToLower()))
            {
                return ValidationResult.Success!;
            }

            return new ValidationResult("Invalid language code. Allowed values are: en, ja, zh.");
        }
    }
}
