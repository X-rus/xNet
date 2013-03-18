* PL: C#
* Version: 3.0
* Version .NET: 4.0 Client Profile

xNet - a class library for .NET Framework, which includes:
* Classes for work with proxy servers: _HTTP, Socks4(a), Socks5_.
* Classes for work with *HTTP 1.0/1.1* protocol: _keep-alive, gzip, deflate, chunked, SSL, proxies and more_.
* Classes for work with multithreading: _a multithreaded bypassing the collection, asynchronous events and more_.

Detailed description: http://habrahabr.ru/post/146475/

Example:
<pre>
using (var request = new HttpRequest("http://site.com"))
{
    request.UserAgent = HttpHelper.ChromeUserAgent();
	request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");

    request
        // Parameters URL-address.
        .AddUrlParam("data1", "value1")
        .AddUrlParam("data2", "value2")

        // Parameters 'x-www-form-urlencoded'.
        .AddParam("data1", "value1")
        .AddParam("data2", "value2")
        .AddParam("data2", "value2")

        // Multipart data.
        .AddField("data1", "value1")
        .AddFile("game_code", @"C:\orion.zip")

        // HTTP-header.
        .AddHeader("X-Apocalypse", "21.12.12");
		
    // These parameters are sent in this request.
    request.Post("/").None();

    // But in this request, they will be gone.
    request.Post("/").None();
}
</pre>