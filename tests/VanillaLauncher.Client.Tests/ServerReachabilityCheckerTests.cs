using System.Net;
using System.Net.Sockets;
using VanillaLauncher.Client;
using Xunit;

namespace VanillaLauncher.Client.Tests;

public class ServerReachabilityCheckerTests
{
    [Fact]
    public async Task IsReachableAsync_ReturnsTrue_WhenPortIsListening()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        _ = listener.AcceptTcpClientAsync();

        try
        {
            var checker = new ServerReachabilityChecker();
            var reachable = await checker.IsReachableAsync("127.0.0.1", port, TimeSpan.FromSeconds(5));

            Assert.True(reachable);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task IsReachableAsync_ReturnsFalse_WhenConnectionIsRefused()
    {
        // Занимаем и сразу освобождаем порт — гарантированно никто не слушает именно на нём,
        // ОС ответит немедленным отказом (не таймаутом), тест остаётся быстрым.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var checker = new ServerReachabilityChecker();
        var reachable = await checker.IsReachableAsync("127.0.0.1", port, TimeSpan.FromSeconds(5));

        Assert.False(reachable);
    }
}
