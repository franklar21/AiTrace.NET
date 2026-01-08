using AiTrace.Pro.Licensing;
using AiTrace.Pro.Signing;
using AiTrace.Pro.Verification;

namespace AiTrace.Pro;

public static class AiTracePro
{
    /// <summary>
    /// Vérifie l'intégrité des fichiers d'audit (hash + chaîne si PrevHashSha256 présent).
    /// Fonctionnalité Pro: requiert une licence valide.
    /// </summary>
    public static VerificationResult Verify(string auditDirectory)
    {
        LicenseGuard.EnsureLicensed();

        var publicKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            "aitrace_public.pem"
        );

        if (!File.Exists(publicKeyPath))
        {
            return VerificationResult.Fail(0,
                $"Public key not found: {publicKeyPath}");
        }

        var publicKeyPem = File.ReadAllText(publicKeyPath);

        var signatureOptions = new SignatureOptions
        {
            SignatureService = new RsaAuditSignatureService(publicKeyPem)
        };

        var verifier = new ChainVerifier(signatureOptions);
        return verifier.Verify(auditDirectory);
    }


    /// <summary>
    /// Vérifie l'intégrité du dossier d'audit par défaut (./aitrace).
    /// Fonctionnalité Pro : requiert une licence valide.
    /// </summary>
    public static VerificationResult VerifyDefault()
    {
        LicenseGuard.EnsureLicensed();

        var auditDir = Path.Combine(AppContext.BaseDirectory, "aitrace");
        var verifier = new ChainVerifier();
        return verifier.Verify(auditDir);
    }
}
