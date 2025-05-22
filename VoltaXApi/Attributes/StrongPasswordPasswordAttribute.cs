using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace VoltaXApi.Attributes;
public class StrongPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var password = value as string;

        if (string.IsNullOrWhiteSpace(password))
            return ValidationResult.Success; // Let [Required] handle empty values

        bool hasUpperCase = Regex.IsMatch(password, "[A-Z]");
        bool hasLowerCase = Regex.IsMatch(password, "[a-z]");
        bool hasDigit = Regex.IsMatch(password, "[0-9]");
        bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]");
        bool isValidLength = password.Length >= 8;

        if (hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar && isValidLength)
            return ValidationResult.Success;

        return new ValidationResult("Password must be at least 8 characters long and contain uppercase, lowercase, number, and special character.");
    }
}