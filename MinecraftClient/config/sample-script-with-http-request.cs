//MCCScript 1.0

string mojangStatus = PerformHttpRequest("https://status.mojang.com/check");
MCC.LogToConsole(mojangStatus);

//MCCScript Extensions

string PerformHttpRequest(string uri)
{
    using var httpClient = new System.Net.Http.HttpClient();
    return httpClient.GetStringAsync(uri).GetAwaiter().GetResult();
}

void SendHttpPostAsync(string uri, string text)
{
    new Thread(() => {
        using var httpClient = new System.Net.Http.HttpClient();
        using var content = new System.Net.Http.StringContent(text, System.Text.Encoding.UTF8, "text/plain");
        using var response = httpClient.PostAsync(uri, content).GetAwaiter().GetResult();
        string responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        //LogToConsole(responseString);
    }).Start();
}
