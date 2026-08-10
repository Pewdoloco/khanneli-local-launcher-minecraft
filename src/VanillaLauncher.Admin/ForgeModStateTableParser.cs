namespace VanillaLauncher.Admin;

/// <summary>
/// Ищет в выводе сервера строки отчёта Forge/NeoForge о загрузке модов — таблицу вида
/// <c>ИмяФайла.jar |DisplayName |modid |version |СТАТУС |доп. инфо</c>, которую сам загрузчик
/// печатает при падении. Строка с колонкой <c>ERROR</c> — прямое указание Forge на конкретный
/// виновный мод, самый надёжный источник (не эвристика одного класса краша, как
/// <see cref="JarDataDescriptorScanner"/>), но появляется только если сервер дошёл до этой
/// стадии загрузки (после первичного сканирования директории mods).
/// </summary>
public static class ForgeModStateTableParser
{
    public static IReadOnlyList<string> FindFailedModJars(IEnumerable<string> lines)
    {
        var result = new List<string>();

        foreach (var line in lines)
        {
            var columns = line.Split('|');
            if (columns.Length < 2)
                continue;

            var jarFileName = columns[0].Trim();
            if (!jarFileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                continue;

            var hasErrorStatus = columns.Skip(1).Any(c => c.Trim().Equals("ERROR", StringComparison.OrdinalIgnoreCase));
            if (hasErrorStatus)
                result.Add(jarFileName);
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
