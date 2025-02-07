using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Nellebot.Attributes;

namespace Nellebot.CommandModules;

public class RandomModule
{
    [BaseCommandCheck]
    [Command("Oi")]
    [Description("Oi!")]
    public static ValueTask Oi(CommandContext ctx)
    {
        return ctx.RespondAsync("Oi!");
    }

    [BaseCommandCheck]
    [Command("Meowdy")]
    [Description("Say meowdy!")]
    public static ValueTask Meowdy(CommandContext ctx)
    {
        const string meowdy = @".
                 <:meowdy:993855998601220166> are u gonna say meowdy
　＿ノ ヽ ノ＼＿ back or are we
/      / ⌒ Ｙ ⌒ Ｙ     ヽ gonna have a
( 　(三ヽ人　 /　　 | heckin problem
|　ﾉ⌒＼ ￣￣ヽ　 ノ here purrdner?
ヽ＿＿＿＞､＿＿_／
　　 ｜( 王 ﾉ〈
　　 /ﾐ`ー―彡
      /      ╰  ╯
";

        return ctx.RespondAsync(meowdy);
    }

    [BaseCommandCheck]
    [Command("Slap")]
    [Description("Slap someone with a large trout")]
    [SlashCommandTypes(DiscordApplicationCommandType.SlashCommand, DiscordApplicationCommandType.UserContextMenu)]
    public static async ValueTask Slap(
        CommandContext ctx,
        [Parameter("slapee")] [Description("The receiver of the slap")]
        DiscordMember member)
    {
        string slapper = ctx.Member?.DisplayName ?? ctx.User.Username;
        string slapee = member.DisplayName;

        await ctx.RespondAsync($"_**{slapper}** slaps **{slapee}** around a bit with a large trout_");
    }

    [BaseCommandCheck]
    [Command("ban")]
    [Description("Ban someone. For realzies!")]
    [AllowedProcessors(typeof(TextCommandProcessor))]
    public static ValueTask Ban(
        CommandContext ctx,
        [Parameter("name")] [Description("Someone you don't like")] [RemainingText]
        string str)
    {
        return ctx.RespondAsync($"Why ban **{str}** when I can ban you instead?");
    }
}
