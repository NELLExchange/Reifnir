using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using Nellebot.Common.Models.Modmail;
using Nellebot.Common.Models.Ordbok.Store;
using Nellebot.Common.Models.UserLogs;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Nellebot.Data;

public class BotDbContext : DbContext
{
    private const string JsonbColumnType = "jsonb";
    private readonly IDataProtectionProvider _dataProtectionProvider;

#if DEBUG
    public BotDbContext(DbContextOptions options)
        : base(options)
    {
        bool insideLINQPad = AppDomain.CurrentDomain.FriendlyName.StartsWith("LINQPad");

        if (!insideLINQPad) throw new Exception("This constructor should only be used in LINQPad");

        _dataProtectionProvider = new MockProtector();
    }
#endif

    public BotDbContext(DbContextOptions options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<AwardMessage>()
            .HasIndex(x => x.OriginalMessageId)
            .IsUnique();

        builder.Entity<AwardMessage>()
            .HasIndex(x => x.AwardedMessageId)
            .IsUnique();

        builder.Entity<BotSettting>()
            .HasIndex(x => x.Key)
            .IsUnique();

        builder.Entity<MessageRef>()
            .HasKey(x => x.MessageId);

        builder.Entity<MessageRef>()
            .Property(x => x.MessageId)
            .ValueGeneratedNever();

        builder.Entity<UserLog>()
            .HasIndex(x => new { x.UserId, x.LogType, x.Timestamp })
            .IsDescending(false, false, true);

        builder.Entity<UserLog>()
            .Property(x => x.RawValue)
            .HasColumnName("Value");

        builder.Entity<UserLog>()
            .Property(x => x.ValueType)
            .HasConversion(
                x => x.FullName ?? typeof(object).FullName!,
                x => Type.GetType(x) ?? typeof(object));

        builder.Entity<ModmailTicket>()
            .OwnsOne(
                x => x.TicketPost,
                x =>
                {
                    x.Property(x => x.ChannelThreadId).HasColumnName("ChannelThreadId");
                    x.Property(x => x.MessageId).HasColumnName("MessageId");
                });

        builder.Entity<ModmailTicket>()
            .Property(x => x.RequesterId)
            .HasConversion(new ProtectedConverter(_dataProtectionProvider, "RequesterId"));

        builder.Entity<OrdbokArticleStore>()
            .HasKey(x => new { x.Dictionary, x.WordClass });

        builder.Entity<OrdbokArticleStore>()
            .Property(x => x.ArticleList)
            .HasColumnType(JsonbColumnType);

        builder.Entity<OrdbokConceptStore>()
            .HasKey(x => x.Dictionary);

        builder.Entity<OrdbokConceptStore>()
            .Property(x => x.Concepts)
            .HasColumnType(JsonbColumnType);

        builder.Entity<MessageTemplate>()
            .Property(x => x.Message)
            .HasMaxLength(256);
    }

#pragma warning disable SA1201 // Elements should appear in the correct order

    public DbSet<AwardMessage> AwardMessages { get; set; }

    public DbSet<BotSettting> GuildSettings { get; set; }

    public DbSet<MessageRef> MessageRefs { get; set; }

    public DbSet<UserLog> UserLogs { get; set; }

    public DbSet<ModmailTicket> ModmailTickets { get; set; }

    public DbSet<OrdbokArticleStore> OrdbokArticlesStore { get; set; }

    public DbSet<OrdbokConceptStore> OrdbokConceptStore { get; set; }

    public DbSet<MessageTemplate> MessageTemplates { get; set; }
}
