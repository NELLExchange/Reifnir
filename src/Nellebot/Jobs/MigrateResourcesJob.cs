using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Quartz;

namespace Nellebot.Jobs;

public class MigrateResourcesJob : IJob
{
    public static readonly JobKey Key = new("migrate-resources", "default");

    private const string PostEmojiCode = "\uD83C\uDDF5";
    private const string MergeEmojiCode = "\uD83C\uDDF2";
    private const string CommentEmojiCode = "\uD83C\uDDE8";

    private const string MediaTagEmojiCode = "\uD83C\uDDEA";
    private const string NynorskTagEmojiCode = "\uD83C\uDDF3";
    private const string DialectsTagEmojiCode = "\uD83C\uDDE9";

    private const string MediaTag = "media";
    private const string NynorskTag = "nynorsk";
    private const string DialectsTag = "dialects";

#if DEBUG
    private const ulong ResourcesForumChannelId = 1333043798313537628;

    private static readonly ResourceChannel[] SourceResourcesChannelIds =
    [
        new(DiscordChannelId: 1333043499200679947, Tag: null), // #lang-res
        new(DiscordChannelId: 1333048476950466570, NynorskTag), // #nn-res
        new(DiscordChannelId: 1333043544553820222, MediaTag), // #media-res
    ];
#else
    private const ulong ResourcesForumChannelId = 1289968921331634231;

    private static readonly ResourceChannel[] SourceResourcesChannelIds =
    [
        new(622153554198659122, null), // #lang-res
        new(799759595513446480, NynorskTag), // #nn-res
        new(769645524134395904, MediaTag), // #media-res
    ];
#endif

