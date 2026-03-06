namespace EventBookingService.Common.Validations
{
    /// <summary>
    /// Хэлпер для более явного указания намерений в коде
    /// + чтобы не писать в конструкторе в угловых скобках каждый раз тип
    /// </summary>
    public static class ResultCreator
    {
        public static ValidationResult<T?> Success<T>(T? val)
        {
            return new ValidationResult<T?>(val);
        }

        public static ValidationResult<T?> Fail<T>(T? val, IEnumerable<string> errors)
        {
            return new ValidationResult<T?>(val, errors);
        }

        public static ValidationResult<T?> Fail<T>(T? val, string error)
        {
            return new ValidationResult<T?>(val, error);
        }
    }
}
