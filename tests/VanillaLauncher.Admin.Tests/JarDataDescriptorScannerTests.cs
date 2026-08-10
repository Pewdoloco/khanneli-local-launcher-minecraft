using VanillaLauncher.Admin;
using Xunit;

namespace VanillaLauncher.Admin.Tests;

public class JarDataDescriptorScannerTests : IDisposable
{
    private readonly string _dir;

    public JarDataDescriptorScannerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vlc-jarscan-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }
    }

    private void WriteZip(string fileName, ushort generalPurposeFlag, ushort compressionMethod) =>
        File.WriteAllBytes(
            Path.Combine(_dir, fileName),
            ZipTestHelper.BuildSingleEntryZip("test.txt", generalPurposeFlag, compressionMethod));

    [Fact]
    public void FindSuspiciousJars_FlagsStoredEntryWithDataDescriptor()
    {
        WriteZip("bad.jar", generalPurposeFlag: 0x0008, compressionMethod: 0); // STORED + дескриптор

        var result = JarDataDescriptorScanner.FindSuspiciousJars(_dir);

        Assert.Contains("bad.jar", result);
    }

    [Fact]
    public void FindSuspiciousJars_IgnoresStoredEntryWithoutDataDescriptor()
    {
        WriteZip("ok-stored.jar", generalPurposeFlag: 0x0000, compressionMethod: 0);

        var result = JarDataDescriptorScanner.FindSuspiciousJars(_dir);

        Assert.Empty(result);
    }

    [Fact]
    public void FindSuspiciousJars_IgnoresDeflatedEntryWithDataDescriptor()
    {
        // DEFLATED + data descriptor — легальная, обычная комбинация (типичный "streamed" zip),
        // не должна считаться подозрительной.
        WriteZip("ok-deflate-streamed.jar", generalPurposeFlag: 0x0008, compressionMethod: 8);

        var result = JarDataDescriptorScanner.FindSuspiciousJars(_dir);

        Assert.Empty(result);
    }

    [Fact]
    public void FindSuspiciousJars_ScansOnlyMatchingFileInMixedFolder()
    {
        WriteZip("bad.jar", generalPurposeFlag: 0x0008, compressionMethod: 0);
        WriteZip("ok.jar", generalPurposeFlag: 0x0000, compressionMethod: 0);

        var result = JarDataDescriptorScanner.FindSuspiciousJars(_dir);

        Assert.Single(result);
        Assert.Equal("bad.jar", result[0]);
    }

    [Fact]
    public void FindSuspiciousJars_IgnoresGarbageFile_DoesNotThrow()
    {
        File.WriteAllBytes(Path.Combine(_dir, "garbage.jar"), new byte[] { 1, 2, 3, 4, 5 });

        var result = JarDataDescriptorScanner.FindSuspiciousJars(_dir);

        Assert.Empty(result);
    }

    [Fact]
    public void FindSuspiciousJars_MissingDirectory_ReturnsEmpty()
    {
        var result = JarDataDescriptorScanner.FindSuspiciousJars(Path.Combine(_dir, "does-not-exist"));

        Assert.Empty(result);
    }
}
