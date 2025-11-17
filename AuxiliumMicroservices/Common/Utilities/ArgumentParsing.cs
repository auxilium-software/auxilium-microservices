using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumMicroservices.Common.Utilities
{
    internal class ArgumentParsing
    {
        internal static void SortConfigFileLocation(string[] args)
        {
            string configPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    configPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrEmpty(configPath))
            {
                ConsoleWriting.CatastrophicFail("No configuration file provided. Use --config <path>\n");
                Environment.Exit(1);
            }

            ConfigurationUtilities.ConfigurationFileLocation = configPath;
            ConsoleWriting.Debug($"Using config file: {configPath}\n");
            ConsoleWriting.Debug($"Using config file: {configPath}\n\n");
        }
    }
}
