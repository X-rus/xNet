* PL: C#
* Version: 2.3
* Version .NET: 4.0 Client Profile

xNet - a class library for .NET Framework, which includes:
* Classes for work with proxy servers: _HTTP, Socks4(a), Socks5_.
* Classes for work with *HTTP 1.0/1.1* protocol: _keep-alive, gzip, deflate, chunked, SSL, proxies and more_.
* Classes for work with multithreading: _a multithreaded bypassing the collection, asynchronous events and more_.
* Classes helpers that extend standard classes *.NET Framework*: _FileHelper, DirectoryHelper, StringHelper, XmlHelper, BitHelper and others_.

Detailed description: http://habrahabr.ru/post/146475/

Example 1:
<pre>
using (var request = new HttpRequest())
{
    request.UserAgent = HttpHelper.RandomUserAgent();

    // Parameters URL-address.
    request.AddUrlParam("data1", "value1");
    request.AddUrlParam("data2", "value2");

    // Parameters 'x-www-form-urlencoded'.
    request.AddParam("data1", "value1");
    request.AddParam("data2", "value2");
    request.AddParam("data2", "value2");

    // Multipart data.
    request.AddField("data1", "value1");
    request.AddFile("game_code", @"C:\orion.zip");

    // HTTP-header.
    request.AddHeader("X-Apocalypse", "21.12.12");

    // These parameters are sent in this request.
    request.Raw(HttpMethod.POST, "http://habrahabr.ru/").None();

    // But in this request, they will be gone.
    request.Raw(HttpMethod.POST, "http://habrahabr.ru/").None();
}
</pre>

Example 2 (old version):
<pre>
using (var request = new HttpRequest())
{
	request.UserAgent = HttpHelper.RandomUserAgent();
	request.Proxy = Socks5ProxyClient.Parse("127.0.0.1:1080");

	var reqParams = new StringDictionary();

	reqParams["login"] = "neo";
	reqParams["password"] = "knockknock";

	string content = request.Post(
		"www.whitehouse.gov", reqParams).ToText();

	string secretsGovernment = content.Substring("secrets_government=\"", "\"");
}
</pre>

Example 3 (old version):
<pre>
using (var request = new HttpRequest())
{
    var multipartData = new MultipartDataCollection();

    multipartData.AddData("login", "Bill Gates");
    multipartData.AddData("password", "qwerthahaha");
    multipartData.AddDataFile("file1", @"C:\windows_9_alpha.rar", true);

    string content = request.Post(
        "www.microsoft.com", multipartData).ToText();
}
</pre>

Example 4:
<pre>
static void Main(string[] args)
{
    var mt = new MultiThreading<int>(10);

    mt.Run(MyAction);

    Thread.Sleep(1000);
    Console.ReadKey();
}

static void MyAction(MultiThreading<int> mt)
{
    Console.WriteLine("Hello Thread!");
}
</pre>