namespace Domain.User;

public record PasswordHash
{
    public string Value { get; }
    
    internal PasswordHash(string value) 
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Hash cannot be empty");
        Value = value;
    }
    
    public static implicit operator string(PasswordHash hash) => hash.Value;
}