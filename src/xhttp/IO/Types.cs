namespace Xhttp.IO;

internal static class Types
{
    // BCL
    public const string Encoding = "global::System.Text.Encoding";
    public const string Task     = "global::System.Threading.Tasks.Task";

    // JSON Serialization
    public const string JsonSerializerContext = "global::System.Text.Json.Serialization.JsonSerializerContext";
    public const string JsonSerializerOptions = "global::System.Text.Json.JsonSerializerOptions";
    public const string JsonTypeInfo          = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";

    // HTTP
    public const string HttpClient         = "global::System.Net.Http.HttpClient";
    public const string HttpMethod         = "global::System.Net.Http.HttpMethod";
    public const string HttpRequestMessage = "global::System.Net.Http.HttpRequestMessage";
    public const string HttpUtility        = "global::System.Web.HttpUtility";
    public const string StringBuilder      = "global::System.Text.StringBuilder";

    // HTTP Contents
    public const string HttpContent    = "global::System.Net.Http.HttpContent";
    public const string JsonContent      = "global::System.Net.Http.Json.JsonContent";
    public const string StreamContent    = "global::System.Net.Http.StreamContent";
    public const string ByteArrayContent = "global::System.Net.Http.ByteArrayContent";
}
