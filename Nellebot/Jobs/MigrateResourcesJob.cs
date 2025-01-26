using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Quartz;

namespace Nellebot.Jobs;

public class MigrateResourcesJob : IJob
{
    private readonly DiscordResolver _discordResolver;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    public static readonly JobKey Key = new("mig-res", "default");

    private const string PostEmojiCode = "\uD83C\uDDF5";
    private const string MergeEmojiCode = "\uD83C\uDDF2";
    private const string CommentEmojiCode = "\uD83C\uDDE8";

    private const string MediaTagEmojiCode = "\uD83C\uDDEA";
    private const string NynorskTagEmojiCode = "\uD83C\uDDF3";
    private const string DialectsTagEmojiCode = "\uD83C\uDDE9";

    private const string MediaTag = "media";
    private const string NynorskTag = "nynorsk";
    private const string DialectsTag = "dialects";

    public MigrateResourcesJob(DiscordResolver discordResolver, IDiscordErrorLogger discordErrorLogger)
    {
        _discordResolver = discordResolver;
        _discordErrorLogger = discordErrorLogger;
    }

    private static ResourceChannel[] SourceResourcesChannelIds =
    {
        new(1333043499200679947, null), // #lang-res
        new(1333043544553820222, MediaTag), // #media-res
        new(1333048476950466570, NynorskTag), // #nn-res
    };

    private const ulong ResourcesForumChannelId = 1333043798313537628;

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var forumPosts = new List<ForumPost>();

            DiscordForumChannel resourcesChannel =
                await _discordResolver.ResolveChannelAsync(ResourcesForumChannelId) as DiscordForumChannel
                ?? throw new InvalidOperationException(
                    $"Could not resolve channel {ResourcesForumChannelId}");

            IReadOnlyList<DiscordForumTag> channelTags = resourcesChannel.AvailableTags;

            // Collect messages
            foreach (ResourceChannel resChannel in SourceResourcesChannelIds)
            {
                (ulong resChannelId, string? resChannelTag) = resChannel;

                DiscordChannel discordChannel = await _discordResolver.ResolveChannelAsync(resChannelId)
                                                ?? throw new InvalidOperationException(
                                                    $"Could not resolve channel {resChannelId}");

                ForumPost? currentPost = null;

                await foreach (DiscordMessage message in discordChannel.GetMessagesAfterAsync(0))
                {
                    if (HasReaction(message, PostEmojiCode))
                    {
                        currentPost = new ForumPost(message);

                        if (HasReaction(message, EmojiMap.WhiteCheckmark))
                        {
                            // Already migrated
                            currentPost.Migrated = true;
                            continue;
                        }

                        // Add tags
                        if (HasReaction(message, MediaTagEmojiCode))
                        {
                            currentPost.AddTagIfNotExists(MediaTag, channelTags);
                        }

                        if (HasReaction(message, NynorskTagEmojiCode))
                        {
                            currentPost.AddTagIfNotExists(NynorskTag, channelTags);
                        }

                        if (HasReaction(message, DialectsTagEmojiCode))
                        {
                            currentPost.AddTagIfNotExists(DialectsTag, channelTags);
                        }

                        if (resChannelTag is not null)
                        {
                            currentPost.AddTagIfNotExists(resChannelTag, channelTags);
                        }

                        forumPosts.Add(currentPost);
                    }
                    else if (HasReaction(message, MergeEmojiCode))
                    {
                        if (currentPost is null)
                            throw new InvalidOperationException("No post message to merge this message with");

                        if (currentPost.Migrated)
                            continue;

                        currentPost.AddContentMessage(message);
                    }
                    else if (HasReaction(message, CommentEmojiCode))
                    {
                        if (currentPost is null)
                            throw new InvalidOperationException("No post message to add this comment to");

                        if (currentPost.Migrated)
                            continue;

                        currentPost.AddCommentMessage(message);
                    }
                }
            }

            // Post messages in forum channel in chronological order
            IOrderedEnumerable<ForumPost> orderedForumPosts =
                forumPosts
                    .Where(x => !x.Migrated)
                    .OrderBy(x => x.ContentMessages.First().Id);

            foreach (ForumPost forumPost in orderedForumPosts)
            {
                try
                {
                    string title = forumPost.GetTitle();
                    List<string> content = forumPost.GetTextContent();
                    List<DiscordForumTag> tags = forumPost.Tags;
                    string[] comments = forumPost.GetComments();

                    string firstMessagePart = content.First();
                    List<string> remainingMessageParts = content.Skip(1).ToList();

                    DiscordMessageBuilder discordMessageBuilder =
                        new DiscordMessageBuilder().WithContent(firstMessagePart);

                    ForumPostBuilder forumPostBuilder = new ForumPostBuilder()
                        .WithName(title)
                        .WithMessage(discordMessageBuilder)
                        .WithAutoArchiveDuration(DiscordAutoArchiveDuration.Week);

                    forumPostBuilder = tags.Aggregate(forumPostBuilder, (current, tag) => current.AddTag(tag));

                    DiscordForumPostStarter post = await resourcesChannel.CreateForumPostAsync(forumPostBuilder);

                    foreach (string messagePart in remainingMessageParts)
                    {
                        await post.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(messagePart));
                    }

                    foreach (string comment in comments)
                    {
                        await post.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(comment));
                    }

