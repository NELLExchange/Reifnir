using System.Linq;
using Microsoft.Extensions.Options;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.Services;

public class AuthorizationService
{
    private readonly BotOptions _options;

    public AuthorizationService(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public bool IsAdminOrMod(AppDiscordMember member)
    {
        bool currentUserIsAdmin = member.Roles.Any(r => r.HasAdminPermission);

        if (currentUserIsAdmin)
        {
            return true;
        }

        ulong modRoleId = _options.ModRoleId;

        bool currentUserIsMod = member.Roles.Select(r => r.Id).Contains(modRoleId);

        return currentUserIsMod;
    }

    public bool IsTrustedMember(AppDiscordMember member)
    {
        if (IsAdminOrMod(member))
        {
            return true;
        }

        ulong[] trustedRoleIds = _options.TrustedRoleIds;

        bool currentUserIsTrusted = member.Roles.Select(r => r.Id).Intersect(trustedRoleIds).Any();

        return currentUserIsTrusted;
    }
}
