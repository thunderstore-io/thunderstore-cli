using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tomlyn.Syntax;

namespace ThunderstoreCLI.Config
{
    class ProjectFileConfig : IConfigProvider
    {
        public void Parse() { }

        public PackageMeta GetPackageMeta()
        {
            return null;
        }

        public BuildConfig GetBuildConfig()
        {
            return null;
        }

        public PublishConfig GetPublishConfig()
        {
            return null;
        }

        public AuthConfig GetAuthConfig()
        {
            return null;
        }

        public static string GetFilepath()
        {
            return Path.GetFullPath("./thunderstore.toml");
        }

        public static void Read()
        {
            var configPath = GetFilepath();
            if (!File.Exists(configPath))
            {
                throw new CommandException($"Unable to find project configuration file. Looked from {configPath}");
            }
        }

        public static void WriteDefault()
        {
            var path = GetFilepath();
            if (File.Exists(path))
            {
                Console.WriteLine($"Project configuration already exists at {path}, skipping creation");
            }
            else
            {
                var doc = new DocumentSyntax()
                {
                    Tables =
                    {
                        new TableSyntax("config")
                        {
                            Items =
                            {
                                { "schemaVersion", "0.0.1" }
                            }
                        },
                        new TableSyntax("package")
                        {
                            Items = {
                                {"author", "AuthorName"},
                                {"name", "PackageName"},
                                {"versionNumber", "1.0.0"},
                                {"description", ""},
                                {"websiteUrl", ""},
                            }
                        },
                        new TableSyntax(new KeySyntax("package", "dependencies"))
                        {
                            Items =
                            {
                                {"Example-Dependency", "1.0.0"},
                            },
                        },
                        new TableSyntax("build")
                        {
                            Items =
                            {
                                { "icon", "./icon.png" },
                                { "readme", "./README.md" },
                                { "outdir", "./build" },
                            }
                        },
                        new TableSyntax(new KeySyntax("build", "copy"))
                        {
                            Items =
                            {
                                { "./dist", "./" }
                            }
                        },
                        new TableSyntax(new KeySyntax("publish"))
                        {
                            Items =
                            {
                                { "repository", "thunderstore.io" }
                            }
                        }
                    }
                };
                File.WriteAllText(path, doc.ToString());
                Console.WriteLine($"Wrote project configuration to {path}");
            }
        }
    }
}
