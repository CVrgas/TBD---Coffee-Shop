namespace Infrastructure.Caching;

public class CachePolicy
{
    public string Name { get; private set; }
    public TimeSpan Expiration { get; private set; }
    
    private CachePolicy(string name, TimeSpan expiration)
    {
        Name = name;
        Expiration = expiration;
    }
    
    public static readonly CachePolicy Catalog = new("Catalog", TimeSpan.FromMinutes(5));
    public static readonly CachePolicy Inventory = new("Inventory", TimeSpan.FromMinutes(5));

    public static List<CachePolicy> List() => [Catalog, Inventory];
}