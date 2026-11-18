using System.Net;
using System.Reflection;
using System.Text.Json;
using Voxta.Abstractions.Chats.Sessions;
using Voxta.Abstractions.Services.Agents;
using Voxta.Model.Shared;
using Voxta.Modules.AgenticSims;

var root = Path.Combine(Path.GetTempPath(), "agentic-module-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var count = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
JsonElement Args(string value = "{}") => JsonDocument.Parse(value).RootElement.Clone();
async Task Reject(Func<Task> action, string description)
{
    try { await action(); throw new Exception("Expected rejection: " + description); }
    catch (BridgeException) { count++; }
}
void Grant(bool enabled = true, string token = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") =>
    File.WriteAllText(Path.Combine(root, "remote-bridge.json"), JsonSerializer.Serialize(new { enabled, token }));

try
{
    Grant();
    var handler = new FakeHandler();
    using var client = new BridgeClient(root, 5390, handler);
    var result = await client.CallAsync("agentic_sims_status", Args(), default);
    Check(result.GetProperty("game").GetString() == "running", "status returned");
    Check(handler.Uri == "http://127.0.0.1:5390/api/remote/bridge", "fixed loopback only");
    Check(handler.Body.Contains("agentic_sims_status"), "bridge envelope");
    Grant(token: new string('b', 43));
    await client.CallAsync("agentic_sims_status", Args(), default);
    Check(handler.Key == new string('b', 43), "key rotation read on next call");
    Grant(false);
    var before = handler.Calls;
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "revoked grant");
    Check(handler.Calls == before, "no request with disabled grant");
    Grant();
    foreach (var (name, arguments) in new[] {
        ("unknown", "{}"), ("agentic_sims_inspect", "{}"), ("agentic_sims_status", "{\"url\":\"https://example.com\"}"),
        ("agentic_sims_inspect", "{\"sim_id\":1}"), ("agentic_sims_inspect", "{\"sim_id\":\"a\",\"sim_id\":\"b\"}"),
        ("agentic_sims_receipt", "{\"request_id\":\"invalid\"}"), ("agentic_sims_command", JsonSerializer.Serialize(new { text = new string('x', 301) })) })
        await Reject(() => client.CallAsync(name, Args(arguments), default), "argument validation");
    handler.Status = HttpStatusCode.Redirect;
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "redirect rejected");
    handler.Status = HttpStatusCode.Forbidden;
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "server revocation");
    handler.Status = HttpStatusCode.OK;
    handler.Response = new string('x', 65537);
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "bounded response");
    handler.Response = "[]";
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "invalid response type");
    File.WriteAllText(Path.Combine(root, "remote-bridge.json"), "null");
    await Reject(() => client.CallAsync("agentic_sims_status", Args(), default), "malformed grant");
    Grant();
    handler.Response = FakeHandler.StatusResponse;
    var session = DispatchProxy.Create<IChatSessionChatAugmentationApi, SessionProxy>();
    var proxy = (SessionProxy)(object)session;
    await using var instance = new AgenticSimsInstance(session, client, false);
    await instance.RefreshAsync(default);
    Check(proxy.Context.Contains("running"), "live context injected");
    Grant(false);
    await instance.PreProcessTextAsync(ChatMessageRole.User, "hello", default);
    Check(proxy.Context.Contains("unavailable") && !proxy.Context.Contains("\"game\":\"running\""), "revocation replaces stale context");
    Check(proxy.Updates == 2, "one refresh per user turn");
    await instance.PreProcessTextAsync(ChatMessageRole.Assistant, "hi", default);
    Check(proxy.Updates == 2, "assistant reply does not refresh or loop");
    var tools = await instance.GetToolsAsync(default);
    Check(tools.Count == 6, "six tools registered");
    Check(tools.Where(t => t.Permission == ToolPermissionClass.Act).All(t => !t.Enabled && !t.AlwaysAllowed), "writes off by default");
    before = handler.Calls;
    await instance.CallToolAsync("agentic_sims_request_action", Args(), default);
    Check(handler.Calls == before, "disabled actions cannot bypass through direct call");
    Grant();
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { await client.CallAsync("agentic_sims_status", Args(), cancellation.Token); throw new Exception("Cancellation swallowed"); }
    catch (OperationCanceledException) { count++; }
    Console.WriteLine($"Passed {count} native module checks.");
}
finally { Directory.Delete(root, true); }

sealed class FakeHandler : HttpMessageHandler
{
    public const string StatusResponse = "{\"game\":\"running\",\"sims\":[{\"id\":\"123\",\"name\":\"Test Sim\"}],\"mode\":\"monitor\"}";
    public string Response = StatusResponse;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string? Key, Uri;
    public string Body = "";
    public int Calls;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); Calls++;
        Key = request.Headers.Authorization?.Parameter; Uri = request.RequestUri?.ToString();
        Body = await request.Content!.ReadAsStringAsync(ct);
        return new HttpResponseMessage(Status) { Content = new StringContent(Response) };
    }
}

public class SessionProxy : DispatchProxy
{
    public string Context = "";
    public int Updates;
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method?.Name == "SetContexts")
        {
            Context = JsonSerializer.Serialize(args![1]); Updates++;
            if (method.ReturnType == typeof(ValueTask)) return ValueTask.CompletedTask;
            return Task.CompletedTask;
        }
        throw new NotSupportedException(method?.Name);
    }
}
