using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using Microsoft.Extensions.DependencyInjection;
using Nellebot.Common.AppDiscordModels;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;

namespace Nellebot.Attributes;

/// <summary>
///     Reject commands coming from DM or from other guild (if it exists).
/// </summary>
public class RequireTrustedMemberCheck : IContextCheck<RequireTrustedMemberAttribute>
{
    public ValueTask<string?> ExecuteCheckAsync(RequireTrustedMemberAttribute attribute, CommandContext ctx)
    {
        var authorizationService = ctx.Client.ServiceProvider.GetRequiredService<AuthorizationService>();

        if (ctx.Member is null) return ValueTask.FromResult<string?>("This command must be executed in a guild.");

        AppDiscordMember appMember = DiscordMemberMapper.Map(ctx.Member);

        bool isAuthorized = authorizationService.IsTrustedMember(appMember);

        return ValueTask.FromResult<string?>(isAuthorized ? null : "You are not authorized to use this command.");
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class RequireTrustedMemberAttribute : ContextCheckAttribute;
