namespace AiTrace.Pro.Licensing;

public sealed class LicenseInfo
{
    public string Licensee { get; init; } = "";
    public DateTimeOffset ExpiresUtc { get; init; }
    public string Plan { get; init; } = "Pro";

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresUtc;
}
