using System;
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

            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.PublishConfig.Repository}/api/experimental/usermedia/initiate-upload/")
            {
                Content = new StringContent(SerializeFileData(filepath), Encoding.UTF8, "application/json")
            };

            var uploadResponse = client.Send(uploadRequest);
            using var uploadReader = new StreamReader(uploadResponse.Content.ReadAsStream());
            if (uploadResponse.StatusCode != HttpStatusCode.Created)
            {
                Console.WriteLine(Red("ERROR: Failed to start usermedia upload"));
                Console.WriteLine(Red("Details:"));
                Console.WriteLine($"Status code: {uploadResponse.StatusCode:D} {uploadResponse.StatusCode}");
                Console.WriteLine(Dim(uploadReader.ReadToEnd()));
                return 1;
            }

            var uploadData = JsonSerializer.Deserialize<UploadInitiateData>(uploadReader.ReadToEnd());

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
                var abortRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.PublishConfig.Repository}/api/experimental/usermedia/{uploadData.Metadata.UUID}/abort-upload/");
                abortRequest.Headers.Authorization = config.GetAuthHeader();
                client.Send(abortRequest);
                return 1;
            }

            var uploadedParts = uploadTasks.Select(x => x.Result.data).ToArray();

            var finishRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.PublishConfig.Repository}/api/experimental/usermedia/{uploadData.Metadata.UUID}/finish-upload/");
            finishRequest.Content = new StringContent(JsonSerializer.Serialize(new CompletedUpload()
            {
                Parts = uploadedParts
            }), Encoding.UTF8, "application/json");
            client.Send(finishRequest);

            var publishPackageRequest = new HttpRequestMessage(HttpMethod.Post, $"{config.PublishConfig.Repository}/api/experimental/submission/submit/");
            publishPackageRequest.Content = new StringContent(SerializeUploadMeta(config, uploadData.Metadata.UUID), Encoding.UTF8, "application/json");
            var publishResponse = client.Send(publishPackageRequest);
            
            if (publishResponse.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine(Blue($"Successfully published {config.PackageMeta.Namespace}-{config.PackageMeta.Name}"));
                return 0;
            }
            else
            {
                Console.WriteLine(Red($"ERROR: Unexpected response from the server"));
                using var responseReader = new StreamReader(publishResponse.Content.ReadAsStream());
                Console.WriteLine(Red($"Details:"));
                Console.WriteLine($"Status code: {publishResponse.StatusCode:D} {publishResponse.StatusCode}");
                Console.WriteLine(Dim(responseReader.ReadToEnd()));
                return 1;
            }
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
    }
}
