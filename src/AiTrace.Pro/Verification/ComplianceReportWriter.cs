using System.Text;

namespace AiTrace.Pro.Verification;

public static class ComplianceReportWriter
{
    public static string ToTextReport(ComplianceVerificationSummary s)
    {
        var sb = new StringBuilder();

        sb.AppendLine("AI Decision Audit Verification Report");
        sb.AppendLine();

        sb.AppendLine($"Status: {s.Status}");
        if (!string.IsNullOrWhiteSpace(s.Reason))
            sb.AppendLine($"Reason: {s.Reason}");

        sb.AppendLine();
        sb.AppendLine("Integrity:");
        sb.AppendLine($"- Record integrity: {(s.IntegrityVerified ? "VERIFIED" : "FAILED")}");
        sb.AppendLine($"- Chain integrity: {(s.ChainVerified ? "VERIFIED" : "FAILED")}");

        sb.AppendLine();
        sb.AppendLine("Signature (Pro):");
        sb.AppendLine($"- Signature required: {(s.SignatureRequired ? "YES" : "NO")}");
        sb.AppendLine($"- Any signature present: {(s.AnySignaturePresent ? "YES" : "NO")}");
        sb.AppendLine($"- Signature status: {s.SignatureStatus}");

        sb.AppendLine();
        sb.AppendLine("Scope:");
        sb.AppendLine($"- Files verified: {s.FilesVerified}");
        sb.AppendLine($"- Records verified: {s.RecordsVerified}");
        sb.AppendLine($"- Time range (UTC): {Fmt(s.FirstTimestampUtc)} to {Fmt(s.LastTimestampUtc)}");

        sb.AppendLine();
        sb.AppendLine("Tampering detection:");
        sb.AppendLine($"- Post-decision modification detected: {(s.IsValid ? "NO" : "YES")}");

        if (!s.IsValid)
        {
            sb.AppendLine();
            sb.AppendLine("Failure details:");
            sb.AppendLine($"- Failed index: {s.FailedIndex}");
            sb.AppendLine($"- File: {s.FailedFileName}");
        }

        sb.AppendLine();
        sb.AppendLine("Conclusion:");
        sb.AppendLine(s.IsValid
            ? "No evidence of post-decision modification detected."
            : "Potential tampering or mismatch detected. See failure details.");

        return sb.ToString();
    }

    private static string Fmt(DateTimeOffset? dt)
        => dt is null ? "(unknown)" : dt.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
}
