using System.Net.Sockets;

namespace VanillaLauncher.Client;

/// <summary>
/// TCP-проверка доступности адреса:порта сервера — используется MainWindow перед игрой, чтобы
/// явно сказать игроку "сервер недоступен, проверь Tailscale", а не дать ему упереться в
/// невнятную ошибку подключения уже внутри самого Minecraft. Не завязана на протокол
/// Minecraft (Server List Ping и т.п.) — самого факта установленного TCP-соединения на
/// игровой порт достаточно, чтобы отличить "сеть не достаёт до сервера" от "сеть в порядке".
/// </summary>
public sealed class ServerReachabilityChecker
{
    public async Task<bool> IsReachableAsync(string host, int port, TimeSpan timeout, CancellationToken ct = default)
    {
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            await client.ConnectAsync(host, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
