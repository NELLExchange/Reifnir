using System.Collections.Generic;

namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordMember : AppDiscordUser
{
    public string DisplayName { get; set; } = string.Empty;

    public IEnumerable<AppDiscordRole> Roles { get; set; } = null!;

    /// <summary>
    ///     Builds a stub AppDiscordMember with the given id.
    ///     The source of the stub is usually a MessageRef, where only the member id is available.
    /// </summary>
    /// <param name="id">The DiscordUserId.</param>
    /// <returns><see cref="AppDiscordMember" />.</returns>
    public static AppDiscordMember BuildStub(ulong id)
    {
        return new AppDiscordMember
        {
            Id = id,
            Username = "N/A",
            Discriminator = "N/A",
            DisplayName = "N/A",
            Roles = [],
        };
    }
}
