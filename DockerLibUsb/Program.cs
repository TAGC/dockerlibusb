using System;
using System.Threading.Tasks;
using DockerLibUsb.Communication;
using DockerLibUsb.Monitoring;
using LibUsbDotNet;
using Serilog;

namespace DockerLibUsb
{
    internal static class Program
    {
        public static async Task Main()
        {
            Console.WriteLine("While running this program, try unplugging & replugging the test USB device.");
            Console.WriteLine("Monitor the console logs to check whether communication is being re-established");

            ConfigureRootLogger();

            var (vid, pid) = IdentifyTargetDevice();
            Console.WriteLine($"Using USB device with VID={vid} and PID={pid} as test subject");

            using (var communicationService = BuildRestartableCommunicationService(vid, pid))
            {
                await communicationService.Start();

                Console.WriteLine("Press any key to end the test...");
                Console.Read();
            }

            UsbDevice.Exit();
        }

        private static ICommunicationService BuildDelegate(int vid, int pid) =>
            new TestUsbCommunicationService(vid, pid, Log.Logger);

        private static ICommunicationService BuildRestartableCommunicationService(int vid, int pid)
        {
            var fileSystem = new WatchableFileSystem();
            var devMonitor = new DevMonitor(fileSystem, Log.Logger);

            return new RestartableUsbCommunicationService(vid, pid, BuildDelegate, devMonitor, Log.Logger);
        }

        private static void ConfigureRootLogger()
        {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
        }

        private static (ushort vid, ushort pid) IdentifyTargetDevice()
        {
            if (!ushort.TryParse(Environment.GetEnvironmentVariable("TEST_VID"), out var vid))
            {
                Console.Write("Vendor ID not specified in environment. Please enter it (0-65535): ");

                while (!ushort.TryParse(Console.ReadLine(), out vid))
                {
                }
            }

            if (!ushort.TryParse(Environment.GetEnvironmentVariable("TEST_PID"), out var pid))
            {
                Console.Write("Product ID not specified in environment. Please enter it (0-65535): ");

                while (!ushort.TryParse(Console.ReadLine(), out pid))
                {
                }
            }

            return (vid, pid);
        }
    }
}