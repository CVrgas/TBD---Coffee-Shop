using System.Text.RegularExpressions;

namespace Domain.Users.ValueObjects;

public sealed partial record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        var formatted = value?.ToLowerInvariant().Trim();
        
        if (string.IsNullOrEmpty(formatted)) 
            throw new ArgumentNullException(nameof(value), "Email address cannot be null or empty.");
            
        if (!EmailFormatRegex().IsMatch(formatted)) 
            throw new ArgumentException("Invalid email format.", nameof(value));

        Value = formatted;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailFormatRegex();
}