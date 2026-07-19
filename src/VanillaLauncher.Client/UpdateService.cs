namespace VanillaLauncher.Client;

public enum FileAction { UpToDate, NeedsDownload }

public sealed record FilePlanItem(ManifestFileEntry Entry, FileAction Action);

public sealed class UpdateService
{
    private readonly string _profileRoot;

    public UpdateService(string profileRoot)
    {
        _profileRoot = profileRoot;
    }

    public async Task<List<FilePlanItem>> BuildPlanAsync(Manifest manifest, CancellationToken ct = default)
    {
        var plan = new List<FilePlanItem>();

        foreach (var entry in manifest.Files)
        {
            var localPath = Path.Combine(_profileRoot, entry.Path);

            if (!File.Exists(localPath))
            {
                plan.Add(new FilePlanItem(entry, FileAction.NeedsDownload));
                continue;
            }

            var localHash = await HashService.ComputeSha256Async(localPath, ct);
            var action = HashService.Matches(localHash, entry.Sha256)
                ? FileAction.UpToDate
                : FileAction.NeedsDownload;

            plan.Add(new FilePlanItem(entry, action));
        }

        return plan;
    }
}
