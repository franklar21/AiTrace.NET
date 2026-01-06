using System.Security.Cryptography;
using System.Text;

namespace AiTrace;

public static class AuditHasher
{
    /// <summary>
    /// Calcule le SHA-256 hex (lowercase) d'une chaîne UTF-8.
    /// </summary>
    public static string Sha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Construit une "matière" de hash déterministe. Doit être utilisée partout (store + verify).
    /// </summary>
    public static string BuildHashMaterial(AuditRecord r)
    {
        // IMPORTANT : ordre + séparateurs stables.
        // On inclut PrevHashSha256 si présent (vide sinon).
        // TimestampUtc en "O" (ISO 8601 round-trip) pour stabilité.
        return string.Join("\n", new[]
        {
            r.Id ?? "",
            r.TimestampUtc.ToString("O"),
            r.Model ?? "",
            r.UserId ?? "",
            r.ContentStored ? "1" : "0",
            r.Prompt ?? "",
            r.Output ?? "",
            r.MetadataJson ?? "",
            r.PrevHashSha256 ?? ""
        });
    }

    public static string ComputeRecordHash(AuditRecord r)
        => Sha256Hex(BuildHashMaterial(r));
}
