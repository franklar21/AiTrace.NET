namespace AiTrace.Pro.Verification;

public enum SignatureCheckStatus
{
    NotChecked = 0,
    Valid,
    Invalid,
    NotPresent,
    MissingService,
    RequiredButMissing
}

/// <summary>
/// Regulator-friendly summary built from low-level verification.
/// This is what you can export/print/share.
/// </summary>
public sealed class ComplianceVerificationSummary
{
    public VerificationStatus Status { get; init; } = VerificationStatus.Ok;

    public bool IsValid => Status == VerificationStatus.Ok;

    // Integrity proof
    public bool IntegrityVerified { get; init; }   // Hashes verified for all records
    public bool ChainVerified { get; init; }       // PrevHashSha256 chain consistent (when applicable)

    // Signature proof (Pro)
    public bool AnySignaturePresent { get; init; }
    public bool SignatureRequired { get; init; }
    public SignatureCheckStatus SignatureStatus { get; init; } = SignatureCheckStatus.NotChecked;

    // Scope (useful to a regulator)
    public int FilesVerified { get; init; }
    public int RecordsVerified { get; init; } // same as FilesVerified for JSON store

    public DateTimeOffset? FirstTimestampUtc { get; init; }
    public DateTimeOffset? LastTimestampUtc { get; init; }

    // Failure details
    public int? FailedIndex { get; init; }
    public string? FailedFileName { get; init; }
    public string? Reason { get; init; }
}
