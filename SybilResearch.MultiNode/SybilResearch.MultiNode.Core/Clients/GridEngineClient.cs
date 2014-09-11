using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using SybilResearch.MultiNode;

namespace SybilResearch.MultiNode.Core.Clients
{
    public class GridEngineClient: ClientBase<IComputeEngine>, IComputeEngine
    {
        public EngineStatus Status
        {
            get;
            set;
        }

        public Task Task
        {
            get { throw new NotImplementedException(); }
        }

        public ITaskBroker TaskBroker
        {
            get
            {
                throw new NotImplementedException("Taskbroker cannot be retrieved for the grid engine client");
            }
            set
            {
                throw new NotImplementedException("Taskbroker cannot be set for the grid engine client");
            }
        }

        public string EngineId
        {
            get { return base.Channel.EngineId; }
            set { base.Channel.EngineId = value; }
        }

        public string EngineHost
        {
            get { return base.Channel.EngineHost; }
            set { base.Channel.EngineHost = value; }
        }

        public string ResourcePath
        {
            get { return base.Channel.ResourcePath; }
            set { base.Channel.ResourcePath = value; }
        }

        public HostObjectPair CreateObject(Task task)
        {
            return base.Channel.CreateObject(task);
        }

        public void ExecuteTask(Task task)
        {
            base.Channel.ExecuteTask(task);
        }
    }
}
