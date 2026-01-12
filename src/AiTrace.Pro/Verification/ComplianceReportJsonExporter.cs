using System.Text;
using System.Text.Json;

namespace AiTrace.Pro.Verification;

public static class ComplianceReportJsonExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Generates a compliance summary and writes it as JSON to disk.
    /// </summary>
    public static string WriteJsonReport(
        string auditDirectory,
        string outputPath,
        ChainVerifier verifier,
        bool signatureRequired = true)
    {
        if (verifier is null) throw new ArgumentNullException(nameof(verifier));

        var summary = verifier.VerifySummary(
            auditDirectory,
            signatureRequired: signatureRequired
        );

        var json = JsonSerializer.Serialize(summary, JsonOptions);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, Encoding.UTF8);

        return outputPath;
    }
}
