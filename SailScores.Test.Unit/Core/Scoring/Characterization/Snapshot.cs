using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// Approved-snapshot comparison. Approved output is Snapshots/<name>.verified.json beside the
// test source. When the output differs, or there is no approved file yet, the test writes
// <name>.received.json next to it and fails; diff the two and, if the new output is right,
// replace the .verified.json file with it.
//
// Set SAILSCORES_ACCEPT_SNAPSHOTS=1 to write every result straight to .verified.json, for
// adding fixtures or accepting an intended change. Review the diff before committing.
public static class Snapshot
{
    private const string AcceptVariable = "SAILSCORES_ACCEPT_SNAPSHOTS";

    public static async Task MatchAsync(
        string actual,
        string name,
        string subdirectory = null,
        [CallerFilePath] string callerFile = "")
    {
        var directory = Path.Combine(Path.GetDirectoryName(callerFile)!, "Snapshots", subdirectory ?? "");
        Directory.CreateDirectory(directory);
        var verifiedPath = Path.Combine(directory, name + ".verified.json");
        var receivedPath = Path.Combine(directory, name + ".received.json");
        actual = Normalize(actual);

        if (Environment.GetEnvironmentVariable(AcceptVariable) == "1")
        {
            await File.WriteAllTextAsync(verifiedPath, actual);
            File.Delete(receivedPath);
            return;
        }

        var verified = File.Exists(verifiedPath) ? Normalize(await File.ReadAllTextAsync(verifiedPath)) : null;
        if (verified == actual)
        {
            File.Delete(receivedPath);
            return;
        }

        await File.WriteAllTextAsync(receivedPath, actual);
        Assert.Fail(verified == null
            ? $"No approved snapshot for {name}. Review {receivedPath} and rename it to {Path.GetFileName(verifiedPath)}, "
                + $"or run with {AcceptVariable}=1."
            : $"Scoring output for {name} changed. Diff {receivedPath} against {verifiedPath}. Accept only intended changes.");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";
}
