using System.Net.Http.Headers;

namespace ThunderstoreCLI.Utils;

public class RequestBuilder
{
    private UriBuilder builder { get; } = new()
    {
        Scheme = "https"
    };
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public AuthenticationHeaderValue? AuthHeader { get; set; } = null;
    public HttpContent? Content { get; set; } = null;

    public RequestBuilder() { }

    public RequestBuilder(string host)
    {
        if (host.StartsWith("https://"))
            host = host[8..];
        builder.Host = host;
    }

    public RequestBuilder StartNew()
    {
        return new(builder.Uri.Host);
    }

    public HttpRequestMessage GetRequest()
    {
        var req = new HttpRequestMessage(Method, builder.Uri)
        {
            Content = Content
        };

        req.Headers.Authorization = AuthHeader;

        return req;
    }

    public RequestBuilder WithEndpoint(string endpoint)
    {
        if (!endpoint.EndsWith('/'))
        {
            endpoint += '/';
        }
        builder.Path = endpoint;
        return this;
    }

    public RequestBuilder WithAuth(AuthenticationHeaderValue auth)
    {
        AuthHeader = auth;
        return this;
    }

    public RequestBuilder WithContent(HttpContent content)
    {
        Content = content;
        return this;
    }

    public RequestBuilder WithMethod(HttpMethod method)
    {
        Method = method;
        return this;
    }
}
