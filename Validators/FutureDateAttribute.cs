using System.ComponentModel.DataAnnotations;

namespace JobShadowing.Validators
{
    public class FutureDateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateTime dateTime)
            {
                if (dateTime.Date < DateTime.UtcNow.Date)
                {
                    return new ValidationResult(
                        ErrorMessage ?? "Date must be in the future");
                }
                return ValidationResult.Success;
            }

            return new ValidationResult("Invalid date format");
        }
    }
}
