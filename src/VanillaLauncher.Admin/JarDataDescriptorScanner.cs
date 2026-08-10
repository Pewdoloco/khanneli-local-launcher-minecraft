namespace VanillaLauncher.Admin;

/// <summary>
/// Ищет .jar-файлы с ZIP-записью, у которой выставлен общий флаг "есть data descriptor" (бит 3),
/// но метод сжатия — STORED, а не DEFLATED. По спецификации ZIP этот флаг допустим только для
/// DEFLATED-записей; <c>java.util.zip.ZipInputStream</c> (его использует <c>securejarhandler</c>
/// у Forge/NeoForge при сканировании папки mods) на этом строго падает с
/// <c>ZipException: only DEFLATED entries can have EXT descriptor</c> — мгновенно, ДО того как
/// успевает залогировать, какой именно файл виноват (см. реальный инцидент, из-за которого
/// появился этот класс — стектрейс без имени файла).
///
/// Сам jar при этом не обязательно повреждён — так его просто собрал сборщик мода (нередкая
/// особенность некоторых build-инструментов); для строгого Java-ридера это тем не менее фатально.
///
/// Разбирает только Central Directory (в конце файла), не распаковывает содержимое — быстро и не
/// грузит весь jar в память.
/// </summary>
public static class JarDataDescriptorScanner
{
    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const uint CentralDirectoryEntrySignature = 0x02014b50;
    private const int MaxEocdSearchWindow = 66_000; // 22 (EOCD) + макс. 65535 (comment)
    private const int CentralDirectoryEntryFixedSize = 46;
    private const ushort HasDataDescriptorFlag = 0x0008;
    private const ushort StoredCompressionMethod = 0;

    public static IReadOnlyList<string> FindSuspiciousJars(string modsDirectory)
    {
        if (!Directory.Exists(modsDirectory))
            return Array.Empty<string>();

        var result = new List<string>();
        foreach (var jarPath in Directory.EnumerateFiles(modsDirectory, "*.jar"))
        {
            if (HasStoredEntryWithDataDescriptor(jarPath))
                result.Add(Path.GetFileName(jarPath));
        }
        return result;
    }

    private static bool HasStoredEntryWithDataDescriptor(string jarPath)
    {
        try
        {
            using var fs = File.OpenRead(jarPath);
            var tailSize = (int)Math.Min(MaxEocdSearchWindow, fs.Length);
            var tail = new byte[tailSize];
            fs.Seek(-tailSize, SeekOrigin.End);
            fs.ReadExactly(tail, 0, tailSize);

            var eocdRel = FindEocdOffset(tail);
            if (eocdRel < 0)
                return false;

            var cdSize = BitConverter.ToUInt32(tail, eocdRel + 12);
            var cdOffset = BitConverter.ToUInt32(tail, eocdRel + 16);
            var entryCount = BitConverter.ToUInt16(tail, eocdRel + 10);

            fs.Seek(cdOffset, SeekOrigin.Begin);
            var cd = new byte[cdSize];
            fs.ReadExactly(cd, 0, (int)cdSize);

            var pos = 0;
            for (var i = 0; i < entryCount; i++)
            {
                if (pos + CentralDirectoryEntryFixedSize > cd.Length)
                    break;

                var signature = BitConverter.ToUInt32(cd, pos);
                if (signature != CentralDirectoryEntrySignature)
                    break;

                var generalPurposeFlag = BitConverter.ToUInt16(cd, pos + 8);
                var compressionMethod = BitConverter.ToUInt16(cd, pos + 10);
                var fileNameLength = BitConverter.ToUInt16(cd, pos + 28);
                var extraLength = BitConverter.ToUInt16(cd, pos + 30);
                var commentLength = BitConverter.ToUInt16(cd, pos + 32);

                var hasDataDescriptor = (generalPurposeFlag & HasDataDescriptorFlag) != 0;
                var isStored = compressionMethod == StoredCompressionMethod;
                if (hasDataDescriptor && isStored)
                    return true;

                pos += CentralDirectoryEntryFixedSize + fileNameLength + extraLength + commentLength;
            }

            return false;
        }
        catch
        {
            // Не валидный/нечитаемый zip — не наша забота здесь: смоук-тест и так упадёт на
            // старте сервера с более информативной ошибкой, если дело именно в этом файле.
            return false;
        }
    }

    private static int FindEocdOffset(byte[] tail)
    {
        for (var i = tail.Length - 22; i >= 0; i--)
        {
            if (BitConverter.ToUInt32(tail, i) == EndOfCentralDirectorySignature)
                return i;
        }
        return -1;
    }
}
