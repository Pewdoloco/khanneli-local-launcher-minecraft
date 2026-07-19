using System.IO.Compression;

namespace VanillaLauncher.Admin;

/// <summary>
/// Бэкап и пересоздание папки мира сервера. Пересоздание без бэкапа недопустимо
/// (см. SPEC.md) — удаление возможно только через RecreateWorld, который сначала бэкапит.
/// </summary>
public sealed class WorldBackupService
{
    private readonly string _serverDirectory;
    private readonly string _backupsDirectory;
    private readonly int _maxBackupsToKeep;

    public WorldBackupService(string serverDirectory, string backupsDirectory, int maxBackupsToKeep)
    {
        if (maxBackupsToKeep < 1)
            throw new ArgumentOutOfRangeException(nameof(maxBackupsToKeep), "Нужно хранить хотя бы 1 бэкап.");

        _serverDirectory = serverDirectory;
        _backupsDirectory = backupsDirectory;
        _maxBackupsToKeep = maxBackupsToKeep;
    }

    /// <summary>
    /// Архивирует папку мира в backups/&lt;levelName&gt;_&lt;timestamp&gt;.zip и удаляет
    /// самые старые бэкапы сверх лимита. Ничего не удаляет из папки мира.
    /// </summary>
    /// <returns>Путь к созданному архиву, либо null, если папки мира не существует (нечего бэкапить).</returns>
    public string? BackupWorld(string levelName)
    {
        var worldPath = Path.Combine(_serverDirectory, levelName);
        if (!Directory.Exists(worldPath))
            return null;

        Directory.CreateDirectory(_backupsDirectory);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss_fff");
        var backupPath = Path.Combine(_backupsDirectory, $"{levelName}_{timestamp}.zip");

        ZipFile.CreateFromDirectory(worldPath, backupPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        RotateBackups(levelName);

        return backupPath;
    }

    /// <summary>
    /// Обязательный бэкап, затем удаление папки мира (сервер при следующем запуске
    /// сгенерирует новый мир на её месте). Вызывающая сторона обязана убедиться,
    /// что сервер остановлен, — эта проверка сюда сознательно не делегирована
    /// (ServerProcessController живёт отдельно от WorldBackupService).
    /// </summary>
    /// <returns>Путь к бэкапу удалённого мира, либо null, если удалять было нечего.</returns>
    /// <remarks>
    /// Синхронный метод — вызывающая сторона (UI) сама решает, выносить ли вызов
    /// в фоновый поток (Task.Run), чтобы не блокировать интерфейс во время архивации.
    /// </remarks>
    public string? RecreateWorld(string levelName)
    {
        var worldPath = Path.Combine(_serverDirectory, levelName);
        var backupPath = BackupWorld(levelName);

        if (Directory.Exists(worldPath))
            Directory.Delete(worldPath, recursive: true);

        return backupPath;
    }

    private void RotateBackups(string levelName)
    {
        var existing = Directory.GetFiles(_backupsDirectory, $"{levelName}_*.zip")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        foreach (var stale in existing.Skip(_maxBackupsToKeep))
            stale.Delete();
    }
}
