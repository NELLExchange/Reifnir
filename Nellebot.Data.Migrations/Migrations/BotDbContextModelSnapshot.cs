﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nellebot.Data;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nellebot.Data.Migrations.Migrations
{
    [DbContext(typeof(BotDbContext))]
    partial class BotDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Nellebot.Common.Models.AwardMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AwardChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<long>("AwardCount")
                        .HasColumnType("bigint");

                    b.Property<decimal>("AwardedMessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("OriginalChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("OriginalMessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("AwardedMessageId")
                        .IsUnique();

                    b.HasIndex("OriginalMessageId")
                        .IsUnique();

                    b.ToTable("AwardMessages");
                });

            modelBuilder.Entity("Nellebot.Common.Models.BotSettting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Key")
                        .IsUnique();

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("Nellebot.Common.Models.MessageRef", b =>
                {
                    b.Property<decimal>("MessageId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("MessageId");

                    b.ToTable("MessageRefs");
                });

            modelBuilder.Entity("Nellebot.Common.Models.MessageTemplate", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("AuthorId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("MessageTemplates");
                });

            modelBuilder.Entity("Nellebot.Common.Models.Modmail.ModmailTicket", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<bool>("IsAnonymous")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsClosed")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastActivity")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("RequesterDisplayName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RequesterId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ModmailTickets");
                });

            modelBuilder.Entity("Nellebot.Common.Models.Ordbok.Store.OrdbokArticleStore", b =>
                {
                    b.Property<string>("Dictionary")
                        .HasColumnType("text");

                    b.Property<string>("WordClass")
                        .HasColumnType("text");

                    b.Property<int>("ArticleCount")
                        .HasColumnType("integer");

                    b.Property<int[]>("ArticleList")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Dictionary", "WordClass");

                    b.ToTable("OrdbokArticlesStore");
                });

            modelBuilder.Entity("Nellebot.Common.Models.Ordbok.Store.OrdbokConceptStore", b =>
                {
                    b.Property<string>("Dictionary")
                        .HasColumnType("text");

                    b.Property<Dictionary<string, string>>("Concepts")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.HasKey("Dictionary");

                    b.ToTable("OrdbokConceptStore");
                });

            modelBuilder.Entity("Nellebot.Common.Models.UserLogs.UserLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("LogType")
                        .HasColumnType("integer");

                    b.Property<string>("RawValue")
                        .HasColumnType("text")
                        .HasColumnName("Value");

                    b.Property<decimal?>("ResponsibleUserId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("UserId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("ValueType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "LogType", "Timestamp")
                        .IsDescending(false, false, true);

                    b.ToTable("UserLogs");
                });

            modelBuilder.Entity("Nellebot.Common.Models.Modmail.ModmailTicket", b =>
                {
                    b.OwnsOne("Nellebot.Common.Models.Modmail.ModmailTicketPost", "TicketPost", b1 =>
                        {
                            b1.Property<Guid>("ModmailTicketId")
                                .HasColumnType("uuid");

                            b1.Property<decimal>("ChannelThreadId")
                                .HasColumnType("numeric(20,0)")
                                .HasColumnName("ChannelThreadId");

                            b1.Property<decimal>("MessageId")
                                .HasColumnType("numeric(20,0)")
                                .HasColumnName("MessageId");

                            b1.HasKey("ModmailTicketId");

                            b1.ToTable("ModmailTickets");

                            b1.WithOwner()
                                .HasForeignKey("ModmailTicketId");
                        });

                    b.Navigation("TicketPost");
                });
#pragma warning restore 612, 618
        }
    }
}
