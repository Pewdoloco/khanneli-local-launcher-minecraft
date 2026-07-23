namespace VanillaLauncher.Client;

/// <summary>
/// Канонический репозиторий ДВИЖКА — используется как фоллбек, когда
/// EngineGitHubOwner/EngineGitHubRepo не заданы в конфиге, чтобы самообновление exe
/// работало "из коробки" для любого модпака на этом движке без отдельной настройки этих
/// двух полей. Кто-то форкнувший движок себе может переопределить оба поля через
/// "Настройки" — фоллбек применяется только когда поля пустые, существующее значение
/// никогда не перезаписывается.
/// </summary>
public static class EngineRepositoryDefaults
{
    public const string Owner = "Pewdoloco";
    public const string Repo = "khanneli-local-launcher-minecraft";
}
