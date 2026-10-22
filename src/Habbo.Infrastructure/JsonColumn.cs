using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Habbo.Infrastructure;

/// <summary>Stores a value that nests, and is only ever read and written whole, as one jsonb column.</summary>
internal static class JsonColumn
{
    public static PropertyBuilder<T?> StoredAsJson<T>(this PropertyBuilder<T?> property) where T : class =>
        property.HasColumnType("jsonb").HasConversion(
            new ValueConverter<T?, string?>(value => Write(value), json => Read<T>(json)),
            new ValueComparer<T?>((a, b) => Write(a) == Write(b), value => (Write(value) ?? string.Empty).GetHashCode(), value => Read<T>(Write(value))));

    private static string? Write<T>(T? value) where T : class => value is null ? null : JsonSerializer.Serialize(value);

    private static T? Read<T>(string? json) where T : class => json is null ? null : JsonSerializer.Deserialize<T>(json);
}
