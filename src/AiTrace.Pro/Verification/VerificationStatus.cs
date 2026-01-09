namespace AiTrace.Pro.Verification;

public enum VerificationStatus
{
    Ok = 0,

    // Integrity / chain issues
    HashMismatch,
    ChainBroken,

    // Signature issues
    SignatureInvalid,
    SignatureNotPresent,
    SignatureServiceMissing,
    SignatureRequiredButMissing,

    // Input / file issues
    DirectoryNotFound,
    NoFiles,
    ParseError
}
