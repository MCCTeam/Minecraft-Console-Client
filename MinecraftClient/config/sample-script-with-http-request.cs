//MCCScript 1.0

string mojangStatus = PerformHttpRequest("https://status.mojang.com/check");
MCC.LogToConsole(mojangStatus);

//MCCScript Extensions

string PerformHttpRequest(string uri)
{
    var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
    var response = (System.Net.HttpWebResponse)request.GetResponse();
    string responseString;
    using (var stream = response.GetResponseStream())
        using (var reader = new StreamReader(stream))
            responseString = reader.ReadToEnd();
    return responseString;
}