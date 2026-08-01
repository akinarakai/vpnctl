using System.Diagnostics;
using System.Net.Http.Json;

public class Client
{
    private readonly HttpClient _http;

    public Client(HttpClient http)
    {
        _http = http;
    }

    public ServerInfo GetServerInfo()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = SendGet<ServerInfoResponse>(ApiRoutes.Server.Info);
        stopwatch.Stop();

        return new ServerInfo
        {
            Response = response,
            LatencyMs = stopwatch.ElapsedMilliseconds
        };
    }

    public bool TryGetServerInfo(out ServerInfo? info)
    {
        try
        {
            info = GetServerInfo();
            return true;
        }
        catch
        {
            info = null;
            return false;
        }
    }

    public SystemMonitorResponse GetSystemMonitor()
    {
        return SendGet<SystemMonitorResponse>(ApiRoutes.System.Monitor);
    }

    public VpnLogsResponse GetVpnLogs(int lines)
    {
        return SendGet<VpnLogsResponse>($"{ApiRoutes.Vpn.Logs}?lines={lines}");
    }

    public VpnListResponse GetVpns(VpnServiceType? type = null)
    {
        var url = type == null
            ? ApiRoutes.Vpn.List
            : $"{ApiRoutes.Vpn.List}?type={type}";

        return SendGet<VpnListResponse>(url);
    }

    public ClientsResponse GetClients(string? name = null)
    {
        var url = string.IsNullOrEmpty(name)
            ? ApiRoutes.Clients.List
            : $"{ApiRoutes.Clients.List}?name={name}";

        return SendGet<ClientsResponse>(url);
    }

    public void SendVpnAction(VpnServiceType type, VpnNetActionType action)
    {
        var request = new VpnActionRequest
        {
            Type = type,
            Action = action,
        };

        SendPostJson(ApiRoutes.Vpn.Action, request);
    }

    public ClientNetData SendClientAction(ClientActionRequest request)
    {
        return SendPostJson<ClientActionRequest, ClientNetData>(ApiRoutes.Clients.Action, request);
    }

    public void SendProtocolAction(ProtocolType type, ProtocolNetActionType action, string? value = null)
    {
        var request = new ProtocolActionRequest
        {
            Type = type,
            Action = action,
            Value = value,
        };

        SendPostJson(ApiRoutes.Protocols.Action, request);
    }

    public void SendPurge()
    {
        SendPost(ApiRoutes.Maintenance.Purge);
    }

    private TResponse SendPostJson<TRequest, TResponse>(string route, TRequest body)
    {
        var response = _http.PostAsJsonAsync(route, body).Result;

        return ReadResponse<TResponse>(response);
    }

    private void SendPostJson<T>(string route, T body)
    {
        var response = _http.PostAsJsonAsync(route, body).Result;

        ReadEmptyResponse(response);
    }

    private void SendPost(string route)
    {
        var response = _http.PostAsync(route, null).Result;

        ReadEmptyResponse(response);
    }

    private T SendGet<T>(string route)
    {
        var response = _http.GetAsync(route).Result;

        return ReadResponse<T>(response);
    }

    private T ReadResponse<T>(HttpResponseMessage response)
    {
        var result = response.Content.ReadFromJsonAsync<ApiResponse<T>>().Result;

        if (result == null)
            throw new ApiErrorException((int)response.StatusCode, "Empty response");

        if (!result.Success)
            throw new ApiErrorException((int)response.StatusCode, result.Error ?? "Unknown api error");

        if (result.Data == null)
            throw new ApiErrorException((int)response.StatusCode, "Response data is empty");

        return result.Data;
    }

    private void ReadEmptyResponse(HttpResponseMessage response)
    {
        var result = response.Content.ReadFromJsonAsync<ApiResponse<object>>().Result;

        if (result == null)
            throw new ApiErrorException((int)response.StatusCode, "Empty response");

        if (!result.Success)
            throw new ApiErrorException((int)response.StatusCode, result.Error ?? "Unknown api error");
    }
}