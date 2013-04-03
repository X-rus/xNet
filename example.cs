public class Captcha
{
    public string Url { get; private set; }
    public string Key { get; set; }

    public Captcha(string url)
    {
        Url = url;
    }
}

public enum AccountStatus
{
    None,
    Valid,
    NotValid
}

public class Message
{
    public string AuthorID { get; set; }
    public string AuthorName { get; set; }

    public string Value { get; set; }
}

public class Account : IHttpConnect
{
    public HttpRequest Request { get; set; }

    public string Login { get; private set; }
    public string Password { get; private set; }
    public CookieDictionary Cookies { get; private set; }

    public Account(string login, string password)
    {
        Login = login;
        Password = password;
    }

    public void InitRequest(HttpRequest request)
    {
        Request = request;

        request.Cookies = Cookies; // На случай, если аккаунт уже авторизован.
        request.AllowAutoRedirect = false;

        request.Referer = "http://www.site.com";
        request.BaseAddress = new Uri("http://www.site.com");
    }

    public AccountStatus Logining(ref Captcha captcha)
    {
        // Если это первое обращение, то устанавливаем новые куки.
        if (captcha == null)
        {
            Cookies = new CookieDictionary();
            Request.Cookies = Cookies;
        }

        Request.AddParam("login", Login).AddParam("password", Password);

        // Если до этого требовался ввод капчи.
        if (captcha != null)
        {
            Request.AddParam("c_key", captcha.Key);
        }

        string loginingResult = Request.Post("/logining.php").ToString();

        // Если требуется ввод капчи.
        if (loginingResult.Contains("need_captcha"))
        {
            string captchaUrl = loginingResult.Substring("captcha_url=", "&");
            captcha = new Captcha(captchaUrl);

            return AccountStatus.None;
        }

        if (loginingResult.Contains("ok"))
        {
            return AccountStatus.Valid;
        }

        return AccountStatus.NotValid;
    }

    public Message[] GetPrivateMessages()
    {
        string getPmResult = Request.Post("/get_pm.php").ToString();

        var messages = new List<Message>();

        // парсим список личных сообщений

        return messages.ToArray();
    }

    public void ReplyToMessage(string userId, string message)
    {
        Request.AddParam("to_id", userId).AddParam("msg", message);

        Request.Post("/send_pm.php").None();
    }
}

static void Main()
{
    using (var request = new HttpRequest())
    {
        request.UserAgent = HttpHelper.ChromeUserAgent();
        request.Proxy = HttpProxyClient.Parse("127.0.0.18080");

        var account = new Account("John", "Smith");
        account.InitRequest(request);

        Captcha captcha = null;
        AccountStatus accountStatus = AccountStatus.None;

        do
        {
            accountStatus = account.Logining(ref captcha);

            // Нужно ввести капчу.
            if (captcha != null)
            {
                request.Get(captcha.Url).ToFile("cap.jpg");
                captcha.Key = Console.ReadLine();
            }
        } while (captcha != null);

        if (accountStatus == AccountStatus.Valid)
        {
            var messages = account.GetPrivateMessages();

            foreach (var message in messages)
            {
                string answer = string.Format(
                    "Привет, {0}! Мой хозяин сейчас занят и не может ответить тебе...",
                    message.AuthorName);

                account.ReplyToMessage(message.AuthorID, answer);
            }
        }
    }
}