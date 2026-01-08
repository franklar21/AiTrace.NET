using AiTrace;
using AiTrace.Pro;
using AiTrace.Pro.Signing;
using AiTrace.Pro.Stores;

AiTrace.AiTrace.Configure(o =>
{
    o.StoreContent = true;
    o.BasicRedaction = true;

    var privateKeyPem = File.ReadAllText(@"C:\temp\aitrace_private.pem");
    var signer = new RsaAuditSignatureService(privateKeyPem);

    // Pro store that computes PrevHash + Hash THEN signs THEN writes
    o.Store = new SignedJsonAuditStore(signer);

});

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

Console.WriteLine("Logged. Check the ./aitrace folder next to the executable.");
Console.WriteLine($"Base directory: {AppContext.BaseDirectory}");

// --- Pro verification (temporary test) ---
var auditDir = Path.Combine(AppContext.BaseDirectory, "aitrace");
var result = AiTracePro.Verify(auditDir);

Console.WriteLine(result.IsValid
    ? "VERIFY OK (integrity + signature verified)"
    : $"VERIFY FAIL: {result.Reason}");
