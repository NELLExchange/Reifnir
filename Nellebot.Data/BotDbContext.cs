﻿using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;

namespace Nellebot.Data
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserRole>()
                    .HasIndex(x => x.RoleId)
                    .IsUnique();

            builder.Entity<UserRoleAlias>()
                    .HasIndex(x => x.Alias)
                    .IsUnique();

            builder.Entity<AwardMessage>()
                .HasIndex(x => x.OriginalMessageId)
                .IsUnique();

            builder.Entity<AwardMessage>()
                .HasIndex(x => x.AwardedMessageId)
                .IsUnique();

            builder.Entity<BotSettting>()
                .HasIndex(x => x.Key)
                .IsUnique();
        }

        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<UserRoleAlias> UserRoleAliases { get; set; } = null!;
        public DbSet<AwardMessage> AwardMessages { get; set; } = null!;
        public DbSet<BotSettting> GuildSettings { get; set; } = null!;

    }
}
