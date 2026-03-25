//MCCScript 1.0

string mojangStatus = PerformHttpRequest("https://status.mojang.com/check");
MCC.LogToConsole(mojangStatus);

//MCCScript Extensions

private static readonly System.Net.Http.HttpClient s_httpClient = new();

string PerformHttpRequest(string uri)
{
    return s_httpClient.GetStringAsync(uri).GetAwaiter().GetResult();
}

void SendHttpPostAsync(string uri, string text)
{
    new Thread(() => {
        using var content = new System.Net.Http.StringContent(text, System.Text.Encoding.UTF8, "text/plain");
        using var response = s_httpClient.PostAsync(uri, content).GetAwaiter().GetResult();
        string responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        //LogToConsole(responseString);
    }).Start();
}
