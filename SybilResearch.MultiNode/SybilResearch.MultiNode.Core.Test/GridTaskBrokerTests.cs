using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using log4net;
using log4net.Config;
using NUnit.Framework;
using SybilResearch.MultiNode;
using SybilResearch.MultiNode.Core.TaskBrokers;
using SybilResearch.MultiNode.Core.Clients;
using SybilResearch.MultiNode.Core.Engines;

namespace SybilResearch.Core.Test
{
    [TestFixture]
    public class GridTaskBrokerTests
    {
        #region Data members
        private const int NUM_ENGINES = 10;
        private static readonly ILog _log = LogManager.GetLogger(typeof(GridTaskBrokerTests));
        private ITaskBroker _taskBrokerClient = new GridTaskBrokerClient(new GridTaskBrokerCallback());
        private ITaskBroker _taskBroker = new GridTaskBroker();
        private List<IComputeEngine> _engines = new List<IComputeEngine>();
        private List<ServiceHost> _engineHosts = new List<ServiceHost>();
        #endregion

        #region Tests
        [Test]
        public void Host_Init_Test()
        {
            SetupGrid();
            ServiceHost host = new ServiceHost(typeof(GridTaskBroker));
            try
            {
                host.Open();
                _log.DebugFormat("Service hosted at {0}", host.Description.Endpoints[0].Address.Uri.ToString());
                Thread.Sleep(TimeSpan.FromDays(1));
                host.Close();
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Exception: {0}\n{1}", e.Message, e.StackTrace);
                host.Abort();
            }
        }

        [Test]
        public void Client_ExecuteTask_Test()
        {
            ITaskBroker client = new GridTaskBrokerClient(new GridTaskBrokerCallback());
            client.Start();
            Task task = new Task();

            foreach (IComputeEngine engine in _engines)
            {
                engine.ResourcePath = System.IO.Path.Combine(Environment.CurrentDirectory, "..\\..\\..\\SybilResearch.MultiNode.SampleCustomerCode\\bin\\Debug");
                client.RegisterEngineId(engine.EngineId);
            }

            for (int i = 0; i < 1; i++)
            {
                task.TaskId = Guid.NewGuid().ToString();

                task.Assembly = "SybilResearch.MultiNode.SampleCustomerCode";
                task.TypeName = "SybilResearch.MultiNode.SampleCustomerCode.SentenceRepeater";
                task.MethodName = "RepeatSentencesWithParams";

                RequestHandle handle = client.ExecuteTask(task);
                _log.DebugFormat("Handle submit time: {0} - {1}", handle.TaskStatus.ToString(), handle.SubmitTime);
            }

            Thread.Sleep(TimeSpan.FromHours(1));

            (client as GridTaskBrokerClient).Close();
        }

        [Test]
        public void Client_CreateObject_Test()
        {
            ITaskBroker client = new GridTaskBrokerClient(new GridTaskBrokerCallback());
            Task task = new Task();


            (client as GridTaskBrokerClient).Close();
        }

        [Test]
        public void Client_GetEngineStatus_Test()
        {
            ITaskBroker client = new GridTaskBrokerClient(new GridTaskBrokerCallback());

            string engineId = Guid.NewGuid().ToString();
            EngineStatus status = client.GetEngineStatus(engineId);
            _log.DebugFormat("Status: {0}", status.ToString());

            (client as GridTaskBrokerClient).Close();
        }

        [Test]
        public void Client_Register_EngineId()
        {
            ITaskBroker client = new GridTaskBrokerClient(new GridTaskBrokerCallback());
            foreach (IComputeEngine engine in _engines)
            {
                client.RegisterEngineId(engine.EngineId);
            }
        }

        [Test]
        public void Client_Unregister_Engine()
        {
            ITaskBroker client = new GridTaskBrokerClient(new GridTaskBrokerCallback());
            foreach (IComputeEngine engine in _engines)
            {
                client.RegisterEngineId(engine.EngineId);
            }
            foreach (IComputeEngine engine in _engines)
            {
                client.UnregisterEngine(engine.EngineId);
            }
        }

        [Test]
        public void Client_GetTaskStatus()
        {

        }

        [Test]
        public void Client_Update_Engine_Status()
        {

        }
        #endregion

        #region Setup and helper methods
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // configure logger
            XmlConfigurator.Configure();

            // set up grid-engines locally
            //SetupGrid();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            ShutdownGrid();
        }

        /// <summary>
        /// Set up the grid-engines locally
        /// </summary>
        private void SetupGrid()
        {
            _log.InfoFormat("Setting up grid...");
            for (int i = 0; i < NUM_ENGINES; i++)
            {
                IComputeEngine engine = new GridEngine();
                engine.TaskBroker = _taskBrokerClient;
                _engines.Add(engine);

                ServiceHost host = new ServiceHost(engine);
                host.Description.Endpoints[0].Address = new EndpointAddress(String.Format(@"{0}/{1}"
                                                                            , host.BaseAddresses[0].AbsoluteUri
                                                                            , engine.EngineId));
                _engineHosts.Add(host);
                try
                {
                    host.Open();
                    _log.InfoFormat("Started grid engine with id: {0}", engine.EngineId);
                }
                catch (Exception e)
                {
                    _log.ErrorFormat("Exception: {0}\n{1}", e.Message, e.StackTrace);
                    host.Abort();
                    _engineHosts.Remove(host);
                }
            }
            _log.InfoFormat("Grid setup complete.");
        }

        /// <summary>
        /// Shut down the local grid engines
        /// </summary>
        private void ShutdownGrid()
        {
            _log.InfoFormat("**** Shutting down grid... ****");

            foreach (ServiceHost host in _engineHosts)
            {
                try
                {
                    host.Close();
                }
                catch (Exception e)
                {
                    _log.ErrorFormat("Exception: {0}\n{1}", e.Message, e.StackTrace);
                    host.Abort();
                    _engineHosts.Remove(host);
                }
            }


            _log.InfoFormat("**** Grid shut down. ****");
        }
        #endregion
    }
}