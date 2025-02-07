namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordUser
{
    public ulong Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Discriminator { get; set; } = string.Empty;

    public bool HasLegacyUsername()
    {
        return Discriminator != "0";
    }

    public string GetFullUsername()
    {
        return HasLegacyUsername() ? $"{Username}#{Discriminator}" : Username;
    }

    public string GetDetailedUserIdentifier()
    {
        return $"{GetFullUsername()} ({Id})";
    }
}
