using FluentValidation.Results;

namespace Application.Common.Validation;

public static class ValidationExtensions
{
    public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> ToEnvelopeErrors(this ValidationResult v) => v.Errors
        .GroupBy(e => e.PropertyName)
        .Select(g => new KeyValuePair<string, IEnumerable<string>>(g.Key, g.Select(e => e.ErrorMessage)))
        .ToList();
}