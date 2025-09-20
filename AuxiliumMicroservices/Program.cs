using AuxiliumMicroservices.Common.ServiceInteractions;
using AuxiliumMicroservices.Common.Utilities;
using System;
using System.Threading.Tasks;

namespace AuxiliumMicroservices
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(@"
                     _ _ _                       _______        _          _____                             
     /\             (_) (_)                     |__   __|      | |        |  __ \                            
    /  \  _   ___  ___| |_ _   _ _ __ ___          | | __ _ ___| | __     | |__) |   _ _ __  _ __   ___ _ __ 
   / /\ \| | | \ \/ / | | | | | | '_ ` _ \         | |/ _` / __| |/ /     |  _  / | | | '_ \| '_ \ / _ \ '__|
  / ____ \ |_| |>  <| | | | |_| | | | | | |        | | (_| \__ \   <      | | \ \ |_| | | | | | | |  __/ |   
 /_/    \_\__,_/_/\_\_|_|_|\__,_|_| |_| |_|        |_|\__,_|___/_|\_\     |_|  \_\__,_|_| |_|_| |_|\___|_|   
");
            ArgumentParsing.SortConfigFileLocation(args);
            await Preflight.Go();
        }
    }
}
