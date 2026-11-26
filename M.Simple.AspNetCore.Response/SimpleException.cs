using System.ComponentModel;
using System.Reflection;

namespace M.Simple.AspNetCore.Response;

/// <summary>A business exception whose message may be returned to the client.</summary>
public sealed class SimpleException : Exception
{
    private SimpleException(string message) : base(message) { }

    /// <summary>Creates a business exception with a client-visible message.</summary>
    public static SimpleException Of(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new SimpleException(message);
    }

    /// <summary>Uses the enum member's Description, or its name when no description is present.</summary>
    public static SimpleException Of<TCode>(TCode code) where TCode : struct, Enum
    {
        var name = Enum.GetName(code)
            ?? throw new ArgumentOutOfRangeException(nameof(code), code, "The error code must be a named enum member.");
        var description = typeof(TCode).GetField(name)!.GetCustomAttribute<DescriptionAttribute>()?.Description;
        return Of(string.IsNullOrWhiteSpace(description) ? name : description);
    }
}
