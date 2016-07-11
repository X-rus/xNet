# eXtremeNet

[![AppVeyor branch](https://img.shields.io/appveyor/ci/gruntjs/grunt/master.svg?maxAge=2592000)](https://ci.appveyor.com/project/extremecodetv/extremenet)
[![NuGet version](https://badge.fury.io/nu/eXtremeNet.svg)](https://badge.fury.io/nu/eXtremeNet)

eXtremeNet - http class library for C# which includes:
 * Classes for work with proxy servers: HTTP, Socks4(a), Socks5, Chain.
 * Classes for work with HTTP 1.0/1.1 protocol: keep-alive, gzip, deflate, chunked, SSL, proxies and more.
 
# Installation
 
Install via NuGet
 
```
PM > Install-Package eXtremeNet
```
 
# Example
 
```csharp 
using (var request = new HttpRequest("http://site.com/"))
{
    request.UserAgent = Http.ChromeUserAgent();
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

    // But in this request they will be gone.
    request.Post("/").None();
}
```
