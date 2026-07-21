namespace VanillaLauncher.Client;

/// <summary>
/// Нормализует значение поля GitHubRepo/EngineGitHubRepo, введённое человеком в "Настройках".
/// GitHubReleaseClient/EngineSelfUpdater строят URL вида
/// api.github.com/repos/{owner}/{repo}/... из голого имени репозитория — если туда попадёт
/// целая ссылка вида "https://github.com/Owner/Repo.git" (частая опечатка: поле называется
/// "репозиторий", и её естественно перепутать со ссылкой на него), запрос уходит на
/// несуществующий путь и GitHub отвечает 404 без внятной причины (было ровно так с
/// GitHubRepo = "https://github.com/Pewdoloco/test-pack.git"). Нормализация вырезает из
/// такого значения голое имя репозитория, чтобы поле продолжало работать независимо от того,
/// что именно в него вставили.
/// </summary>
public static class GitHubRepoNameNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim().TrimEnd('/');

        if (trimmed.Contains('/'))
        {
            var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
            trimmed = segments[^1];
        }

        if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^".git".Length];

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
