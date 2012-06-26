*PL*: C#
*Version*: 2.1.1
*Version .NET*: 4.0 Client Profile
*Documentation*: XML and HTML in Russian
*IDE*: Microsoft Visual C # 2010 Express

*xNet* - a class library for *.NET Framework*, which includes:
* Classes for work with proxy servers: _HTTP, Socks4(a), Socks5_.
* Classes for work with *HTTP 1.0/1.1* protocol: _keep-alive, gzip, deflate, chunked, SSL, proxies and more_.
* Classes for work with multithreading: _a multithreaded bypassing the collection, asynchronous events and more_.
* Classes helpers that extend standard classes *.NET Framework*: _FileHelper, DirectoryHelper, StringHelper, XmlHelper, BitHelper and others_.

Here you can find examples and lessons (in Russian): [url:http://blog.epicsoft.ru/]

Example 1:
{{
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
}}

Example 2:
{{
using (var request = new HttpRequest())
{
    var multipartData = new MultipartDataCollection();

    multipartData.AddData("login", "Bill Gates");
    multipartData.AddData("password", "qwerthahaha");
    multipartData.AddDataFile("file1", @"C:\windows_9_alpha.rar", true);

    string content = request.Post(
        "www.microsoft.com", multipartData).ToText();
}
}}

Example 3:
{{
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
}}

The class diagram:
[image:http://epicsoft.ru/Content/xnet_diagram.png]