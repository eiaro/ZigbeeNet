using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ZigbeeNet;
using ZigbeeNet.CC;

namespace BasicSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            var controller = new ZigbeeController("COM5");
            controller.Open();

            try
            {
                Run(controller).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadLine();
        }

        private static async Task Run(ZigbeeController controller)
        {
            var rst = new SYS_RESET(1);
            await controller.Channel.SendAsync(rst, Predicate, CancellationToken.None);
            Console.ReadLine();
        }

        private static bool Predicate(SerialPacket arg)
        {
            if (arg == null) return false;
            Console.WriteLine($"Received package: {arg.Payload}");
            return true;
        }
    }
}
