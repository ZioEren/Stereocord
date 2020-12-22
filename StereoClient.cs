using Discord.Gateway;
using System.Net;
public class StereoClient
{
    private string token;
    private DiscordSocketClient client;
    private WebProxy proxy;
    public StereoClient(string token, DiscordSocketClient client, WebProxy proxy = null)
    {
        this.token = token;
        this.client = client;
        this.proxy = proxy;
    }
    public string GetToken()
    {
        return token;
    }
    public DiscordSocketClient GetClient()
    {
        return client;
    }
    public WebProxy GetProxy()
    {
        return proxy;
    }
}