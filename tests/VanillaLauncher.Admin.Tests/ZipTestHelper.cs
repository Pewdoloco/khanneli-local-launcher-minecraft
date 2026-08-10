using System.Text;

namespace VanillaLauncher.Admin.Tests;

/// <summary>
/// Собирает минимальный синтетический .zip (local file header + central directory + EOCD) с
/// одной записью нулевой длины — единственный способ протестировать
/// <see cref="VanillaLauncher.Admin.JarDataDescriptorScanner"/> на конкретной ZIP-аномалии,
/// которую .NET-овский System.IO.Compression сам никогда не производит.
/// </summary>
internal static class ZipTestHelper
{
    public static byte[] BuildSingleEntryZip(string entryName, ushort generalPurposeFlag, ushort compressionMethod, byte[]? data = null)
    {
        data ??= Array.Empty<byte>();
        var nameBytes = Encoding.ASCII.GetBytes(entryName);
        const uint crc = 0;

        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write((uint)0x04034b50); // local file header signature
        w.Write((ushort)20);       // version needed to extract
        w.Write(generalPurposeFlag);
        w.Write(compressionMethod);
        w.Write((ushort)0);        // last mod time
        w.Write((ushort)0);        // last mod date
        w.Write(crc);
        w.Write((uint)data.Length);
        w.Write((uint)data.Length);
        w.Write((ushort)nameBytes.Length);
        w.Write((ushort)0);        // extra field length
        w.Write(nameBytes);
        w.Write(data);

        if ((generalPurposeFlag & 0x0008) != 0)
        {
            w.Write((uint)0x08074b50); // (опциональная) сигнатура data descriptor
            w.Write(crc);
            w.Write((uint)data.Length);
            w.Write((uint)data.Length);
        }

        var centralDirOffset = (int)ms.Length;

        w.Write((uint)0x02014b50); // central directory file header signature
        w.Write((ushort)20);       // version made by
        w.Write((ushort)20);       // version needed to extract
        w.Write(generalPurposeFlag);
        w.Write(compressionMethod);
        w.Write((ushort)0);        // last mod time
        w.Write((ushort)0);        // last mod date
        w.Write(crc);
        w.Write((uint)data.Length);
        w.Write((uint)data.Length);
        w.Write((ushort)nameBytes.Length);
        w.Write((ushort)0);        // extra field length
        w.Write((ushort)0);        // comment length
        w.Write((ushort)0);        // disk number start
        w.Write((ushort)0);        // internal attributes
        w.Write((uint)0);          // external attributes
        w.Write((uint)0);          // relative offset of local header
        w.Write(nameBytes);

        var centralDirSize = (int)ms.Length - centralDirOffset;

        w.Write((uint)0x06054b50); // end of central directory signature
        w.Write((ushort)0);        // disk number
        w.Write((ushort)0);        // central directory start disk
        w.Write((ushort)1);        // entries on this disk
        w.Write((ushort)1);        // total entries
        w.Write((uint)centralDirSize);
        w.Write((uint)centralDirOffset);
        w.Write((ushort)0);        // comment length

        w.Flush();
        return ms.ToArray();
    }
}
