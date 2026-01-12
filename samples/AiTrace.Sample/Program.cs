using AiTrace;
using AiTrace.Pro.Signing;
using AiTrace.Pro.Stores;
using AiTrace.Pro.Verification;

// ---- Configure AiTrace (Pro signing store) ----
AiTrace.AiTrace.Configure(o =>
{
    o.StoreContent = true;
    o.BasicRedaction = true;

    var privateKeyPem = File.ReadAllText(@"C:\temp\aitrace_private.pem");
    var signer = new RsaAuditSignatureService(privateKeyPem);

    // Pro store: computes PrevHash + Hash, then signs, then writes JSON files
    o.Store = new SignedJsonAuditStore(signer);
});

// ---- Log one decision ----
var decision = new AiDecision
{
    Prompt = "Summarize: The quick brown fox jumps over the lazy dog.",
    Output = "A fox jumps over a dog.",
    Model = "demo-model",
    UserId = "user-123",
    Metadata = new Dictionary<string, object?>
    {
        ["Feature"] = "Demo",
        ["CorrelationId"] = Guid.NewGuid().ToString("n")
    }
};

await AiTrace.AiTrace.LogDecisionAsync(decision);

// ---- Paths ----
var baseDir = AppContext.BaseDirectory;
var auditDir = Path.Combine(baseDir, "aitrace");

Console.WriteLine("Logged audit record.");
Console.WriteLine($"Audit directory: {auditDir}");

// ---- Verify (integrity + signature) ----
var publicKeyPem = File.ReadAllText(@"C:\temp\aitrace_public.pem");
var sigOpts = new SignatureOptions
{
    SignatureService = new RsaAuditSignatureService(publicKeyPem)
};

var verifier = new ChainVerifier(sigOpts);
var summary = verifier.VerifySummary(auditDir, signatureRequired: true);

Console.WriteLine(summary.IsValid
    ? "VERIFY OK (integrity + signature verified)"
    : $"VERIFY FAIL: {summary.Reason}");

Console.WriteLine($"SUMMARY: Status={summary.Status}, Files={summary.FilesVerified}, Signature={summary.SignatureStatus}");

// ---- Export compliance report to disk ----
var reportPath = Path.Combine(auditDir, "compliance_report.txt");

ComplianceReportExporter.WriteTextReport(
    auditDir,
    reportPath,
    verifier,
    signatureRequired: true
);

Console.WriteLine($"Compliance report written to: {reportPath}");
