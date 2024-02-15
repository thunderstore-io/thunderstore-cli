using System.Net.Http.Headers;
using System.Text;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;

namespace ThunderstoreCLI.API;

public class ApiHelper
{
    private Config Config { get; }
    private RequestBuilder BaseRequestBuilder { get; }

    private readonly Lazy<AuthenticationHeaderValue> authHeader;

    private AuthenticationHeaderValue AuthHeader => authHeader.Value;

    public ApiHelper(Config config)
    {
        Config = config;
        BaseRequestBuilder = new RequestBuilder(config.GeneralConfig.Repository ?? throw new Exception("Repository can't be empty"));
        authHeader = new Lazy<AuthenticationHeaderValue>(() =>
        {
            if (string.IsNullOrEmpty(Config.AuthConfig.AuthToken))
                throw new Exception("This action requires an auth token");
            return new AuthenticationHeaderValue("Bearer", Config.AuthConfig.AuthToken);
        });
    }

    private const string V1 = "api/v1/";
    private const string EXPERIMENTAL = "api/experimental/";
    private const string COMMUNITY = "c/";

    public HttpRequestMessage SubmitPackage(string fileUuid)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + "submission/submit/")
            .WithMethod(HttpMethod.Post)
            .WithAuth(AuthHeader)
            .WithContent(new StringContent(Config.GetUploadMetadata(fileUuid).Serialize(), Encoding.UTF8, "application/json"))
            .GetRequest();
    }

    public HttpRequestMessage StartUploadMedia(string filePath)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + "usermedia/initiate-upload/")
            .WithMethod(HttpMethod.Post)
            .WithAuth(AuthHeader)
            .WithContent(new StringContent(SerializeFileData(filePath), Encoding.UTF8, "application/json"))
            .GetRequest();
    }

    public HttpRequestMessage FinishUploadMedia(CompletedUpload finished, string uuid)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + $"usermedia/{uuid}/finish-upload/")
            .WithMethod(HttpMethod.Post)
            .WithAuth(AuthHeader)
            .WithContent(new StringContent(finished.Serialize(), Encoding.UTF8, "application/json"))
            .GetRequest();
    }

    public HttpRequestMessage AbortUploadMedia(string uuid)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + $"usermedia/{uuid}/abort-upload/")
            .WithMethod(HttpMethod.Post)
            .WithAuth(AuthHeader)
            .GetRequest();
    }

    public HttpRequestMessage GetPackageMetadata(string author, string name)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + $"package/{author}/{name}/")
            .GetRequest();
    }

    public HttpRequestMessage GetPackageVersionMetadata(string author, string name, string version)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(EXPERIMENTAL + $"package/{author}/{name}/{version}/")
            .GetRequest();
    }

    public HttpRequestMessage GetPackagesV1()
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(V1 + "package/")
            .GetRequest();
    }

    public HttpRequestMessage GetPackagesV1(string community)
    {
        return BaseRequestBuilder
            .StartNew()
            .WithEndpoint(COMMUNITY + community + "/api/v1/package/")
            .GetRequest();
    }

    private static string SerializeFileData(string filePath)
    {
        return new FileData
        {
            Filename = Path.GetFileName(filePath),
            Filesize = new FileInfo(filePath).Length
        }.Serialize();
    }
}
