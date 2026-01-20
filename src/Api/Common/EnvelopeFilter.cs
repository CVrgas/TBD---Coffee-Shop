using System.Diagnostics;
using Application.Common.Abstractions.Envelope;

namespace Api.Common;

public sealed class EnvelopeFilter : IEndpointFilter
{
    private const string HdrRequestId = "X-Request-ID";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var reqId = http.TraceIdentifier;
        var traceId = Activity.Current?.TraceId.ToString();

        var res = await next(ctx);
            
        if (res is not EnvelopeBase envBase) return res;
            
        var stamped = envBase with { RequestId = traceId ?? reqId, };

        http.Response.Headers[HdrRequestId] = stamped.RequestId ?? string.Empty;

        if (!stamped.IsSuccess) return ToProblemIResult(http, stamped);
            
        var t = stamped.GetType();
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Envelope<>))
            return EnvelopeResult.ToIResult((dynamic)stamped);

        return ((Envelope)stamped).ToIResult();
    }
    private static IResult ToProblemIResult(HttpContext http, EnvelopeBase env)
    {
        var status = (int)env.StatusCode;

        var type = GetProblemType(http, env);

        var extensions = new Dictionary<string, object?>(capacity: 4)
        {
            ["requestId"] = env.RequestId,
            ["timestamp"] = env.Timestamp
        };

        if (env.Errors is { Count: > 0 })
            extensions["errors"] = env.Errors;

        return Results.Problem(
            title: env.Title,
            detail: env.Detail,
            statusCode: status,
            type: type,
            instance: http.Request.Path + http.Request.QueryString,
            extensions: extensions
        );
    }

    private static string GetProblemType(HttpContext http, EnvelopeBase env)
    {
        return (int)env.StatusCode switch
        {
            StatusCodes.Status400BadRequest when env.Errors.Count > 0
                => BaseUrl(http) + "/problems/validation-error",
            StatusCodes.Status404NotFound
                => BaseUrl(http) + "/problems/not-found",
            StatusCodes.Status409Conflict
                => BaseUrl(http) + "/problems/conflict",
            _ => "about:blank"
        };
    }

    private static string BaseUrl(HttpContext http)
        => $"{http.Request.Scheme}://{http.Request.Host}";
}