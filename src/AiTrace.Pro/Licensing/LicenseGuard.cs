namespace AiTrace.Pro.Licensing;

public static class LicenseGuard
{

    public static void EnsureLicensed()
    {
        #if DEBUG
            return;
        #endif

        var raw = LicenseLoader.LoadRawLicense();

        if (raw is null)
            throw new InvalidOperationException(
                "AiTrace.Pro requires a license. " +
                "Set env var AITRACE_PRO_LICENSE or place 'aitrace.license' next to your app.");

        if (!LicenseValidator.TryValidate(raw, out var _, out var reason))
            throw new InvalidOperationException("AiTrace.Pro license is invalid: " + reason);
    }
}
