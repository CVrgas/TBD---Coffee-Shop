using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace Infrastructure.Observability;

public static class SerilogConfig
{
    /// <summary>
    /// Adds per-request correlation (TraceId, SpanId, RequestId, UserId, TenantId, Endpoint)
    /// and enables Serilog's request summary logging.
    /// </summary>
    public static WebApplication UseInfrastructureLogging(this WebApplication app)
    {
        // TODO: check and prevent password log.
        
        app.Use(async (ctx, next) =>
        {
            var activity = Activity.Current;
            var traceId  = activity?.TraceId.ToString();
            var spanId   = activity?.SpanId.ToString();

            var requestId = ctx.TraceIdentifier;
            var endpoint  = ctx.GetEndpoint()?.DisplayName;

            string? userId = null;
            if (ctx.User.Identity?.IsAuthenticated == true)
            {
                userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? ctx.User.FindFirst("sub")?.Value
                         ?? ctx.User.Identity?.Name;
            }

            var tenantId = ctx.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            tenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;

            using (LogContext.PushProperty("TraceId",   traceId))
            using (LogContext.PushProperty("SpanId",    spanId))
            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("UserId",    userId))
            using (LogContext.PushProperty("TenantId",  tenantId))
            using (LogContext.PushProperty("Endpoint",  endpoint))
            {
                await next();
            }
        });
        
        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
            opts.EnrichDiagnosticContext = (diag, httpCtx) =>
            {
                diag.Set("RequestId", httpCtx.TraceIdentifier);
                diag.Set("Endpoint",  httpCtx.GetEndpoint()?.DisplayName ?? "");

                var activity = Activity.Current;
                if (activity is not null)
                {
                    diag.Set("TraceId", activity.TraceId.ToString());
                    diag.Set("SpanId",  activity.SpanId.ToString());
                }

                string? userId = null;
                if (httpCtx.User.Identity?.IsAuthenticated == true)
                {
                    userId = httpCtx.User.FindFirst("sub")?.Value
                             ?? httpCtx.User.FindFirst("uid")?.Value
                             ?? httpCtx.User.Identity?.Name;
                }
                diag.Set("UserId",   userId ?? "");
                diag.Set("TenantId", httpCtx.Request.Headers["X-Tenant-ID"].FirstOrDefault() ?? "");
            };
        });

        return app;
    }
}
