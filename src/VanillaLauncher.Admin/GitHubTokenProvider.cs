namespace VanillaLauncher.Admin;

public static class GitHubTokenProvider
{
    public const string EnvironmentVariableName = "VANILLALAUNCHER_GITHUB_TOKEN";

    /// <summary>
    /// Токен намеренно не хранится ни в appsettings.json, ни где-либо в репозитории —
    /// только переменная окружения, которую администратор задаёт себе на машине сам.
    /// </summary>
    public static string GetTokenOrThrow()
    {
        var token = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"Не задана переменная окружения {EnvironmentVariableName} — публикация в GitHub Releases невозможна. " +
                "Создай Personal Access Token с правом Contents: Read and write для этого репозитория и пропиши его " +
                $"в переменные окружения Windows под именем {EnvironmentVariableName}.");
        }

        return token;
    }
}
