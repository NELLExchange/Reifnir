namespace Nellebot.Infrastructure;

public static class SharedCacheKeys
{
    public static string DiscordChannel => "DiscordChannel_{0}";

    public static string GreetingMessage => "GreetingMessage";

    public static string GoodbyeMessages => "GoodbyeMessages";

    public static string UserLog => "UserLog_{0}_{1}";
}
