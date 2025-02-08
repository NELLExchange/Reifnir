using System.Collections.Generic;

namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordApplication
{
    public IEnumerable<AppDiscordUser>? Owners { get; set; }
}
