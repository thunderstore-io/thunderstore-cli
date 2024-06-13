using System.Net;
using System.Security.Cryptography;
using System.Text;
using ThunderstoreCLI.Configuration;
using ThunderstoreCLI.Models;
using ThunderstoreCLI.Utils;
using static Crayon.Output;

namespace ThunderstoreCLI.Commands;

public static class PublishCommand
{
    private static readonly HttpClient HttpClient;

    static PublishCommand()
    {
        HttpClient = new HttpClient();
        HttpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        HttpClient.DefaultRequestHeaders.Add("Keep-Alive", "3600");
        HttpClient.Timeout = TimeSpan.FromHours(1);
    }

    public static int Run(Config config)
    {
        try
        {
            ValidateConfig(config);
        }
        catch (CommandException)
        {
            return 1;
        }

        string packagePath = "";
        if (!string.IsNullOrWhiteSpace(config.PublishConfig.File))
        {
            packagePath = Path.GetFullPath(config.PublishConfig.File);
        }
        else
        {
            var exitCode = BuildCommand.DoBuild(config);
            if (exitCode > 0)
            {
                return exitCode;
            }
            packagePath = config.GetBuildOutputFile();
        }

        return PublishFile(config, packagePath);
    }

    public static int PublishFile(Config config, string filepath)
    {
        Write.WithNL($"Publishing {Cyan(filepath)}", before: true, after: true);

        if (!File.Exists(filepath))
        {
            Write.ErrorExit(
                "File selected for publish was not found",
                $"Looked from: {White(Dim(filepath))}"
            );
            return 1;
        }

        UploadInitiateData uploadData;

        try
        {
            uploadData = InitiateUploadRequest(config, filepath);
        }
        catch (PublishCommandException)
        {
            return 1;
        }

        Task<CompletedUpload.CompletedPartData>[] uploadTasks;

        try
        {
            uploadTasks = uploadData.UploadUrls!.Select(  // Validated in InitiateUploadRequest
                partData => UploadChunk(partData, filepath)
            ).ToArray();
        }
        catch (PublishCommandException)
        {
            return 1;
        }

        string uploadUuid = uploadData.Metadata?.UUID!;  // Validated in InitiateUploadRequest

        try
        {
            var spinner = new ProgressSpinner("chunks uploaded", uploadTasks);
            spinner.Spin().GetAwaiter().GetResult();
        }
        catch (SpinnerException)
        {
            HttpClient.Send(config.Api.AbortUploadMedia(uploadUuid));
            return 1;
        }

        var uploadedParts = uploadTasks.Select(x => x.Result).ToArray();

        try
        {
            var response = HttpClient.Send(
                config.Api.FinishUploadMedia(
                    new CompletedUpload
                    {
                        Parts = uploadedParts
                    },
                    uploadUuid
                )
            );

            HandleRequestError("Finishing usermedia upload", response);
            Write.Success("Successfully finalized the upload");
        }
        catch (PublishCommandException)
        {
            return 1;
        }

        try
        {
            PublishPackageRequest(config, uploadUuid);
        }
        catch (PublishCommandException)
        {
            return 1;
        }

        return 0;
    }

    private static UploadInitiateData InitiateUploadRequest(Config config, string filepath)
    {
        var response = HttpClient.Send(config.Api.StartUploadMedia(filepath));

        HandleRequestError("initializing the upload", response, HttpStatusCode.Created);

        using var responseReader = new StreamReader(response.Content.ReadAsStream());
        var responseContent = responseReader.ReadToEnd();
        var uploadData = UploadInitiateData.Deserialize(responseContent);

        if (uploadData is null)
        {
            Write.ErrorExit("Undeserializable InitiateUploadRequest response:", Dim(responseContent));
            throw new PublishCommandException();
        }

        if (uploadData.Metadata?.Filename is null || uploadData.Metadata?.UUID is null)
        {
            Write.ErrorExit("No valid Metadata found in InitiateUploadRequest response:", Dim(responseContent));
            throw new PublishCommandException();
        }

        if (uploadData.UploadUrls is null)
        {
            Write.ErrorExit("No valid UploadUrls found in InitiateUploadRequest response:", Dim(responseContent));
            throw new PublishCommandException();
        }

        var details = $"({MiscUtils.GetSizeString(uploadData.Metadata.Size)}) in {uploadData.UploadUrls.Length} chunks...";
        Write.WithNL($"Uploading {Cyan(uploadData.Metadata.Filename)} {details}", after: true);

        return uploadData;
    }

