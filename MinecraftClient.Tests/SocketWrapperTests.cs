using System.Net;
using System.Net.Sockets;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Tests;

public sealed class SocketWrapperTests
{
    [Fact]
    public async Task GracefulPeerCloseEndsReadInsteadOfSpinning()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            using var client = new TcpClient();
            Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
            await client.ConnectAsync((IPEndPoint)listener.LocalEndpoint);
            using TcpClient peer = await acceptTask;
            var wrapper = new SocketWrapper(client);

            peer.Client.Shutdown(SocketShutdown.Both);
            peer.Close();

            Assert.True(SpinWait.SpinUntil(wrapper.HasDataAvailable, TimeSpan.FromSeconds(5)));
            await Assert.ThrowsAsync<EndOfStreamException>(
                () => Task.Run(() => wrapper.ReadDataRAW(1)).WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            listener.Stop();
        }
    }
}
