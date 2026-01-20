using System.Collections.Immutable;
using System.Net;

namespace Application.Common.Abstractions.Envelope;

public abstract record EnvelopeBase
{
    public bool IsSuccess => ((int)StatusCode >= 200 && (int)StatusCode < 300);
    public string Title { get; set; } = null!;
    public string Detail { get; init; } = "";
    public HttpStatusCode StatusCode { get; init; }
    public DateTimeOffset  Timestamp { get; init; }  = DateTimeOffset.UtcNow;
    public string? RequestId { get; init; }
    
    public ImmutableDictionary<string, string[]> Errors { get; init; }
        = ImmutableDictionary<string, string[]>.Empty;
}