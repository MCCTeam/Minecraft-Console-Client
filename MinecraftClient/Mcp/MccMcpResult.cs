using System;

namespace MinecraftClient.Mcp;

public sealed class MccMcpResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public object? Data { get; init; }

    public static MccMcpResult Ok(object? data = null, string? message = null)
    {
        return new MccMcpResult
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static MccMcpResult Fail(string errorCode, string? message = null, object? data = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        return new MccMcpResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            Data = data
        };
    }
}
