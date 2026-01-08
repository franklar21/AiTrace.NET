using System.Text.Json;
using AiTrace;
using AiTrace.Pro.Signing;

namespace AiTrace.Pro.Verification;

public sealed class ChainVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SignatureOptions _sig;

    // ✅ Nouveau: injection optionnelle pour vérifier les signatures
    public ChainVerifier(SignatureOptions? signatureOptions = null)
    {
        _sig = signatureOptions ?? new SignatureOptions();
    }

    public VerificationResult Verify(string auditDirectory)
    {
        if (string.IsNullOrWhiteSpace(auditDirectory))
            return VerificationResult.Fail(0, "Audit directory is empty.");

        if (!Directory.Exists(auditDirectory))
            return VerificationResult.Fail(0, $"Audit directory not found: {auditDirectory}");

        var files = Directory.GetFiles(auditDirectory, "*.json", SearchOption.AllDirectories)
            .OrderBy(f => File.GetLastWriteTimeUtc(f)) // ordre chronologique
            .ToArray();

        if (files.Length == 0)
            return VerificationResult.Fail(0, $"No audit JSON files found under: {auditDirectory}");

        string? lastHash = null;

        for (int idx = 0; idx < files.Length; idx++)
        {
            var file = files[idx];

            AuditRecord? record;
            try
            {
                var json = File.ReadAllText(file);
                record = JsonSerializer.Deserialize<AuditRecord>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                return VerificationResult.Fail(idx, $"Failed to read/parse '{Path.GetFileName(file)}': {ex.Message}");
            }

            if (record is null)
                return VerificationResult.Fail(idx, $"Invalid JSON record in '{Path.GetFileName(file)}'.");

            // 1) Vérifie le hash du record
            var expected = AuditHasher.ComputeRecordHash(record);
            if (!string.Equals(record.HashSha256, expected, StringComparison.OrdinalIgnoreCase))
            {
                return VerificationResult.Fail(idx,
                    $"Hash mismatch in '{Path.GetFileName(file)}'. Expected {expected} but found {record.HashSha256 ?? "(null)"}.");
            }

            // ✅ 1.5) Vérifie la signature si elle est présente
            var hasSignature =
                !string.IsNullOrWhiteSpace(record.Signature) &&
                !string.IsNullOrWhiteSpace(record.SignatureAlgorithm);

            if (hasSignature)
            {
                if (_sig.SignatureService is null)
                {
                    return VerificationResult.Fail(idx,
                        $"Signature present in '{Path.GetFileName(file)}' but no SignatureService configured.");
                }

                var ok = _sig.SignatureService.Verify(record.HashSha256, record.Signature!);
                if (!ok)
                {
                    return VerificationResult.Fail(idx,
                        $"Signature invalid in '{Path.GetFileName(file)}'.");
                }
            }

            // 2) Vérifie la chaîne si PrevHashSha256 est présent
            if (idx == 0)
            {
                // premier record : PrevHash peut être null/empty, on accepte
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(record.PrevHashSha256))
                {
                    if (!string.Equals(record.PrevHashSha256, lastHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return VerificationResult.Fail(idx,
                            $"Chain broken at '{Path.GetFileName(file)}'. PrevHashSha256={record.PrevHashSha256} but previous hash was {lastHash}.");
                    }
                }
            }

            lastHash = record.HashSha256;
        }

        return VerificationResult.Ok();
    }
}
