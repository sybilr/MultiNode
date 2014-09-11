using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using SybilResearch.MultiNode;

namespace SybilResearch.MultiNode.Core.Clients
{
    /// <summary>
    /// This will act as an interface to the actual GridTaskBroker
    /// which can reside on a different machine than the end-user client.
    /// The end-user client will then use the GridTaskBrokerClient object to submit tasks
    /// to the the GridTaskBroker
    /// </summary>
    public class GridTaskBrokerClient: DuplexClientBase<ITaskBroker>, ITaskBroker
    {
        public GridTaskBrokerClient(object callbackInstance): base(callbackInstance)
        {

        }

        public HostObjectPair CreateObject(Task task)
        {
            return base.Channel.CreateObject(task);
        }

        public RequestHandle ExecuteTask(Task task)
        {
            return base.Channel.ExecuteTask(task);
        }

        public TaskStatus GetTaskStatus(string taskId)
        {
            return base.Channel.GetTaskStatus(taskId);
        }

        public EngineStatus GetEngineStatus(string engineId)
        {
            return base.Channel.GetEngineStatus(engineId);
        }

        public void RegisterEngine(IComputeEngine engine)
        {
            base.Channel.RegisterEngine(engine);
        }

        public int TotalEngines
        {
            get
            {
                throw new NotImplementedException("Total engines cannot be returned by the client");
            }
        }

        public int AvailableEngines
        {
            get
            {
                throw new NotImplementedException("Available engines cannot be returned by the client");
            }
        }

        public List<string> RegisteredEngines
        {
            get { throw new NotImplementedException("Registered engines cannot be returned by the client"); }
        }


        public void UnregisterEngine(string engineId)
        {
            base.Channel.UnregisterEngine(engineId);
        }


        public void UpdateEngineStatus(string engineId, EngineStatus status)
        {
            base.Channel.UpdateEngineStatus(engineId, status);
        }

        public void RegisterEngineId(string engineId)
        {
            base.Channel.RegisterEngineId(engineId);
        }


        public void Start()
        {
            base.Channel.Start();
        }

        public void Stop()
        {
            base.Channel.Stop();
        }
    }

    /// <summary>
    /// Class to handle callbacks from the GridTaskbroker
    /// </summary>
    public class GridTaskBrokerCallback : ITaskBrokerCallback
    {
        /// <summary>
        /// Method to be run by the taskbroker client 
        /// after a task complete notification has come from the TaskBroker.
        /// Set this to be run to be notified of the completion of a particular task.
        /// </summary>
        public RunOnTaskComplete MethodToRun { get; set; }

        public void TaskComplete(RequestHandle handle)
        {
            if (MethodToRun != null)
            {
                MethodToRun(handle);
            }
        }
    }
}
