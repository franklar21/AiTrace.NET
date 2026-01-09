namespace AiTrace.Pro.Verification;

public static class ComplianceSummaryBuilder
{
    /// <summary>
    /// Builds a regulator-friendly summary from the low-level VerificationResult.
    /// Optionally provide additional scope info (files/time range) if your verifier collects it.
    /// </summary>
    public static ComplianceVerificationSummary FromResult(
        VerificationResult r,
        int filesVerified = 0,
        DateTimeOffset? firstUtc = null,
        DateTimeOffset? lastUtc = null,
        bool anySignaturePresent = false,
        bool signatureRequired = false)
    {
        if (r is null) throw new ArgumentNullException(nameof(r));

        return new ComplianceVerificationSummary
        {
            Status = r.Status,

            IntegrityVerified = r.Status != VerificationStatus.HashMismatch && r.Status != VerificationStatus.ParseError,
            ChainVerified = r.Status != VerificationStatus.ChainBroken,

            AnySignaturePresent = anySignaturePresent,
            SignatureRequired = signatureRequired,
            SignatureStatus = MapSignatureStatus(r, anySignaturePresent, signatureRequired),

            FilesVerified = filesVerified,
            RecordsVerified = filesVerified,

            FirstTimestampUtc = firstUtc,
            LastTimestampUtc = lastUtc,

            FailedIndex = r.FailedIndex,
            FailedFileName = r.FileName,
            Reason = r.Reason
        };
    }

    private static SignatureCheckStatus MapSignatureStatus(
        VerificationResult r,
        bool anySignaturePresent,
        bool signatureRequired)
    {
        // If verifier returned a signature-related status, reflect it.
        return r.Status switch
        {
            VerificationStatus.SignatureInvalid => SignatureCheckStatus.Invalid,
            VerificationStatus.SignatureServiceMissing => SignatureCheckStatus.MissingService,
            VerificationStatus.SignatureNotPresent => SignatureCheckStatus.NotPresent,
            VerificationStatus.SignatureRequiredButMissing => SignatureCheckStatus.RequiredButMissing,
            _ => r.SignatureChecked
                ? (r.SignatureValid ? SignatureCheckStatus.Valid : SignatureCheckStatus.Invalid)
                : (!anySignaturePresent
                    ? (signatureRequired ? SignatureCheckStatus.RequiredButMissing : SignatureCheckStatus.NotPresent)
                    : SignatureCheckStatus.NotChecked)
        };
    }
}
