using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AiTrace.Tests;

public class AiTraceTests
{
    [Fact]
    public async Task LogDecision_Writes_AuditFile()
    {
        // Arrange
        _ = NewTempDirAndSetCwd();

        AiTrace.Configure(o =>
        {
            o.StoreContent = true;
            o.BasicRedaction = false;
        });

        var before = DateTime.UtcNow;

        // Act
        await AiTrace.LogDecisionAsync(new AiDecision
        {
            Prompt = "Hello",
            Output = "World",
            Model = "test-model",
            UserId = "user-1",
            Metadata = new Dictionary<string, object?>
            {
                ["CorrelationId"] = "abc"
            }
        });

        // Assert
        var files = FindAuditJsonCreatedAfter(before).ToArray();

        Assert.True(
            files.Length > 0,
            "Expected at least one audit JSON file to be written, but none was found.\n" +
            "Searched roots:\n" + string.Join("\n", GetCandidateRoots().Select(r => " - " + r))
        );
    }

    [Fact]
    public async Task Same_Decision_Produces_Same_Hash()
    {
        // Arrange
        _ = NewTempDirAndSetCwd();

        AiTrace.Configure(o =>
        {
            o.StoreContent = true;
            o.BasicRedaction = false;
        });

        var decision = new AiDecision
        {
            Prompt = "P",
            Output = "O",
            Model = "test-model",
            UserId = "user-1",
            Metadata = new Dictionary<string, object?>
            {
                ["K"] = "V"
            }
        };

        var before = DateTime.UtcNow;

        // Act
        await AiTrace.LogDecisionAsync(decision);
        await AiTrace.LogDecisionAsync(decision);

        // Assert
        var files = FindAuditJsonCreatedAfter(before)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        Assert.True(
            files.Length >= 2,
            $"Expected at least 2 audit JSON files but found {files.Length}.\n" +
            "Searched roots:\n" + string.Join("\n", GetCandidateRoots().Select(r => " - " + r))
        );

        var json1 = await File.ReadAllTextAsync(files[0]);
        var json2 = await File.ReadAllTextAsync(files[1]);

        Assert.Contains("\"HashSha256\":", json1);
        Assert.Contains("\"HashSha256\":", json2);

        var h1 = ExtractJsonValue(json1, "HashSha256");
        var h2 = ExtractJsonValue(json2, "HashSha256");

        Assert.False(string.IsNullOrWhiteSpace(h1));
        Assert.False(string.IsNullOrWhiteSpace(h2));

        Assert.Equal(h1, h2);
    }

    [Fact]
    public async Task Two_Decisions_Are_Persisted_Either_As_Two_Files_Or_One_Updated_File()
    {
        // Arrange
        _ = NewTempDirAndSetCwd();

        AiTrace.Configure(o =>
        {
            o.StoreContent = true;
            o.BasicRedaction = false;
        });

        var before = DateTime.UtcNow;

        // Act
        await AiTrace.LogDecisionAsync(new AiDecision
        {
            Prompt = "P1",
            Output = "O1",
            Model = "test-model",
            UserId = "user-1",
        });

        // Snapshot after first write
        var filesAfter1 = FindAuditJsonCreatedAfter(before)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(filesAfter1.Length >= 1, "Expected at least one audit JSON file after first decision.");

        var file1 = filesAfter1
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();

        var json1 = await File.ReadAllTextAsync(file1);

        // Second decision
        await AiTrace.LogDecisionAsync(new AiDecision
        {
            Prompt = "P2",
            Output = "O2",
            Model = "test-model",
            UserId = "user-1",
        });

        var filesAfter2 = FindAuditJsonCreatedAfter(before)
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        Assert.True(filesAfter2.Length >= 1, "Expected at least one audit JSON file after second decision.");

        // Case A: we now have >= 2 distinct files -> compare IDs (or timestamps)
        if (filesAfter2.Length >= 2)
        {
            var fA = filesAfter2[0];
            var fB = filesAfter2[1];

            var jA = await File.ReadAllTextAsync(fA);
            var jB = await File.ReadAllTextAsync(fB);

            var idA = ExtractJsonValue(jA, "Id");
            var idB = ExtractJsonValue(jB, "Id");

            Assert.False(string.IsNullOrWhiteSpace(idA));
            Assert.False(string.IsNullOrWhiteSpace(idB));
            Assert.NotEqual(idA, idB);

            // sanity: hashes present
            var hA = ExtractJsonValue(jA, "HashSha256");
            var hB = ExtractJsonValue(jB, "HashSha256");
            Assert.False(string.IsNullOrWhiteSpace(hA));
            Assert.False(string.IsNullOrWhiteSpace(hB));
            return;
        }

        // Case B: only one file is used/overwritten -> ensure it changed after 2nd decision
        var onlyFile = filesAfter2[0];
        var json2 = await File.ReadAllTextAsync(onlyFile);

        Assert.NotEqual(json1, json2); // file content must change between decision 1 and 2

        // sanity: still has Id + Hash
        var id2 = ExtractJsonValue(json2, "Id");
        var h2 = ExtractJsonValue(json2, "HashSha256");
        Assert.False(string.IsNullOrWhiteSpace(id2));
        Assert.False(string.IsNullOrWhiteSpace(h2));
    }


    // ---------------------------
    // Helpers
    // ---------------------------

    private static string NewTempDirAndSetCwd()
    {
        var dir = Path.Combine(Path.GetTempPath(), "AiTraceTests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(dir);
        Directory.SetCurrentDirectory(dir);
        return dir;
    }

    private static IEnumerable<string> GetCandidateRoots()
    {
        var cwd = Directory.GetCurrentDirectory();
        var baseDir = AppContext.BaseDirectory;
        var temp = Path.GetTempPath();

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localAiTrace = Path.Combine(localAppData, "aitrace");

        var cwdAiTrace = Path.Combine(cwd, "aitrace");
        var baseAiTrace = Path.Combine(baseDir, "aitrace");

        var roots = new[]
        {
            cwdAiTrace,
            baseAiTrace,
            cwd,
            baseDir,
            localAiTrace,
            temp
        };

        return roots
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindAuditJsonCreatedAfter(DateTime utc)
    {
        foreach (var root in GetCandidateRoots())
        {
            if (!Directory.Exists(root))
                continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var f in files)
            {
                DateTime t;
                try
                {
                    t = File.GetLastWriteTimeUtc(f);
                }
                catch
                {
                    continue;
                }

                if (t >= utc.AddSeconds(-2))
                    yield return f;
            }
        }
    }

    private static string ExtractJsonValue(string json, string key)
    {
        var needle = $"\"{key}\":";
        var i = json.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return "";

        i += needle.Length;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

        if (i >= json.Length || json[i] != '"') return "";
        i++;

        var j = json.IndexOf('"', i);
        if (j < 0) return "";

        return json.Substring(i, j - i);
    }
}
