﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.UserRoles;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.CommandModules.Roles;

public class RoleModule : ApplicationCommandModule
{
    private const int MaxSelectComponentOptions = 25;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;
    private readonly RoleService _roleService;

    public RoleModule(RoleService roleService, IOptions<BotOptions> options, IDiscordErrorLogger discordErrorLogger)
    {
        _roleService = roleService;
        _discordErrorLogger = discordErrorLogger;
        _options = options.Value;
    }

    [SlashCommand("role", "Choose a new role or remove an existing role")]
    public async Task SetSelfRole(InteractionContext ctx, [Option("role", "Role name or alias")] string role)
    {
        DiscordMember member = ctx.Member ?? throw new Exception($"{nameof(ctx.Member)} is null");

        await ctx.DeferAsync(true);

        UserRole? userRole = await _roleService.GetUserRoleByNameOrAlias(role);

        if (userRole == null)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{role}** role does not exist"));
            return;
        }

        DiscordRole? existingDiscordRole = member.Roles.SingleOrDefault(r => r.Id == userRole.RoleId);

        if (existingDiscordRole == null)
        {
            await AddSelfDiscordRole(ctx, userRole);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Enjoy your new role!"));
        }
        else
        {
            await member.RevokeRoleAsync(existingDiscordRole);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are now one role lighter."));
        }
    }

    [SlashCommand("test", "Test")]
    public async Task TestSlashCommand(InteractionContext ctx)
    {
        // Respond with either CreateResponseAsync directly
        // Or DeferAsync + EditResponseAsync
        var aButton = new DiscordButtonComponent(DiscordButtonStyle.Primary, "aButton", "Click me");

        DiscordInteractionResponseBuilder interactionBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("I does da test")
            .AsEphemeral()
            .AddComponents(aButton);

        await ctx.CreateResponseAsync(interactionBuilder);

        // The original response is the bot's response to the command
        DiscordMessage originalResponse = await ctx.GetOriginalResponseAsync();

        /*
         *  await ctx.DeferAsync(true);
         *
         *  var webhookBuilder = new DiscordWebhookBuilder()
         *      .WithContent("I does da test")
         *      .AddComponents(aButton);
         *
         *  // When using the EditResponse method, there's no need to call getOriginalResponseAsync()
         *  var originalResponse = await ctx.EditResponseAsync(webhookBuilder);
         */

        // At this point, regardless of answering with Create or Defer + Edit
        // We're done with the Slash command interaction
        InteractivityResult<ComponentInteractionCreateEventArgs> interactionResult =
            await originalResponse.WaitForButtonAsync();

        DiscordInteractionResponseBuilder responseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent("Thanks for clicking me");

        await interactionResult.Result.Interaction.CreateResponseAsync(
            DiscordInteractionResponseType.UpdateMessage,
            responseBuilder);
    }

    [SlashCommand("roles", "Choose one or more roles")]
    public Task Roles(InteractionContext ctx)
    {
        try
        {
            DiscordMember member = ctx.Member ?? throw new Exception("Not a guild member");

            bool hasMemberRole = member.Roles.Any(r => r.Id == _options.MemberRoleId);

            return hasMemberRole ? MemberRoleFlow(ctx) : NewUserRoleFlow(ctx);
        }
        catch (Exception ex)
        {
            // TODO add better error handling for Slash commands
            _discordErrorLogger.LogError(ex, ex.Message);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Role chooser flow for new users.
    ///     Present each role category input one after the other.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task NewUserRoleFlow(InteractionContext ctx)
    {
        // Acknowledge the command interaction by responding with a "thinking..." message
        await ctx.DeferAsync(true);

        DiscordMember user = ctx.Member;

        IEnumerable<UserRole> userRoles = await _roleService.GetRoleList();

        List<IGrouping<UserRoleGroup?, UserRole>> roleGroups = userRoles
            .GroupBy(r => r.Group)
            .OrderBy(r => r.Key?.Id ?? int.MaxValue)
            .ToList();

        var rolesToAdd = new List<ulong>();
        var rolesToRemove = new List<ulong>();

        const string roleDropdownId = "dropdown_role";

        foreach (IGrouping<UserRoleGroup?, UserRole> roleGroup in roleGroups)
        {
            // TODO: Temporary(tm) hack. Mandatory should be a flag on the group
            bool isMandatory = roleGroup == roleGroups.First();

            const string skipButtonId = "skip_button";
            var skipButton = new DiscordButtonComponent(
                DiscordButtonStyle.Secondary,
                skipButtonId,
                "Skip",
                isMandatory);

            DiscordMessage theMessage = await PresentRolesDropdown(ctx, roleGroup, user, roleDropdownId, skipButton);

            // Wait for a choice, either dropdown selection or skip button
            var messageCancellationTokenSource = new CancellationTokenSource();
            Task<InteractivityResult<ComponentInteractionCreateEventArgs>> waitForSelectTask =
                theMessage.WaitForSelectAsync(roleDropdownId, messageCancellationTokenSource.Token);
            Task<InteractivityResult<ComponentInteractionCreateEventArgs>> waitForButtonTask =
                theMessage.WaitForButtonAsync(skipButtonId, messageCancellationTokenSource.Token);

            InteractivityResult<ComponentInteractionCreateEventArgs> roleDropdownInteractionResult =
                (await Task.WhenAny(waitForSelectTask, waitForButtonTask)).Result;
            messageCancellationTokenSource.Cancel();

            // Acknowledge the interaction.
            // This deferred message will be handled either in the next interation or outside the loop when we're done
            await roleDropdownInteractionResult.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType
                    .DeferredMessageUpdate);

            bool isSkipped = roleDropdownInteractionResult.Result.Id == skipButtonId;

            if (isSkipped) continue;

            List<ulong> chosenRoleIds = roleDropdownInteractionResult.Result.Values.Select(ulong.Parse).ToList();

            List<ulong> rolesFromGroupToAdd = chosenRoleIds
                .Except(user.Roles.Select(r => r.Id))
                .ToList();

            rolesToAdd.AddRange(rolesFromGroupToAdd);

            IEnumerable<ulong> rolesFromGroupToRemove = user.Roles.Select(r => r.Id)
                .Intersect(roleGroup.ToList().Select(r => r.RoleId))
                .Except(chosenRoleIds);

            rolesToRemove.AddRange(rolesFromGroupToRemove);
        }

        foreach (ulong roleToAdd in rolesToAdd)
        {
            await GrantDiscordRole(ctx, roleToAdd);
        }

        foreach (ulong roleToRemove in rolesToRemove)
        {
            await RevokeDiscordRole(ctx, roleToRemove);
        }

        // Respond by editing the deferred message with a thank you message
        DiscordWebhookBuilder responseBuilder = new DiscordWebhookBuilder().WithContent("Enjoy your new roles!");

        await ctx.EditResponseAsync(responseBuilder);
    }

    /// <summary>
    ///     Role chooser flow for existing users aka members
    ///     Executing the command first prompts the user to choose
    ///     the role category they want to choose roles from.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task MemberRoleFlow(InteractionContext ctx)
    {
        // Acknowledge the command interaction by responding with a "thinking..." message
        await ctx.DeferAsync(true);

        DiscordMember user = ctx.Member;

        IEnumerable<UserRole> userRoles = await _roleService.GetRoleList();

        List<IGrouping<UserRoleGroup?, UserRole>> roleGroups = userRoles
            .GroupBy(r => r.Group)
            .OrderBy(r => r.Key?.Id ?? int.MaxValue)
            .ToList();

        const string exitButtonId = "exit_button";
        const string roleDropdownId = "dropdown_role";
        const string backButtonId = "back_button";

        while (true)
        {
            DiscordMessage theMessage = await PresentCategoriesMenu(ctx, roleGroups, exitButtonId);

            InteractivityResult<ComponentInteractionCreateEventArgs> theMessageInteractivityResult =
                await theMessage.WaitForButtonAsync();

            // Acknowledge the interaction
            await theMessageInteractivityResult.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType
                    .DeferredMessageUpdate);

            string chosenButtonId = theMessageInteractivityResult.Result.Id;

            if (chosenButtonId == exitButtonId) break;

            bool choiceIsRoleGroup = chosenButtonId != "none";

            IGrouping<UserRoleGroup?, UserRole> roleGroupToChange = choiceIsRoleGroup
                ? roleGroups.Single(g => g.Key?.Id == uint.Parse(chosenButtonId))
                : roleGroups.Single(g => g.Key == null);

            var backButton = new DiscordButtonComponent(DiscordButtonStyle.Secondary, backButtonId, "Back");

            theMessage = await PresentRolesDropdown(ctx, roleGroupToChange, user, roleDropdownId, backButton);

            // Wait for a choice, either dropdown selection or skip button
            var messageCancellationTokenSource = new CancellationTokenSource();
            Task<InteractivityResult<ComponentInteractionCreateEventArgs>> waitForSelectTask =
                theMessage.WaitForSelectAsync(roleDropdownId, messageCancellationTokenSource.Token);
            Task<InteractivityResult<ComponentInteractionCreateEventArgs>> waitForButtonTask =
                theMessage.WaitForButtonAsync(backButtonId, messageCancellationTokenSource.Token);

            InteractivityResult<ComponentInteractionCreateEventArgs> roleDropdownInteractionResult =
                (await Task.WhenAny(waitForSelectTask, waitForButtonTask)).Result;
            messageCancellationTokenSource.Cancel();

            // Just acknowledge interaction
            await roleDropdownInteractionResult.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType
                    .DeferredMessageUpdate);

            bool isBackOption = roleDropdownInteractionResult.Result.Id == backButtonId;

            if (isBackOption) continue;

            List<ulong> chosenRoleIds = roleDropdownInteractionResult.Result.Values.Select(ulong.Parse).ToList();

            List<ulong> rolesToAdd = chosenRoleIds
                .Except(user.Roles.Select(r => r.Id))
                .ToList();

            foreach (ulong roleToAdd in rolesToAdd)
            {
                await GrantDiscordRole(ctx, roleToAdd);
            }

            IEnumerable<ulong> rolesToRemove = user.Roles.Select(r => r.Id)
                .Intersect(roleGroupToChange.ToList().Select(r => r.RoleId))
                .Except(chosenRoleIds);

            foreach (ulong roleToRemove in rolesToRemove)
            {
                await RevokeDiscordRole(ctx, roleToRemove);
            }
        }

        // Respond by editing the deferred message with a thank you message
        DiscordWebhookBuilder responseBuilder = new DiscordWebhookBuilder().WithContent("Enjoy your new roles!");

        await ctx.Interaction.EditOriginalResponseAsync(responseBuilder);
    }

    private async Task<DiscordMessage> PresentCategoriesMenu(
        InteractionContext ctx,
        List<IGrouping<UserRoleGroup?, UserRole>> roleGroups,
        string exitButtonId)
    {
        var categoryButtonRow = new List<DiscordComponent>();

        foreach (IGrouping<UserRoleGroup?, UserRole> roleGroup in roleGroups)
        {
            string buttonId = roleGroup.Key?.Id.ToString() ?? "none";
            string buttonLabel = roleGroup.Key != null ? $"{roleGroup.Key.Name} roles" : "Ungrouped roles";

            categoryButtonRow.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, buttonId, buttonLabel));
        }

        var exitButton = new DiscordButtonComponent(DiscordButtonStyle.Secondary, exitButtonId, "Exit");

        DiscordWebhookBuilder roleGroupButtonsResponse = new DiscordWebhookBuilder()
            .WithContent("Select a role group")
            .AddComponents(categoryButtonRow)
            .AddComponents(exitButton);

        // Respond by editing the deferred message
        return await ctx.EditResponseAsync(roleGroupButtonsResponse);
    }

    private async Task<DiscordMessage> PresentRolesDropdown(
        InteractionContext ctx,
        IGrouping<UserRoleGroup?, UserRole> roleGroupToChange,
        DiscordMember user,
        string roleDropdownId,
        DiscordButtonComponent extraButton)
    {
        var roleDropdownOptions = new List<DiscordSelectComponentOption>();

        foreach (UserRole userRole in roleGroupToChange)
        {
            bool userHasRole = user.Roles.Select(r => r.Id).Contains(userRole.RoleId);

            roleDropdownOptions.Add(
                new DiscordSelectComponentOption(
                    userRole.Name,
                    userRole.RoleId.ToString(),
                    userRole.Name,
                    userHasRole));
        }

        bool isSingleSelect = roleGroupToChange.Key != null && roleGroupToChange.Key.MutuallyExclusive;

        int maxOptions = isSingleSelect ? 1 : Math.Min(roleDropdownOptions.Count, MaxSelectComponentOptions);
        int minOptions = isSingleSelect ? 1 : 0;

        string roleDropdownPlaceHolder = isSingleSelect ? "Choose 1 role" : "Choose any roles";
        var roleDropdown = new DiscordSelectComponent(
            roleDropdownId,
            roleDropdownPlaceHolder,
            roleDropdownOptions,
            minOptions: minOptions,
            maxOptions: maxOptions);

        DiscordWebhookBuilder buttonInteractionResultResponse = new DiscordWebhookBuilder()
            .WithContent(roleGroupToChange.Key != null ? $"{roleGroupToChange.Key.Name} roles" : "Ungrouped roles")
            .AddComponents(roleDropdown)
            .AddComponents(extraButton);

        // Respond by editing the deferred message
        return await ctx.EditResponseAsync(buttonInteractionResultResponse);
    }

    private Task GrantDiscordRole(InteractionContext ctx, ulong discordRoleId)
    {
        DiscordMember member = ctx.Member;
        DiscordGuild guild = ctx.Guild;

        if (!guild.Roles.ContainsKey(discordRoleId))
        {
            throw new ArgumentException($"Role Id {discordRoleId} does not exist");
        }

        DiscordRole discordRole = ctx.Guild.Roles[discordRoleId];

        return member.GrantRoleAsync(discordRole);
    }

    private Task RevokeDiscordRole(InteractionContext ctx, ulong discordRoleId)
    {
        DiscordMember member = ctx.Member;
        DiscordGuild guild = ctx.Guild;

        if (!guild.Roles.ContainsKey(discordRoleId))
        {
            throw new ArgumentException($"Role Id {discordRoleId} does not exist");
        }

        DiscordRole discordRole = ctx.Guild.Roles[discordRoleId];

        return member.RevokeRoleAsync(discordRole);
    }

    private async Task AddSelfDiscordRole(InteractionContext ctx, UserRole userRole)
    {
        DiscordMember member = ctx.Member ?? throw new Exception($"{nameof(ctx.Member)} is null");
        DiscordGuild guild = ctx.Guild;

        // Check that the role exists in the guild
        if (!guild.Roles.ContainsKey(userRole.RoleId))
        {
            throw new ArgumentException($"Role Id {userRole.RoleId} does not exist");
        }

        // Assign new role
        DiscordRole discordRole = ctx.Guild.Roles[userRole.RoleId];

        await member.GrantRoleAsync(discordRole);

        // If user role is part of a mutually exclusive group, remove all other roles from the group
        await RemoveAllOtherRolesInMutexGroup(userRole, member, guild);
    }

    private async Task RemoveAllOtherRolesInMutexGroup(UserRole userRole, DiscordMember member, DiscordGuild guild)
    {
        if (userRole.Group == null || userRole.Group.MutuallyExclusive == false)
        {
            return;
        }

        IEnumerable<UserRole> otherRolesInGroup = (await _roleService.GetUserRolesByGroup(userRole.Group.Id))
            .Where(r => r.Id != userRole.Id);

        List<DiscordRole> discordRolesInGroupToRemove = member.Roles
            .Where(
                r => otherRolesInGroup.Select(r => r.RoleId)
                    .ToList()
                    .Contains(r.Id))
            .ToList();

        foreach (DiscordRole discordRoleToRemove in discordRolesInGroupToRemove)
        {
            await member.RevokeRoleAsync(discordRoleToRemove);
        }
    }
}
