using System.Net;

namespace VanillaLauncher.Client;

/// <summary>
/// Переводит типовые ошибки GitHub REST API (публикация релиза, самообновление) в понятную
/// администратору подсказку — сырой JSON от GitHub полезен для отладки, но не объясняет,
/// что конкретно поправить в "Настройках". Возвращает null для нераспознанных случаев —
/// вызывающая сторона в этом случае просто показывает сырое сообщение без подсказки, не
/// придумывая объяснение для того, чего не знает наверняка.
/// </summary>
public static class GitHubApiErrorTranslator
{
    public static string? TryGetHint(HttpStatusCode status, string responseBody)
    {
        return (status, responseBody) switch
        {
            (HttpStatusCode.NotFound, _) =>
                "Проверь GitHubOwner/GitHubRepo в «Настройках» — репозиторий не найден. Частая причина: в поле " +
                "вставили ссылку целиком (https://github.com/Owner/Repo) вместо простого имени репозитория (Repo).",

            (HttpStatusCode.Forbidden, var body) when body.Contains("Resource not accessible", StringComparison.OrdinalIgnoreCase) =>
                "У токена VANILLALAUNCHER_GITHUB_TOKEN нет прав на запись в этот репозиторий. Проверь на " +
                "github.com/settings/tokens (или /settings/personal-access-tokens для fine-grained), что токену " +
                "выдано право Contents: Read and write именно на этот репозиторий.",

            (HttpStatusCode.Unauthorized, _) =>
                "Токен VANILLALAUNCHER_GITHUB_TOKEN недействителен или просрочен — проверь переменную окружения " +
                "и при необходимости выпусти новый токен.",

            (HttpStatusCode.UnprocessableEntity, var body) when body.Contains("Repository is empty", StringComparison.OrdinalIgnoreCase) =>
                "Репозиторий модпака полностью пустой — в нём нет ни одного коммита, а релиз GitHub всегда " +
                "привязан к ветке/тегу. Зайди на страницу репозитория на github.com и создай любой файл " +
                "(например README) — после этого публикация заработает.",

            (HttpStatusCode.UnprocessableEntity, var body) when body.Contains("already_exists", StringComparison.OrdinalIgnoreCase)
                || body.Contains("already exists", StringComparison.OrdinalIgnoreCase) =>
                "Релиз с таким номером версии уже существует в репозитории. Впиши другой номер версии в поле " +
                "«Версия релиза» и попробуй снова.",

            _ => null,
        };
    }
}
