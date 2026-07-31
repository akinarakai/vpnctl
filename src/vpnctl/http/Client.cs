using System.Net.Http.Json;

public class Client
{
    private readonly HttpClient _http;

    public Client(HttpClient http)
    {
        _http = http;
    }

    public StatusResponse GetStatus()
    {
        return SendGet<StatusResponse>(ApiRoutes.Status);
    }

    public VpnLogsResponse GetVpnLogs(int lines)
    {
        return SendGet<VpnLogsResponse>($"{ApiRoutes.VpnLogs}?lines={lines}");
    }

    public ClientsResponse GetClients(string? name = null)
    {
        var url = string.IsNullOrEmpty(name) ? ApiRoutes.Clients : $"{ApiRoutes.Clients}?name={name}";
        return SendGet<ClientsResponse>(url);
    }

    public void SendVpnAction(VpnServiceType type, VpnNetActionType action)
    {
        var request = new VpnActionRequest
        {
            Type = type,
            Action = action,
        };

        SendPostJson(ApiRoutes.VpnAction, request);
    }

    public ClientNetData SendClientAction(ClientActionRequest request)
    {
        return SendPostJson<ClientActionRequest, ClientNetData>(ApiRoutes.ClientAction, request);
    }

    public void SendProtocolAction(ProtocolType type, ProtocolNetActionType action, string? value = null)
    {
        var request = new ProtocolActionRequest
        {
            Type = type,
            Action = action,
            Value = value,
        };

        SendPostJson(ApiRoutes.ClientAction, request);
    }

    public void SendPurge()
    {
        SendPost(ApiRoutes.Purge);
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
        var result = response.Content
            .ReadFromJsonAsync<ApiResponse<T>>()
            .Result;

        if (result == null)
            throw new Exception("Empty response");

        if (!result.Success)
            throw new Exception(
                $"Api error [{response.StatusCode}]: {result.Error}"
            );

        if (result.Data == null)
            throw new Exception("Response data is empty");

        return result.Data;
    }

    private void ReadEmptyResponse(HttpResponseMessage response)
    {
        var result = response.Content
            .ReadFromJsonAsync<ApiResponse<object>>()
            .Result;

        if (result == null)
            throw new Exception("Empty response");

        if (!result.Success)
            throw new Exception(
                $"Api error [{response.StatusCode}]: {result.Error}"
            );
    }
}