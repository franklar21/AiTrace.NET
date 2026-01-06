using System.Text;

namespace AiTrace.Pro.Licensing;

public static class LicenseLoader
{
    public static string? LoadRawLicense()
    {
        // 1) Env var
        var env = Environment.GetEnvironmentVariable("AITRACE_PRO_LICENSE");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        // 2) Fichier à côté de l'app
        var baseDir = AppContext.BaseDirectory;
        var path1 = Path.Combine(baseDir, "aitrace.license");
        if (File.Exists(path1))
            return File.ReadAllText(path1, Encoding.UTF8).Trim();

        // 3) Fichier dans profil utilisateur (optionnel)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path2 = Path.Combine(localAppData, "AiTrace", "aitrace.license");
        if (File.Exists(path2))
            return File.ReadAllText(path2, Encoding.UTF8).Trim();

        return null;
    }
}
