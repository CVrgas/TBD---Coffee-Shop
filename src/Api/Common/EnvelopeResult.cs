using System.Net;
using Application.Common.Abstractions.Envelope;

namespace Api.Common;

public static class EnvelopeResult
{
    public static IResult ToIResult(this Envelope e) => Map(e);
    public static IResult ToIResult<T>(this Envelope<T> e) => Map(e);

    private static IResult Map(EnvelopeBase e)
    {
        return e.StatusCode switch
        {
            HttpStatusCode.OK => Results.Ok(e),
            //HttpStatusCode.Created => Results.Created(e.Location, e),
            HttpStatusCode.NoContent => Results.NoContent(),
            HttpStatusCode.NotFound => Results.NotFound(e),
            HttpStatusCode.BadRequest => Results.BadRequest(e),
            HttpStatusCode.Unauthorized => Results.Unauthorized(),
            HttpStatusCode.Forbidden => Results.Forbid(),
            HttpStatusCode.Conflict => Results.Conflict(e),
            _ => Results.StatusCode((int)e.StatusCode)
        };
    }
}