                    DiscordMessage originalMessageForPost = forumPost.ContentMessages.First();

                    await originalMessageForPost.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
                }
                catch (Exception ex)
                {
                    _discordErrorLogger.LogError(ex, $"Failed to post message: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, $"Job failed: {ex.Message}");
            throw new JobExecutionException(ex);
        }
    }

    private static bool HasReaction(DiscordMessage message, string emojiCode)
    {
        return message.Reactions.Any(x => x.Emoji.Name == emojiCode);
    }

    private record ResourceChannel(ulong DiscordChannelId, string? Tag);

    private class ForumPost
    {
        public ForumPost(DiscordMessage message)
        {
            ContentMessages.Add(message);
        }

        public List<DiscordMessage> ContentMessages { get; } = new();

        public List<DiscordMessage> CommentMessages { get; } = new();

        public List<DiscordForumTag> Tags { get; } = new();

        public bool Migrated { get; set; }

        public void AddContentMessage(DiscordMessage message)
        {
            ContentMessages.Add(message);
        }

        public void AddCommentMessage(DiscordMessage message)
        {
            CommentMessages.Add(message);
        }

        public void AddTagIfNotExists(string tag, IReadOnlyList<DiscordForumTag> channelTags)
        {
            DiscordForumTag discordForumTag = channelTags.FirstOrDefault(x => x.Name == tag) ??
                                              throw new InvalidOperationException($"Tag {tag} not found");

            if (Tags.Any(x => x.Id == discordForumTag.Id)) return;

            Tags.Add(discordForumTag);
        }

        public string GetTitle()
        {
            DiscordMessage? message = ContentMessages.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Content));

            string title = message?.Content.Split('\n').FirstOrDefault() ?? "Untitled";

            title = title.Trim();

            return title[..Math.Min(title.Length, DiscordConstants.MaxThreadTitleLength)];
        }

        public List<string> GetTextContent()
        {
            var sb = new StringBuilder();

            foreach (DiscordMessage message in ContentMessages)
            {
                sb.AppendLineLF(message.Content);
            }

            List<DiscordAttachment> mergedAttachments = ContentMessages.SelectMany(x => x.Attachments).ToList();

            if (mergedAttachments.Count > 0)
            {
                sb.AppendLineLF();
                sb.AppendLineLF("Attachments:");

                foreach (DiscordAttachment attachment in mergedAttachments)
                {
                    sb.AppendLineLF($"[{attachment.FileName}]({attachment.Url})");
                }
            }

            DiscordMessage firstMessage = ContentMessages.First();
            DiscordUser? author = firstMessage.Author;
            string authorMentionOrUsername = author is null ? "Unknown" : author.Mention;
            var messageLink = firstMessage.JumpLink.ToString();

            sb.AppendLineLF();
            sb.AppendLineLF($"[View original message]({messageLink}) by {authorMentionOrUsername}.");

            string allText = sb.ToString().Trim();

            sb.Clear();

            // Split text into chunks of max 2000 chars each, split by newlines
            string[] textLines = allText.Split('\n').ToArray();

            var result = new List<string>();

            foreach (string text in textLines)
            {
                if (sb.Length + text.Length > DiscordConstants.MaxMessageLength)
                {
                    result.Add(sb.ToString().TrimEnd());
                    sb.Clear();
                }

                sb.AppendLineLF(text);
            }

            if (sb.Length > 0)
            {
                result.Add(sb.ToString().TrimEnd());
            }

            return result;
        }

        public string[] GetComments()
        {
            var commentTextList = new List<string>();

            foreach (DiscordMessage message in CommentMessages)
            {
                var sb = new StringBuilder();
                sb.AppendLine(message.Content);

                IReadOnlyList<DiscordAttachment> messageAttachments = message.Attachments;

                if (messageAttachments.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Attachments:");
                }

                foreach (DiscordAttachment attachment in messageAttachments)
                {
                    sb.AppendLine($"[{attachment.FileName}]({attachment.Url})");
                }

                DiscordMessage firstMessage = ContentMessages.First();
                DiscordUser? author = firstMessage.Author;
                string authorMentionOrUsername = author is null ? "Unknown" : author.Mention;
                var messageLink = firstMessage.JumpLink.ToString();

                sb.AppendLine();
                sb.AppendLine($"[View original message]({messageLink}) by {authorMentionOrUsername}.");

                commentTextList.Add(sb.ToString().Trim());
            }

            return commentTextList.ToArray();
        }
    }
}
