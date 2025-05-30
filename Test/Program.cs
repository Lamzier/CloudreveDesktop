using DokanNet;
using DokanNet.Logging;

namespace CloudreveDesktop.Test;

public class Program
{
    public static void Mai11n()
    {
        try
        {
            using var mre = new ManualResetEvent(false);
            using var dokanLogger = new ConsoleLogger("[Dokan] ");
            using var dokan = new Dokan(dokanLogger);
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                mre.Set();
            };

            var rfs = new Rfs();
            var dokanBuilder = new DokanInstanceBuilder(dokan)
                .ConfigureOptions(options =>
                {
                    options.Options = DokanOptions.DebugMode | DokanOptions.StderrOutput;
                    options.MountPoint = "r:\\";
                });
            using (var dokanInstance = dokanBuilder.Build(rfs))
            {
                mre.WaitOne();
            }

            Console.WriteLine(@"Success");
        }
        catch (DokanException ex)
        {
            Console.WriteLine(@"Error: " + ex.Message);
        }
    }
}