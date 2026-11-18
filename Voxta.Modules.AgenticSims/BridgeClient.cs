using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Voxta.Modules.AgenticSims;

public sealed class BridgeException(string message) : Exception(message);

/// <summary>Only the existing player-scoped loopback API; never accepts an AI-selected URL or file.</summary>
public sealed class BridgeClient : IDisposable
{
    public const int MaxBytes = 65536;
    private readonly string configPath;
    private readonly Uri endpoint;
    private readonly HttpClient http;

    public BridgeClient(string runtimeFolder, int port, HttpMessageHandler? handler = null)
    {
        if (!Path.IsPathFullyQualified(runtimeFolder) || port is < 1024 or > 65535)
            throw new BridgeException("Choose an absolute Agentic Sims runtime folder and a port between 1024 and 65535.");
        configPath = Path.Combine(runtimeFolder, "remote-bridge.json");
        endpoint = new Uri($"http://127.0.0.1:{port}/api/remote/bridge");
        http = new HttpClient(handler ?? new SocketsHttpHandler { UseProxy = false, AllowAutoRedirect = false })
            { Timeout = TimeSpan.FromSeconds(12) };
    }

    public async Task<JsonElement> CallAsync(string name, JsonElement arguments, CancellationToken ct)
    {
        ToolCatalog.Validate(name, arguments);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(12));
        var operation = timeout.Token;
        try
        {
            // Saving/revoking access rotates the key. Never cache it or place it in a prompt or settings form.
            using var file = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var config = JsonDocument.Parse(await ReadBoundedAsync(file, operation));
            var root = config.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("enabled", out var enabled) || enabled.ValueKind != JsonValueKind.True ||
                !root.TryGetProperty("token", out var key) || key.ValueKind != JsonValueKind.String ||
                key.GetString() is not { Length: >= 32 and <= 256 } token)
                throw new BridgeException("Enable access and select Sims in Agentic Sims → Remote Sims.");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new { name, arguments });
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operation);
            if (!response.IsSuccessStatusCode)
                throw new BridgeException(response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                    ? "Access was revoked. Check Remote Sims in Agentic Sims."
                    : "The companion rejected the request. Review permissions, scene and request receipts locally; no outcome is confirmed.");
            await using var stream = await response.Content.ReadAsStreamAsync(operation);
            using var document = JsonDocument.Parse(await ReadBoundedAsync(stream, operation));
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new BridgeException("The companion returned invalid data.");
            return document.RootElement.Clone();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (BridgeException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or HttpRequestException or OperationCanceledException or FormatException)
        {
            // Do not expose HTTP headers, paths or exception bodies to the model/logs.
            throw new BridgeException("Agentic Sims is unavailable or not configured. No action outcome is confirmed. Check the local app and receipts before retrying.");
        }
    }

    internal static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken ct)
    {
        using var output = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (output.Length + count > MaxBytes) throw new BridgeException("Bridge data exceeded its size limit. Narrow the request.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    public void Dispose() => http.Dispose();
}
