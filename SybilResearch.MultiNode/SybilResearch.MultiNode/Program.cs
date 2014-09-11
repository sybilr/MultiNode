using System.Collections.Generic;
using System.Threading;
using SybilResearch.Core.Test;

namespace SybilResearch.MultiNode
{
    class Program
    {
        /// <summary>Object that signals the host that it is okay to shut down now.</summary>
        public static AutoResetEvent HostRunning = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            GridTaskBrokerTests test = new GridTaskBrokerTests();
            test.FixtureSetUp();

            // Start the host on a separate thread
            Thread t = new Thread(RunTheHost) { Name = "HostThread" };
            t.Start();

            test.Client_ExecuteTask_Test();

            HostRunning.Set();
            test.FixtureTearDown();
        }

        public static void RunTheHost()
        {
            GridTaskBrokerTests t2 = new GridTaskBrokerTests();
            t2.Host_Init_Test();
            HostRunning.WaitOne();
        }
    }
}
