using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ThunderstoreCLI.Config
{
    class UserFileConfig : EmptyConfig
    {
        private AuthConfig authConfig;

        public override void Parse(Config currentConfig)
        {
            //var confdir = Path.Combine(
            //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            //    "tcli"
            //);
            //var confpath = Path.Combine(
            //    confdir, "auth.conf"
            //);

            //Directory.CreateDirectory(confdir);

            //string configText;
            //if (!File.Exists(confpath))
            //{
            //    configText = GetDefaultToml();
            //    File.WriteAllText(confpath, configText);
            //}
            //else
            //{
            //    configText = File.ReadAllText(confpath);
            //}

            //var configDoc = Toml.Parse(configText).ToModel();
            //authConfig = new AuthConfig()
            //{
            //    DefaultToken = (string)((TomlTable)configDoc["auth"])["defaultToken2222"],
            //    AuthorTokens = new()
            //};
        }

        //private string GetDefaultToml()
        //{
        //    var doc = new DocumentSyntax()
        //    {
        //        Tables =
        //        {
        //            new TableSyntax(new KeySyntax("config"))
        //            {
        //                Items =
        //                {
        //                    { "schemaVersion", "0.0.1" }
        //                }
        //            },
        //            new TableSyntax(new KeySyntax("auth"))
        //            {
        //                Items =
        //                {
        //                    { "defaultToken", "" }
        //                }
        //            },
        //            new TableSyntax(new KeySyntax("auth", "authorTokens"))
        //            {
        //                Items =
        //                {
        //                    { "ExampleAuthor", "" }
        //                }
        //            },
        //        }
        //    };
        //    return doc.ToString();
        //}
    }
}
