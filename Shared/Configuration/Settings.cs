namespace Shared.Configuration;

public class JwtSettings
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 60;
}


public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
}
