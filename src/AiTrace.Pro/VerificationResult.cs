namespace AiTrace.Pro.Verification;

public sealed class VerificationResult
{
    public bool IsValid { get; init; }
    public int? FailedIndex { get; init; }
    public string? Reason { get; init; }

    public static VerificationResult Ok()
        => new() { IsValid = true };

    public static VerificationResult Fail(int index, string reason)
        => new() { IsValid = false, FailedIndex = index, Reason = reason };
}