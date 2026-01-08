using System.Text;
using System.Text.Json;
using AiTrace.Pro.Licensing;
using AiTrace.Pro.Signing;

namespace AiTrace.Pro.Stores;

/// <summary>
/// Pro JSON store that finalizes (PrevHash + Hash) then signs the final hash before writing.
/// This avoids signature invalidation caused by stores mutating the record after signing.
/// </summary>
public sealed class SignedJsonAuditStore : IAuditStore
{
    private readonly string _directory;
    private readonly IAuditSignatureService _signer;

    public SignedJsonAuditStore(IAuditSignatureService signer, string? directory = null)
    {
        _signer = signer ?? throw new ArgumentNullException(nameof(signer));

        _directory = string.IsNullOrWhiteSpace(directory)
            ? Path.Combine(AppContext.BaseDirectory, "aitrace")
            : directory;

        Directory.CreateDirectory(_directory);
    }

    public async Task WriteAsync(AuditRecord record, CancellationToken ct = default)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        LicenseGuard.EnsureLicensed();

        // 1) Chain hashing: find previous hash
        var prev = TryGetLastHash(_directory);
        record.PrevHashSha256 = prev;

        // 2) Compute final hash (must include PrevHashSha256 if your hasher uses it)
        record.HashSha256 = AuditHasher.ComputeRecordHash(record);

        // 3) Sign the final hash
        var signature = _signer.Sign(record.HashSha256);

        // Signature fields are init-only => create a copy
        var signed = record with
        {
            Signature = signature,
            SignatureAlgorithm = "RSA-SHA256"
        };

        // 4) Write file (one file per record)
        var fileName = $"{signed.TimestampUtc:yyyyMMdd_HHmmssfff}_{signed.Id}.json";
        var path = Path.Combine(_directory, fileName);

        var json = JsonSerializer.Serialize(signed, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(path, json, Encoding.UTF8, ct).ConfigureAwait(false);
    }

    private static string? TryGetLastHash(string auditDir)
    {
        if (!Directory.Exists(auditDir)) return null;

        var lastFile = Directory.GetFiles(auditDir, "*.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (lastFile is null) return null;

        var json = File.ReadAllText(lastFile);

        const string key = "\"HashSha256\":";
        var i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;

        i += key.Length;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i >= json.Length || json[i] != '"') return null;
        i++;

        var j = json.IndexOf('"', i);
        if (j < 0) return null;

        var value = json.Substring(i, j - i);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
