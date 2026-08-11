using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_WebApp.Validation;

public class NonNegativeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        decimal? number = value switch
        {
            int i => i,
            decimal d => d,
            double db => (decimal)db,
            long l => l,
            _ => null
        };

        if (number is null)
            return ValidationResult.Success; // Let [Required]/model binding handle missing or non-numeric values

        if (number < 0)
        {
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} cannot be negative.");
        }

        return ValidationResult.Success;
    }
}
