using System;
using DSharpPlus.Entities;

namespace Nellebot.Utils;

public class InteractionException : Exception
{
    public InteractionException(DiscordInteraction interaction, string message)
        : base(message)
    {
        Interaction = interaction;
    }

    public InteractionException(DiscordInteraction interaction, string message, Exception innerException)
        : base(message, innerException)
    {
        Interaction = interaction;
    }

    public DiscordInteraction Interaction { get; }
}
