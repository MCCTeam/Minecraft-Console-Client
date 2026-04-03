using System;

namespace MinecraftClient.Scripting;

/// <summary>
/// Represents the outcome of a shared MCC game operation.
/// </summary>
public class MccGameResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static MccGameResult Ok(string? message = null)
    {
        return new MccGameResult
        {
            Success = true,
            Message = message
        };
    }

    public static MccGameResult Fail(string errorCode, string? message = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        return new MccGameResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

/// <summary>
/// Represents the outcome of a shared MCC game operation with typed payload data.
/// </summary>
public sealed class MccGameResult<T> : MccGameResult
{
    public T? Data { get; init; }

    public static MccGameResult<T> Ok(T? data, string? message = null)
    {
        return new MccGameResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static MccGameResult<T> Fail(string errorCode, string? message = null, T? data = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        return new MccGameResult<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Data = data
        };
    }
}
