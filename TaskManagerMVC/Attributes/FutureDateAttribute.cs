using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.Attributes;

/// <summary>
/// Validates that a date is today or in the future.
/// Used for enforcing that due dates cannot be set to past dates.
/// </summary>
public class FutureDateAttribute : ValidationAttribute
{
    public bool AllowToday { get; set; } = true;

    public FutureDateAttribute() : base("Date must be in the future")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success; // Let [Required] handle null checking

        if (value is DateTime date)
        {
            var compareDate = AllowToday ? DateTime.Today : DateTime.Today.AddDays(1);
            
            if (date.Date < compareDate)
            {
                var errorMessage = ErrorMessage ?? (AllowToday 
                    ? "Date must be today or in the future"
                    : "Date must be in the future");
                return new ValidationResult(errorMessage);
            }
        }

        return ValidationResult.Success;
    }
}