    private static void PublishPackageRequest(Config config, string uploadUuid)
    {
        var response = HttpClient.Send(config.Api.SubmitPackage(uploadUuid));

        HandleRequestError("publishing package", response);

        using var responseReader = new StreamReader(response.Content.ReadAsStream());
        var responseContent = responseReader.ReadToEnd();
        var jsonData = PublishData.Deserialize(responseContent);

        if (jsonData?.PackageVersion?.DownloadUrl is null)
        {
            Write.ErrorExit(
                "Field package_version.download_url missing from PublishPackageRequest response:",
                Dim(responseContent)
            );
            throw new PublishCommandException();
        }

        Write.Success($"Successfully published {Cyan($"{config.PackageConfig.Namespace}-{config.PackageConfig.Name}")}");
        Write.Line($"It's available at {Cyan(jsonData.PackageVersion.DownloadUrl)}");
    }

    private static async Task<CompletedUpload.CompletedPartData> UploadChunk(UploadInitiateData.UploadPartData part, string filepath)
    {
        try
        {
            await Task.Yield();
            await using var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(part.Offset, SeekOrigin.Begin);

            byte[] hash;
            var chunk = new MemoryStream();
            const int blocksize = 65536;

            using (var reader = new BinaryReader(stream, Encoding.Default, true))
            {
                using (var md5 = MD5.Create())
                {
                    md5.Initialize();
                    var length = part.Length;

                    while (length > blocksize)
                    {
                        length -= blocksize;
                        var bytes = reader.ReadBytes(blocksize);
                        md5.TransformBlock(bytes, 0, blocksize, null, 0);
                        await chunk.WriteAsync(bytes);
                    }

                    var finalBytes = reader.ReadBytes(length);
                    md5.TransformFinalBlock(finalBytes, 0, length);

                    if (md5.Hash is null)
                    {
                        Write.ErrorExit($"MD5 hashing failed for part #{part.PartNumber})");
                        throw new PublishCommandException();
                    }

                    hash = md5.Hash;
                    await chunk.WriteAsync(finalBytes);
                    chunk.Position = 0;
                }
            }

            var request = new HttpRequestMessage(HttpMethod.Put, part.Url);
            request.Content = new StreamContent(chunk);
            request.Content.Headers.ContentMD5 = hash;
            request.Content.Headers.ContentLength = part.Length;

            using var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Write.Empty();
                Write.ErrorExit(await response.Content.ReadAsStringAsync());
                throw new PublishCommandException();
            }

            if (response.Headers.ETag is null)
            {
                Write.Empty();
                Write.ErrorExit($"Response contained no ETag for part #{part.PartNumber}");
                throw new PublishCommandException();
            }

            return new CompletedUpload.CompletedPartData
            {
                ETag = response.Headers.ETag.Tag,
                PartNumber = part.PartNumber
            };
        }
        catch (Exception e)
        {
            Write.Empty();
            Write.ErrorExit($"Exception occured while uploading file chunk #{part.PartNumber}:", e.ToString());
            throw new PublishCommandException();
        }
    }

    private static void HandleRequestError(
        string step,
        HttpResponseMessage response,
        HttpStatusCode expectedStatus = HttpStatusCode.OK
    )
    {
        if (response.StatusCode == expectedStatus)
        {
            return;
        }

        using var responseReader = new StreamReader(response.Content.ReadAsStream());
        Write.ErrorExit(
            $"Unexpected response from the server while {step}:",
            $"Status code: {response.StatusCode:D} {response.StatusCode}",
            Dim(responseReader.ReadToEnd())
        );
        throw new PublishCommandException();
    }

    private static void ValidateConfig(Config config, bool justReturnErrors = false)
    {
        var buildConfigErrors = BuildCommand.ValidateConfig(config, false);
        var v = new CommandValidator("publish", buildConfigErrors);
        v.AddIfEmpty(config.AuthConfig.AuthToken, "Auth AuthToken");
        v.ThrowIfErrors();
    }
}

[Serializable]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
public class PublishCommandException : Exception
{
    public PublishCommandException()
    {
    }

    public PublishCommandException(string message)
        : base(message)
    {
    }

    public PublishCommandException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
