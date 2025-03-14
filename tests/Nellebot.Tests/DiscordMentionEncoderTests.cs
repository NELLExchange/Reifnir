using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Utils;

namespace Nellebot.Tests;

[TestClass]
public class DiscordMentionEncoderTests
{
    private static readonly Dictionary<ulong, AppDiscordRole> Roles = new()
    {
        { 1, new AppDiscordRole { Id = 1, Name = "Moderator" } },
        { 2, new AppDiscordRole { Id = 2, Name = "Correct me!" } },
        { 3, new AppDiscordRole { Id = 3, Name = "Norwegian native speaker" } },
        { 4, new AppDiscordRole { Id = 4, Name = "Danish speaker/native" } },
        { 5, new AppDiscordRole { Id = 5, Name = "Beginner (A1/A2)" } },
        { 6, new AppDiscordRole { Id = 6, Name = "Non-learner (A0)" } },
        { 7, new AppDiscordRole { Id = 7, Name = "Bokmål" } },
        { 8, new AppDiscordRole { Id = 8, Name = "matrix-2bot" } },
    };

    private static readonly Dictionary<ulong, AppDiscordChannel> Channels = new()
    {
        { 1, new AppDiscordChannel { Id = 1, Name = "roles" } },
        { 2, new AppDiscordChannel { Id = 2, Name = "channels_and_info" } },
        { 3, new AppDiscordChannel { Id = 3, Name = "meta-test" } },
        { 4, new AppDiscordChannel { Id = 4, Name = "conversation｜🇬🇧" } },
        { 5, new AppDiscordChannel { Id = 5, Name = "loffe_rundt｜🇳🇴" } },
        { 6, new AppDiscordChannel { Id = 6, Name = "trjætt_gpt｜🇳🇴" } },
        { 7, new AppDiscordChannel { Id = 7, Name = "båthavn" } },
    };

    private static readonly Dictionary<ulong, AppDiscordEmoji> Emojis = new()
    {
        { 1, new AppDiscordEmoji { Id = 1, Name = "flag_noball" } },
        { 2, new AppDiscordEmoji { Id = 2, Name = "ablob_fest", IsAnimated = true } },
    };

    private static readonly AppDiscordGuild Guild = new()
    {
        Id = 1,
        Name = "Test Guild",
        Roles = Roles,
        Channels = Channels,
        Emojis = Emojis,
    };

    [TestMethod]
    public void EncodeMentions_WithRoles_ShouldEncodeRoleMentions()
    {
        // Arrange
        const string input = "Please contact @Moderator or someone with @Correct me! role.";
        const string expected = "Please contact <@&1> or someone with <@&2> role.";

        // Act
        string result = DiscordMentionEncoder.EncodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void EncodeMentions_WithChannels_ShouldEncodeChannelMentions()
    {
        // Arrange
        const string input =
            "Check #roles and #meta-test for more information. And watch out for #båthavn and #trjætt_gpt｜🇳🇴 channels!";
        const string expected = "Check <#1> and <#3> for more information. And watch out for <#7> and <#6> channels!";

        // Act
        string result = DiscordMentionEncoder.EncodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void EncodeMentions_WithEmojis_ShouldEncodeEmojis()
    {
        // Arrange
        const string input = "This is a test :flag_noball: :ablob_fest:";
        const string expected = "This is a test <:flag_noball:1> <a:ablob_fest:2>";

        // Act
        string result = DiscordMentionEncoder.EncodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeMentions_WithRoles_ShouldDecodeRoleMentions()
    {
        // Arrange
        const string input = "Please contact <@&1> or someone with <@&2> role.";
        const string expected = "Please contact @Moderator or someone with @Correct me! role.";

        // Act
        string result = DiscordMentionEncoder.DecodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeMentions_WithChannels_ShouldDecodeChannelMentions()
    {
        // Arrange
        const string input = "Check <#1> and <#3> for more information.";
        const string expected = "Check #roles and #meta-test for more information.";

        // Act
        string result = DiscordMentionEncoder.DecodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeMentions_WithEmojis_ShouldDecodeEmojis()
    {
        // Arrange
        const string input = "This is a test with emojis: <:flag_noball:1> and <a:ablob_fest:2>";
        const string expected = "This is a test with emojis: :flag_noball: and :ablob_fest:";

        // Act
        string result = DiscordMentionEncoder.DecodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeMentions_ShouldDecodeMixedMentions()
    {
        // Arrange
        const string input =
            "Users with <@&5> <a:ablob_fest:2> should visit <#4> and <#5> and ask <@&1> for help <:flag_noball:1>";
        const string expected =
            "Users with @Beginner (A1/A2) :ablob_fest: should visit #conversation｜🇬🇧 and #loffe_rundt｜🇳🇴 and ask @Moderator for help :flag_noball:";

        // Act
        string result = DiscordMentionEncoder.DecodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeMentions_WithNonGuildMentions_ShouldNotDecodeMentions()
    {
        // Arrange
        const string input =
            "This <@&999> role and <#888> channel don't exist And neither do these emojis <:unknown_emoji:777> and <a:unknown_emoji:666>.";

        // Act
        string result = DiscordMentionEncoder.DecodeMentions(Guild, input);

        // Assert
        Assert.AreEqual(input, result);
    }

    [TestMethod]
    public void EncodeThenDecode_WithMixedMentions_ShouldReturnOriginalString()
    {
        // Arrange
        const string original =
            "Users with @Beginner (A1/A2) :ablob_fest: should visit #conversation｜🇬🇧 and ask @Moderator for help :flag_noball:";

        // Act
        string encoded = DiscordMentionEncoder.EncodeMentions(Guild, original);
        string decoded = DiscordMentionEncoder.DecodeMentions(Guild, encoded);

        // Assert
        Assert.AreNotEqual(original, encoded); // Ensure encoding did something
        Assert.AreEqual(
            "Users with @Beginner (A1/A2) :ablob_fest: should visit #conversation｜🇬🇧 and ask @Moderator for help :flag_noball:",
            decoded);
    }
}
