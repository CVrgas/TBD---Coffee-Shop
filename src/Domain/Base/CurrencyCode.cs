namespace Domain.Base;

public readonly record struct CurrencyCode
{
    public CurrencyCode(string code)
    {
        if(string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO code.", nameof(code));
        Code = code.ToUpperInvariant();
    }

    public string Code { get; }
    public override string ToString() => Code;
}