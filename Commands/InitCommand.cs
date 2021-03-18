using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderstoreCLI.Config;

namespace ThunderstoreCLI.Commands
{
    public static class InitCommand
    {
        public static int Run(InitOptions options, Config.Config config)
        {
            var path = Paths.GetProjectConfigPath(config);
            Console.WriteLine($"Initializing new project configuration to {path}");
            if (File.Exists(path) && !options.Overwrite)
            {
                Console.WriteLine($"Project configuration already exists, not overwriting");
                Console.WriteLine($"Use the --{InitOptions.OVERWRITE_FLAG} to overwrite the file");
                return 1;
            }
            else
            {
                if (File.Exists(path))
                {
                    Console.WriteLine($"Project configuration already exists, overwriting");
                }
                ProjectFileConfig.Write(config, path);
                Console.WriteLine("Done!");
                return 0;
            }
        }
    }
}
