using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Utils;

namespace Nellebot.CommandModules;

// TODO rewrite help module to only have a single /help command
// and use interactive help command to show all commands
[BaseCommandCheck]
[Command("help")]
[AllowedProcessors(typeof(TextCommandProcessor))]
public class HelpModule
{
    private readonly BotOptions _options;

    public HelpModule(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    [DefaultGroupCommand]
    [Command("index")]
    public ValueTask Help(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;
        const string slashPrefix = DiscordConstants.SlashCommandPrefix;

        sb.AppendLine();
        sb.AppendLine("Dictionary commands:");
        sb.AppendLine($"`{commandPrefix}bm [word] .. Search the bokmål dictionary`");
        sb.AppendLine($"`{commandPrefix}nn [word] .. Search the nynorsk dictionary`");

        sb.AppendLine();
        sb.AppendLine("Other useful commands:");
        sb.AppendLine($"`{commandPrefix}cookie-stats me     .. Show personal cookie stats`");
        sb.AppendLine($"`{commandPrefix}cookie-stats [user] .. Show another user's cookie stats`");

        sb.AppendLine();
        sb.AppendLine("Staff commands:");
        sb.AppendLine($"`{commandPrefix}help admin-misc`");
        sb.AppendLine($"`{commandPrefix}help valhall`");

        sb.AppendLine();
        sb.AppendLine($"Most of the commands are also available as slash commands.");
        sb.AppendLine($"Try typing `{slashPrefix}` in the chat to see them.");

        sb.AppendLine();
        sb.AppendLine("If you have any problems, you can always ask the moderators for help.");
        sb.AppendLine(
            $"You can privately message the moderator team either by using the `{slashPrefix}modmail` command");
        sb.AppendLine("or by sending me a DM. I will then pass your message on to the moderators.");

        sb.AppendLine();
        sb.AppendLine(
            "Reifnir source code: [github.com/NELLE-reifnir-bot/reifnir-bot](https://github.com/NELLE-reifnir-bot/reifnir-bot)");

        DiscordEmbed eb = EmbedBuilderHelper.BuildSimpleEmbed("Help", sb.ToString());

        return ctx.RespondAsync(eb);
    }

    [Command("admin-misc")]
    public ValueTask HelpAdminMisc(CommandContext ctx)
    {
        var sb = new StringBuilder();

        var command = $"{_options.CommandPrefix}admin";

        sb.AppendLine($"`{command} nickname [name]`");
        sb.AppendLine("`   Change Reifnir's nickname`");
        sb.AppendLine();

        sb.AppendLine($"`{command} set-greeting-message [message]`");
        sb.AppendLine("`   Set the greeting message that Reifnir welcomes new users with.`");
        sb.AppendLine("`   Use the token $USER to @mention the new user in the message`");
        sb.AppendLine();

        DiscordEmbed eb = EmbedBuilderHelper.BuildSimpleEmbed("Misc. admin commands", sb.ToString());

        return ctx.RespondAsync(eb);
    }

    [Command("valhall")]
    public ValueTask HelpValhallMisc(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;

        sb.AppendLine($"`{commandPrefix}vkick [user] [reason]`");
        sb.AppendLine("`   Kick a recently joined user with a fresh Discord account.`");
        sb.AppendLine("`   Max 24hrs server memembership, max 7 days Discord account age.`");
        sb.AppendLine();

        sb.AppendLine($"`{commandPrefix}list-award-channels`");
        sb.AppendLine("`   List the channels where Reifnir keeps track of cookies.`");
        sb.AppendLine();

        sb.AppendLine($"`{commandPrefix}goodbye-msg add [message]`");
        sb.AppendLine("`   Add a goodbye message template. The message must contain at least one $USER token.`");
        sb.AppendLine("`Usage examples:`");
        sb.AppendLine($"`   {commandPrefix}goodbye-msg add $USER has left. Goodbye!`");
        sb.AppendLine($"`   {commandPrefix}goodbye-msg add $USER has the building. See ya, $USER!`");
        sb.AppendLine();

        sb.AppendLine($"`{commandPrefix}goodbye-msg remove [message id]`");
        sb.AppendLine("`   Remove a goodbye message template by id. Non-admins can only remove own messages.`");
        sb.AppendLine();

        sb.AppendLine($"`{commandPrefix}goodbye-msg list`");
        sb.AppendLine("`   List all goodbye message templates.`");
        sb.AppendLine();

        DiscordEmbed eb = EmbedBuilderHelper.BuildSimpleEmbed("Misc. valhall commands", sb.ToString());

        return ctx.RespondAsync(eb);
    }
}
