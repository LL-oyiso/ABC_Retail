using System.ComponentModel.DataAnnotations;

namespace ABC_Retail_Functions.Validation;

/// <summary>
/// Functions have no MVC model binder, so DataAnnotations on request bodies are
/// evaluated explicitly here to keep validation declarative on the models.
/// </summary>
public static class RequestValidation
{
    public static bool TryValidate<T>(T instance, out List<string> errors) where T : class
    {
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            results,
            validateAllProperties: true);

        errors = results
            .Select(result => result.ErrorMessage ?? "Invalid value.")
            .ToList();

        return isValid;
    }
}
