using System.Net;
using System.Text.Json.Serialization;

namespace Application.Common.Abstractions.Envelope;

public sealed record Envelope : EnvelopeBase
{
    [JsonConstructor]
    private Envelope() { }
    
    public static Envelope Ok(string message = "Request completed successfully.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.OK };
    public static Envelope NotFound(string message = "The requested resource was not found.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.NotFound };
    public static Envelope BadRequest(string message = "The request could not be processed due to invalid input.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.BadRequest };
    public static Envelope InternalError(string message = "An unexpected error occurred while processing the request.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.InternalServerError };
    public static Envelope Conflict(string message = "The resource was modified by another process. Please refresh and try again.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.Conflict };
    public static Envelope Unauthorized(string message = "Authentication is required to access this resource.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.Unauthorized };
    
    public static Envelope Forbidden(string message = "You do not have the necessary permissions.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.Forbidden };
    

    public Envelope WithError(string key, string message)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(message))
            return this;

        var updated = Errors.TryGetValue(key, out var arr)
            ? Errors.SetItem(key, arr.Append(message).ToArray())
            : Errors.Add(key, new[] { message });

        return this with { Errors = updated, StatusCode = IsSuccess ? HttpStatusCode.BadRequest : StatusCode };
    }
    
    public Envelope WithError(IEnumerable<KeyValuePair<string, IEnumerable<string>>> errors)
    {
        var dict = Errors;
        foreach (var (k, msgs) in errors)
        {
            if (dict.TryGetValue(k, out var existing))
                dict = dict.SetItem(k, existing.Concat(msgs).ToArray());
            else
                dict = dict.Add(k, msgs.ToArray());
        }
        return this with { Errors = dict, StatusCode = HttpStatusCode.BadRequest };
    }
}

public sealed record Envelope<T> : EnvelopeBase
{
    [JsonConstructor]
    private Envelope() { }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }
    
    public static Envelope<T> Ok(T data, string message = "Request completed successfully.") =>
        new() { Data = data, Detail = message, StatusCode = HttpStatusCode.OK };
    public static Envelope<T> NotFound(string message = "The requested resource was not found.") =>
        new() {  Detail = message, StatusCode = HttpStatusCode.NotFound };
    public static Envelope<T> BadRequest(string message = "The request could not be processed due to invalid input.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.BadRequest };
    public static Envelope<T> InternalError(string message = "An unexpected error occurred while processing the request.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.InternalServerError };
    
    public static Envelope<T> Conflict(T data, string message = "The resource was modified by another process. Please refresh and try again.") =>
        new() { Data = data, Detail = message, StatusCode = HttpStatusCode.Conflict };
    public static Envelope<T> Unauthorized(string message = "Authentication is required to access this resource.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.Unauthorized };
    public static Envelope<T> Forbidden(string message = "You do not have the necessary permissions.") =>
        new() { Detail = message, StatusCode = HttpStatusCode.Forbidden };
    
    public Envelope<T> WithError(string key, string message)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(message))
            return this;

        var updated = Errors.TryGetValue(key, out var arr)
            ? Errors.SetItem(key, arr.Append(message).ToArray())
            : Errors.Add(key, [message]);

        return this with { Errors = updated, StatusCode = HttpStatusCode.BadRequest };
    }
    public Envelope<T> WithError(IEnumerable<KeyValuePair<string, IEnumerable<string>>> errors)
    {
        var dict = Errors;
        foreach (var (k, msgs) in errors)
        {
            dict = dict.TryGetValue(k, out var existing) 
                ? dict.SetItem(k, existing.Concat(msgs).ToArray()) 
                : dict.Add(k, msgs.ToArray());
        }
        return this with { Errors = dict, StatusCode = HttpStatusCode.BadRequest };
    }
}