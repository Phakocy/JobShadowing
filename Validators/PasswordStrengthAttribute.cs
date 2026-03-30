using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace JobShadowing.Validators
{
    public class PasswordStrengthAttribute : ValidationAttribute
    {
        public int MinimumLength { get; set; } = 8;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireDigit { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("Password is required");
            }

            var password = value.ToString()!;
            var errors = new List<string>();

            if (password.Length < MinimumLength)
            {
                errors.Add($"Password must be at least {MinimumLength} characters long");
            }

            if (RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }

            if (RequireLowercase && !Regex.IsMatch(password, @"[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }

            if (RequireDigit && !Regex.IsMatch(password, @"[0-9]"))
            {
                errors.Add("Password must contain at least one digit");
            }

            if (RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                errors.Add("Password must contain at least one special character");
            }

            if (errors.Count > 0)
            {
                return new ValidationResult(string.Join(". ", errors));
            }

            return ValidationResult.Success;
        }
    }
}
