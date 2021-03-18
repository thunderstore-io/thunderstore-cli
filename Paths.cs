using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThunderstoreCLI
{
    public static class Paths
    {
        public static string GetProjectConfigPath(Config.Config config)
        {
            return Path.GetFullPath("./thunderstore.toml");
        }
    }
}