    private readonly DiscordResolver _discordResolver;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MigrateResourcesJob(
        DiscordResolver discordResolver,
        DiscordLogger discordLogger,
        IDiscordErrorLogger discordErrorLogger,
        IHttpClientFactory httpClientFactory)
    {
        _discordResolver = discordResolver;
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            CancellationToken cancellationToken = context.CancellationToken;

            context.MergedJobDataMap.TryGetBooleanValue("dryRun", out bool isDryRun);

            _discordLogger.LogOperationMessage($"Running job {Key}{(isDryRun ? " (dry run)" : string.Empty)}");

            var forumPosts = new List<ForumPost>();

            DiscordForumChannel resourcesChannel =
                await _discordResolver.ResolveChannelAsync(ResourcesForumChannelId) as DiscordForumChannel
                ?? throw new InvalidOperationException($"Could not resolve channel {ResourcesForumChannelId}");

            IReadOnlyList<DiscordForumTag> channelTags = resourcesChannel.AvailableTags;

            _discordLogger.LogOperationMessage("Collecting messages...");

            var successCount = 0;
            var failureCount = 0;
            var skippedCount = 0;

            // Collect messages
            foreach (ResourceChannel resChannel in SourceResourcesChannelIds)
            {
                (ulong resChannelId, string? resChannelTag) = resChannel;

                DiscordChannel discordChannel = await _discordResolver.ResolveChannelAsync(resChannelId)
                                                ?? throw new InvalidOperationException(
                                                    $"Could not resolve channel {resChannelId}");

                ForumPost? currentPost = null;

                await foreach (DiscordMessage message in discordChannel.GetMessagesAfterAsync(
                                   after: 0,
                                   short.MaxValue,
                                   cancellationToken))
                {
                    if (HasReaction(message, PostEmojiCode))
                    {
                        currentPost = new ForumPost(message);

                        if (HasReaction(message, EmojiMap.WhiteCheckmark))
                        {
                            // Already migrated
                            currentPost.Migrated = true;
                            skippedCount++;
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

            _discordLogger.LogOperationMessage("Done collecting messages");

            // Post messages in forum channel in chronological order
            List<ForumPost> orderedForumPosts =
                forumPosts
                    .Where(x => !x.Migrated)
                    .OrderBy(x => x.FirstContentMessage.Id)
                    .ToList();

            _discordLogger.LogOperationMessage($"{orderedForumPosts.Count} resource forum posts will be created");

            _discordLogger.LogOperationMessage($"{skippedCount} will be skipped");

            foreach (ForumPost forumPost in orderedForumPosts)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string title = forumPost.GetTitle();
                    List<string> content = forumPost.GetTextContent();
                    List<DiscordForumTag> tags = forumPost.Tags;
                    string[] comments = forumPost.GetComments();
                    List<DiscordAttachment> imageAttachments = forumPost.GetImageAttachments();

                    string firstMessagePart = content.First();
                    List<string> remainingMessageParts = content.Skip(1).ToList();

                    DiscordMessageBuilder discordMessageBuilder =
                        new DiscordMessageBuilder()
                            .WithContent(firstMessagePart)
                            .SuppressNotifications();

                    if (imageAttachments.Count > 0)
                    {
                        using HttpClient client = _httpClientFactory.CreateClient();

                        foreach (DiscordAttachment imageAttachment in imageAttachments)
                        {
                            string fileName = imageAttachment.FileName ?? Guid.NewGuid().ToString();
                            Stream stream = await client.GetStreamAsync(imageAttachment.Url, cancellationToken);

                            discordMessageBuilder.AddFile(fileName, stream);
                        }
                    }

                    ForumPostBuilder forumPostBuilder = new ForumPostBuilder()
                        .WithName(title)
                        .WithMessage(discordMessageBuilder)
                        .WithAutoArchiveDuration(DiscordAutoArchiveDuration.Week);

                    forumPostBuilder = tags.Aggregate(forumPostBuilder, (current, tag) => current.AddTag(tag));

                    var sb = new StringBuilder();
                    sb.AppendLineLF($"Creating post for message {forumPost.FirstContentMessage.JumpLink}")
                        .AppendLineLF($"Title: {title}")
                        .AppendLineLF($"Content: {content.Sum(x => x.Length)} chars in {content.Count} message(s)")
                        .AppendLineLF(
                            $"Tags: {(tags.Count > 0 ? string.Join(", ", tags.Select(x => x.Name)) : "none")}")
                        .AppendLineLF($"Comments: {comments.Length}")
                        .AppendLineLF($"Images: {imageAttachments.Count}")
                        .AppendLineLF($"Non image files: {forumPost.NonImageAttachmentCount}");

                    _discordLogger.LogOperationMessage(sb.ToString().TrimEnd());

                    // Gives us some time to follow along and to cancel if something goes wrong
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                    if (isDryRun)
                    {
                        successCount++;
                        continue;
                    }

                    DiscordForumPostStarter post = await resourcesChannel.CreateForumPostAsync(forumPostBuilder);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    foreach (string messagePart in remainingMessageParts)
                    {
                        DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder()
                            .WithContent(messagePart)
                            .SuppressNotifications();

                        await post.Channel.SendMessageAsync(messageBuilder);
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }

                    foreach (string comment in comments)
                    {
                        DiscordMessageBuilder messageBuilder =
                            new DiscordMessageBuilder()
                                .WithContent(comment)
                                .SuppressNotifications();

                        await post.Channel.SendMessageAsync(messageBuilder);
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }

                    DiscordMessage originalMessageForPost = forumPost.FirstContentMessage;

                    await originalMessageForPost.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                    successCount++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failureCount++;
                    _discordErrorLogger.LogError(ex, $"Failed to post message: {ex.Message}");
                }
            }

            _discordLogger.LogOperationMessage($"Successful forum posts: {successCount}");
            _discordLogger.LogOperationMessage($"Failed forum posts: {failureCount}");
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

        private List<DiscordMessage> ContentMessages { get; } = new();

        private List<DiscordMessage> CommentMessages { get; } = new();

        public List<DiscordForumTag> Tags { get; } = new();

        public bool Migrated { get; set; }

        public DiscordMessage FirstContentMessage => ContentMessages.First();

        public int NonImageAttachmentCount => ContentMessages.SelectMany(x => x.Attachments)
            .Count(x => !x.IsImageAttachment());

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

            List<DiscordAttachment> nonImageAttachments = ContentMessages
                .SelectMany(x => x.Attachments)
                .Where(x => !x.IsImageAttachment())
                .ToList();

            if (nonImageAttachments.Count > 0)
            {
                sb.AppendLineLF();
                sb.AppendLineLF("The following files are present in the original message:");

                foreach (DiscordAttachment attachment in nonImageAttachments)
                {
                    sb.AppendLineLF($"`{attachment.FileName}`");
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
            string[] textLines = allText.Split('\n');

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

        public List<DiscordAttachment> GetImageAttachments()
        {
            List<DiscordAttachment> imageAttachments = ContentMessages
                .SelectMany(x => x.Attachments)
                .Where(x => x.IsImageAttachment())
                .ToList();

            return imageAttachments;
        }

        public string[] GetComments()
        {
            var commentTextList = new List<string>();

            foreach (DiscordMessage message in CommentMessages)
            {
                var sb = new StringBuilder();
                sb.AppendLineLF(message.Content);

                IReadOnlyList<DiscordAttachment> messageAttachments = message.Attachments;

                if (messageAttachments.Count > 0)
                {
                    sb.AppendLineLF();
                    sb.AppendLineLF("The following files are present in the original message:");

                    foreach (DiscordAttachment attachment in messageAttachments)
                    {
                        sb.AppendLineLF($"`{attachment.FileName}`");
                    }
                }

                DiscordUser? author = message.Author;
                string authorMentionOrUsername = author is null ? "Unknown" : author.Mention;
                var messageLink = message.JumpLink.ToString();

                sb.AppendLineLF();
                sb.AppendLineLF($"[View original message]({messageLink}) by {authorMentionOrUsername}.");

                commentTextList.Add(sb.ToString().Trim());
            }

            return commentTextList.ToArray();
        }
    }
}
