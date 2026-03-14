using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.Attributes;

/// <summary>
/// Validation attribute to ensure one date is greater than another
/// </summary>
public class DateGreaterThanAttribute : ValidationAttribute
{
    private readonly string _comparisonProperty;

    public DateGreaterThanAttribute(string comparisonProperty)
    {
        _comparisonProperty = comparisonProperty;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var currentValue = (DateTime)value;

        var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

        if (property == null)
        {
            throw new ArgumentException($"Property {_comparisonProperty} not found");
        }

        var comparisonValue = property.GetValue(validationContext.ObjectInstance);

        if (comparisonValue == null)
        {
            return ValidationResult.Success;
        }

        var comparisonDate = (DateTime)comparisonValue;

        if (currentValue <= comparisonDate)
        {
            return new ValidationResult(ErrorMessage ?? $"Due date must be after start date");
        }

        return ValidationResult.Success;
    }
}


