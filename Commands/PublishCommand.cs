using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Crayon.Output;

namespace ThunderstoreCLI.Commands
{
    public static class PublishCommand
    {
        public static int Run(PublishOptions options, Config.Config config)
        {
            var configPath = config.GetProjectConfigPath();
            if (!File.Exists(configPath))
            {
                Console.WriteLine(Red($"ERROR: Configuration file not found, looked from: {White(Dim(configPath))}"));
                Console.WriteLine(Red("A project configuration file is required for the publish command."));
                Console.WriteLine(Red("You can initialize one with the 'init' command."));
                Console.WriteLine(Red("Exiting"));
                return 1;
            }

            string packagePath = "";
            if (!string.IsNullOrWhiteSpace(options.File))
            {
                var filePath = Path.GetFullPath(options.File);
                if (!File.Exists(filePath))
                {
                    Console.WriteLine(Red($"ERROR: The provided file does not exist."));
                    Console.WriteLine(Red($"Searched path: {White(Dim(filePath))}"));
                    Console.WriteLine(Red("Exiting"));
                    return 1;
                }
                packagePath = filePath;
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

            return PublishFile(options, config, packagePath);
        }

        public static int PublishFile(PublishOptions options, Config.Config config, string filepath)
        {
            Console.WriteLine();
            Console.WriteLine($"Publishing {Cyan(filepath)}");
            Console.WriteLine();

            if (!File.Exists(filepath))
            {
                Console.WriteLine(Red($"ERROR: File selected for publish was not found"));
                Console.WriteLine(Red($"Looked from: {White(Dim(filepath))}"));
                Console.WriteLine(Red("Exiting"));
                return 1;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = config.GetAuthHeader();
            UploadInitiateData uploadData;

            try
            {
                uploadData = InitiateUploadRequest(client, config, filepath);
            }
            catch (PublishCommandException)
            {
                return 1;
            }

            using var partClient = new HttpClient();
            partClient.Timeout = new TimeSpan(72, 0, 0);

            async Task<(bool completed, CompletedPartData data)> UploadChunk(UploadInitiateData.UploadPartData part)
            {
                try
                {
                    await using var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    stream.Seek(part.Offset, SeekOrigin.Begin);

                    byte[] hash;
                    using (var reader = new BinaryReader(stream, Encoding.Default, true))
                    {
                        using (var md5 = MD5.Create())
                        {
                            md5.Initialize();
                            var length = part.Length;
                            while (length > md5.InputBlockSize)
                            {
                                length -= md5.InputBlockSize;
                                md5.TransformBlock(reader.ReadBytes(md5.InputBlockSize), 0, md5.InputBlockSize, null, 0);
                            }
                            md5.TransformFinalBlock(reader.ReadBytes(length), 0, length);
                            hash = md5.Hash;
                        }
                    }

                    stream.Seek(part.Offset, SeekOrigin.Begin);

                    var partRequest = new HttpRequestMessage(HttpMethod.Put, part.Url)
                    {
                        Content = new StreamContent(stream, part.Length)
                    };

                    partRequest.Content.Headers.ContentMD5 = hash;

                    // ReSharper disable once AccessToDisposedClosure
                    // These tasks won't ever run past the client instance's lifetime
                    using var response = await partClient.SendAsync(partRequest);

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch
                    {
                        Console.WriteLine(Red(await response.Content.ReadAsStringAsync()));
                        throw;
                    }

                    return (true, new CompletedPartData()
                    {
                        ETag = response.Headers.ETag.Tag,
                        PartNumber = part.PartNumber
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(Red("Exception occured while uploading file chunk:"));
                    Console.WriteLine(Red(e.ToString()));

                    return (false, null);
                }
            }

            var uploadTasks = uploadData.UploadUrls.Select(UploadChunk).ToArray();

            static async Task<bool> ProgressBar(Task<(bool, CompletedPartData)>[] tasks)
            {
                ushort spinIndex = 0;
                string[] spinChars = { "|", "/", "-", "\\", "|", "/", "-", "\\" };
                while (true)
                {
                    if (tasks.Any(x => x.IsCompleted && !x.Result.Item1))
                    {
                        Console.WriteLine();
                        return false;
                    }

                    var completed = tasks.Count(static x => x.IsCompleted);

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(Green($"{completed}/{tasks.Length} chunks uploaded...{spinChars[spinIndex++ % spinChars.Length]}"));

                    if (completed == tasks.Length)
                    {
                        Console.WriteLine();
                        return true;
                    }

                    await Task.Delay(200);
                }
            }

            if (!ProgressBar(uploadTasks).Result)
            {
                var abortRequest = new HttpRequestMessage(HttpMethod.Post, config.GetUserMediaUploadAbortUrl(uploadData.Metadata.UUID));
                abortRequest.Headers.Authorization = config.GetAuthHeader();
                client.Send(abortRequest);
                return 1;
            }

            var uploadedParts = uploadTasks.Select(x => x.Result.data).ToArray();

            var finishRequest = new HttpRequestMessage(HttpMethod.Post, config.GetUserMediaUploadFinishUrl(uploadData.Metadata.UUID));
            finishRequest.Content = new StringContent(JsonSerializer.Serialize(new CompletedUpload()
            {
                Parts = uploadedParts
            }), Encoding.UTF8, "application/json");
            client.Send(finishRequest);

            var publishPackageRequest = new HttpRequestMessage(HttpMethod.Post, config.GetPackageSubmitUrl());
            publishPackageRequest.Content = new StringContent(SerializeUploadMeta(config, uploadData.Metadata.UUID), Encoding.UTF8, "application/json");
            var publishResponse = client.Send(publishPackageRequest);
            var responseReader = new StreamReader(publishResponse.Content.ReadAsStream());
            var publishResponseContent = responseReader.ReadToEnd();

            if (publishResponse.StatusCode == HttpStatusCode.OK)
            {
                var jsonData = JsonSerializer.Deserialize<PublishData>(publishResponseContent);
                Console.WriteLine(Blue($"Successfully published {config.PackageMeta.Namespace}-{config.PackageMeta.Name}"));
                Console.WriteLine(Blue($"It's available at: {jsonData.PackageVersion.DownloadUrl}"));
                return 0;
            }
            else
            {
                Console.WriteLine(Red($"ERROR: Unexpected response from the server"));
                Console.WriteLine(Red($"Details:"));
                Console.WriteLine($"Status code: {publishResponse.StatusCode:D} {publishResponse.StatusCode}");
                Console.WriteLine(Dim(publishResponseContent));
                return 1;
            }
        }

        private static UploadInitiateData InitiateUploadRequest(HttpClient client, Config.Config config, string filepath)
        {
            var url = config.GetUserMediaUploadInitiateUrl();
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(SerializeFileData(filepath), Encoding.UTF8, "application/json")
            };
            var response = client.Send(request);

            HandleRequestError("initializing the upload", response, HttpStatusCode.Created);

            using var responseReader = new StreamReader(response.Content.ReadAsStream());
            var uploadData = JsonSerializer.Deserialize<UploadInitiateData>(responseReader.ReadToEnd());

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            long size = uploadData.Metadata.Size;
            while (size >= 1024 && suffixIndex < suffixes.Length)
            {
                size /= 1024;
                suffixIndex++;
            }

            Console.WriteLine(Cyan($"Uploading {uploadData.Metadata.Filename} ({size}{suffixes[suffixIndex]}) in {uploadData.UploadUrls.Length} chunks..."));
            Console.WriteLine();

            return uploadData;
        }

        private static void HandleRequestError(
            string step,
            HttpResponseMessage response,
            HttpStatusCode expectedStatus = HttpStatusCode.OK
        )
        {
            if (response.StatusCode == expectedStatus) {
                return;
            }

            using var responseReader = new StreamReader(response.Content.ReadAsStream());
            Console.WriteLine(Red($"ERROR: Unexpected response from the server while {step}:"));
            Console.WriteLine($"Status code: {response.StatusCode:D} {response.StatusCode}");
            Console.WriteLine(Dim(responseReader.ReadToEnd()));
            throw new PublishCommandException();
        }

        public static string SerializeUploadMeta(Config.Config config, string fileUuid)
        {
            var meta = new PackageUploadMetadata()
            {
                AuthorName = config.PackageMeta.Namespace,
                Categories = config.PublishConfig.Categories,
                Communities = config.PublishConfig.Communities,
                HasNsfwContent = config.PackageMeta.ContainsNsfwContent == true,
                UploadUUID = fileUuid
            };
            return JsonSerializer.Serialize(meta);
        }

        public static string SerializeFileData(string filePath)
        {
            return JsonSerializer.Serialize(new FileData()
            {
                Filename = Path.GetFileName(filePath),
                Filesize = new FileInfo(filePath).Length
            });
        }

        public class FileData
        {
            [JsonPropertyName("filename")]
            public string Filename { get; set; }

            [JsonPropertyName("file_size_bytes")]
            public long Filesize { get; set; }
        }

        public class CompletedUpload
        {
            [JsonPropertyName("parts")]
            public CompletedPartData[] Parts { get; set; }
        }
        public class CompletedPartData
        {
            [JsonPropertyName("ETag")]
            public string ETag { get; set; }

            [JsonPropertyName("PartNumber")]
            public int PartNumber { get; set; }
        }

        public class UploadInitiateData
        {
            public class UserMediaData
            {
                [JsonPropertyName("uuid")]
                public string UUID { get; set; }

                [JsonPropertyName("filename")]
                public string Filename { get; set; }

                [JsonPropertyName("size")]
                public long Size { get; set; }

                [JsonPropertyName("datetime_created")]
                public DateTime TimeCreated { get; set; }

                [JsonPropertyName("expiry")]
                public DateTime? ExpireTime { get; set; }

                [JsonPropertyName("status")]
                public string Status { get; set; }
            }
            public class UploadPartData
            {
                [JsonPropertyName("part_number")]
                public int PartNumber { get; set; }

                [JsonPropertyName("url")]
                public string Url { get; set; }

                [JsonPropertyName("offset")]
                public long Offset { get; set; }

                [JsonPropertyName("length")]
                public int Length { get; set; }
            }

            [JsonPropertyName("user_media")]
            public UserMediaData Metadata { get; set; }

            [JsonPropertyName("upload_urls")]
            public UploadPartData[] UploadUrls { get; set; }
        }

        public class PackageUploadMetadata
        {
            [JsonPropertyName("author_name")]
            public string AuthorName { get; set; }

            [JsonPropertyName("categories")]
            public string[] Categories { get; set; }

            [JsonPropertyName("communities")]
            public string[] Communities { get; set; }

            [JsonPropertyName("has_nsfw_content")]
            public bool HasNsfwContent { get; set; }

            [JsonPropertyName("upload_uuid")]
            public string UploadUUID { get; set; }
        }

        // JSON response structure for publish package request.
        public class PublishData
        {
            public class AvailableCommunityData
            {
                public class CommunityData
                {
                    [JsonPropertyName("identifier")]
                    public string Identifier { get; set; }

                    [JsonPropertyName("name")]
                    public string Name { get; set; }

                    # nullable enable
                    [JsonPropertyName("discord_url")]
                    public string? DiscordUrl { get; set; }

                    [JsonPropertyName("wiki_url")]
                    public object? WikiUrl { get; set; }
                    # nullable disable

                    [JsonPropertyName("require_package_listing_approval")]
                    public bool RequirePackageListingApproval { get; set; }
                }

                [JsonPropertyName("community")]
                public CommunityData Community { get; set; }

                [JsonPropertyName("categories")]
                public List<string> Categories { get; set; }

                [JsonPropertyName("url")]
                public string Url { get; set; }
            }

            public class PackageVersionData
            {
                [JsonPropertyName("namespace")]
                public string Namespace { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("version_number")]
                public string VersionNumber { get; set; }

                [JsonPropertyName("full_name")]
                public string FullName { get; set; }

                [JsonPropertyName("description")]
                public string Description { get; set; }

                [JsonPropertyName("icon")]
                public string Icon { get; set; }

                [JsonPropertyName("dependencies")]
                public List<string> Dependencies { get; set; }

                [JsonPropertyName("download_url")]
                public string DownloadUrl { get; set; }

                [JsonPropertyName("downloads")]
                public int Downloads { get; set; }

                [JsonPropertyName("date_created")]
                public DateTime DateCreated { get; set; }

                [JsonPropertyName("website_url")]
                public string WebsiteUrl { get; set; }

                [JsonPropertyName("is_active")]
                public bool IsActive { get; set; }
            }

            [JsonPropertyName("available_communities")]
            public List<AvailableCommunityData> AvailableCommunities { get; set; }

            [JsonPropertyName("package_version")]
            public PackageVersionData PackageVersion { get; set; }

        }
    }

    [Serializable]
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
}
