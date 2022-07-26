﻿using Nellebot.Common.Extensions;
using System;

namespace Nellebot.Common.Models.UserLogs;

public class UserLog
{
    public Guid Id { get; set; }
    public ulong UserId { get; set; }
    public UserLogType LogType { get; set; }
    public string? RawValue { get; set; }
    public Type ValueType { get; set; } = typeof(string);
    public ulong? ResponsibleUserId { get; set; }
    public DateTime Timestamp { get; set; }

    public T? GetValue<T>()
    {
        if (typeof(T) != ValueType) throw new InvalidOperationException($"{typeof(T)} is not compatible with {ValueType}");

        switch (ValueType.Name)
        {
            case nameof(DateTime):
                var parsed = DateTime.TryParse(RawValue, out var parsedDate);
                if (!parsed) return default;
                return (T?)Convert.ChangeType(parsedDate, ValueType);
            case nameof(String):
            default:
                return (T?)Convert.ChangeType(RawValue, ValueType);

        }
    }

    public UserLog WithValue<T>(T value)
    {
        if (typeof(T) != ValueType) throw new InvalidOperationException($"{typeof(T)} is not compatible with {ValueType}");

        switch (ValueType.Name)
        {
            case nameof(DateTime):
                var convertedDateTime = Convert.ChangeType(value, ValueType);

                if (convertedDateTime == null)                
                    RawValue = null;                
                else                
                    RawValue = ((DateTime)convertedDateTime).ToIsoDateTimeString();
                
                break;
            case nameof(String):
            default:
                RawValue = Convert.ChangeType(value, ValueType)?.ToString();
                break;
        }

        return this;
    }
}
