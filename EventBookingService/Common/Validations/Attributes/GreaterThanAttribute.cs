using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace EventBookingService.Common.Validations.Attributes;

/// <summary>
/// Атрибут валидации для сравнения (больше) значения свойства с другим свойством в данном объекте
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class GreaterThanAttribute : ValidationAttribute
{
    #region Properties

    public string OtherProperty { get; }

    public string? OtherPropertyDisplayName { get; internal set; }

    public override bool RequiresValidationContext => true;

    #endregion

    #region Constructors

    [RequiresUnreferencedCode("Свойство, указанное в параметре 'otherProperty', может быть удалено линкером. Убедитесь, что оно сохраняется.")]
    public GreaterThanAttribute(string otherProperty) : base()
    {
        ArgumentNullException.ThrowIfNull(otherProperty);

        OtherProperty = otherProperty;
    }

    #endregion

    #region Base overrides

    public override string FormatErrorMessage(string name) =>
        string.Format(
            CultureInfo.CurrentCulture, ErrorMessageString, name, OtherPropertyDisplayName ?? OtherProperty);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        ArgumentNullException.ThrowIfNull(validationContext);

        // Свойство пустое, это может быть ок, не наше дело
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var otherPropertyInfo = validationContext.ObjectType.GetRuntimeProperty(OtherProperty);
        if (otherPropertyInfo == null)
        {
            return new ValidationResult($"Отсутствует свойство с именем {OtherProperty} в объекте типа {validationContext.ObjectType.FullName}");
        }
        if (otherPropertyInfo.GetIndexParameters().Length > 0)
        {
            return new ValidationResult($"Свойство с именем {OtherProperty} является индексируемым, проверка невозможна");
        }

        object? otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);

        // Если свойство для сравнения null, то это не наша проблема, пусть другие атрибуты это контролируют, если нужно
        if (otherPropertyValue is null)
        {
            return ValidationResult.Success;
        }

        if (value is IComparable comparableValue)
        {
            if (otherPropertyValue is not IComparable otherComparableValue)
            {
                return new ValidationResult($"Свойство с именем {OtherProperty} не поддерживает сравнение");
            }

            if (comparableValue.CompareTo(otherComparableValue) <= 0)
            {
                OtherPropertyDisplayName ??= GetDisplayNameForProperty(otherPropertyInfo);

                string[]? memberNames = validationContext.MemberName != null
                   ? [validationContext.MemberName]
                   : null;
                return new ValidationResult(FormatErrorMessage(validationContext.DisplayName), memberNames);
            }
        }
        else
        {
            return new ValidationResult($"Свойство с именем {validationContext.MemberName} не поддерживает сравнение");
        }

        return ValidationResult.Success;
    }

    #endregion

    #region Private methods

    private string? GetDisplayNameForProperty(PropertyInfo property)
    {
        var attributes = CustomAttributeExtensions.GetCustomAttributes(property, true);
        foreach (var attribute in attributes)
        {
            if (attribute is DisplayAttribute display)
            {
                return display.GetName();
            }
        }

        return OtherProperty;
    }

    #endregion
}
