using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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


            var requestContent = new MultipartFormDataContent();

            using var fileStream = File.Open(filepath, FileMode.Open);
            requestContent.Add(new StreamContent(fileStream), "file", "file");

            using var metaStream = new MemoryStream(Encoding.UTF8.GetBytes(SerializeUploadMeta(config)));
            requestContent.Add(new StreamContent(metaStream), "metadata", "metadata");

            var request = new HttpRequestMessage(HttpMethod.Post, config.GetPackageUploadUrl())
            {
                Content = requestContent,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.AuthConfig.DefaultToken);

            var response = client.Send(request);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {

                return 0;
            }
            else
            {
                Console.WriteLine(Red($"ERROR: Unexpected response from the server"));
                using var responseReader = new StreamReader(response.Content.ReadAsStream());
                Console.WriteLine(Red($"Details:"));
                Console.WriteLine($"Status code: {response.StatusCode:D} {response.StatusCode}");
                Console.WriteLine(Dim(responseReader.ReadToEnd()));
                return 1;
            }
        }

        public static string SerializeUploadMeta(Config.Config config)
        {
            var meta = new PackageUploadMetadata()
            {
                AuthorName = config.PackageMeta.Namespace,
                Categories = Array.Empty<string>(), // TODO: Add
                Communities = Array.Empty<string>(), // TODO: Add
                HasNsfwContent = config.PackageMeta.ContainsNsfwContent == true
            };
            return JsonSerializer.Serialize(meta);
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
        }
    }
}
