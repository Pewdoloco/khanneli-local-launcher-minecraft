namespace VanillaLauncher.Client;

/// <summary>
/// Версия самого движка (не модпака) — сверяется с тегами <c>engine-vX.Y.Z</c> в
/// EngineGitHubOwner/EngineGitHubRepo при самообновлении (см. EngineSelfUpdater).
///
/// Обновляется вручную при каждом релизе движка, вместе с пушем тега engine-vX.Y.Z —
/// CI (.github/workflows/engine-release.yml) сверяет эти два значения и проваливает
/// публикацию при расхождении, чтобы забытое обновление константы не сделало
/// самообновление тихо бесполезным (или наоборот, не предложило откат на старую версию).
/// </summary>
public static class EngineVersion
{
    public const string Current = "1.1.0";
}
