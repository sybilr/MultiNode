using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SybilResearch.MultiNode
{
    [ServiceContract(Namespace="http://www.sybilresearch.com/SybilResearch/Core/ITaskBroker/"
                    , SessionMode = SessionMode.Required
                    , Name="TaskBroker"
                    , CallbackContract=typeof(ITaskBrokerCallback))]
    public interface ITaskBroker
    {
        #region Properties
        /// <summary>
        /// Total engines controlled by the broker
        /// </summary>        
        int TotalEngines { get; }

        /// <summary>
        /// Engines currently available to receive tasks
        /// </summary>        
        int AvailableEngines { get; }

        /// <summary>
        /// Get a list of the engines registered with this task-broker
        /// </summary>
        List<string> RegisteredEngines { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Create an object of the type specified in the task
        /// </summary>
        /// <param name="task">
        /// The task containing info regarding the type to be created.
        /// The task will also retain info about the object/host on which it was created.
        /// </param>
        [OperationContract]
        HostObjectPair CreateObject(Task task);

        /// <summary>
        /// Execute the method specified in the task.
        /// If the task-object contains an object-host pair data, then the method
        /// will be executed on that object - otherwise, a brand new non-persistent
        /// object of the type specified in the task will be created and the method
        /// will be executed in that object.
        /// </summary>
        /// <returns>task request handle</returns>
        [OperationContract]
        RequestHandle ExecuteTask(Task task);
        
        /// <summary>
        /// Get task status for a given task
        /// </summary>
        /// <param name="task">request handle for the task</param>
        /// <returns>task status</returns>
        [OperationContract]
        TaskStatus GetTaskStatus(string taskId);

        /// <summary>
        /// Get engine status
        /// </summary>
        /// <param name="engineId">engine id</param>
        /// <returns>engine status</returns>
        [OperationContract]
        EngineStatus GetEngineStatus(string engineId);

        /// <summary>
        /// Register an engine with this task-broker
        /// </summary>
        /// <param name="engine">engine</param>
        [OperationContract]
        void RegisterEngine(IComputeEngine engine);

        /// <summary>
        /// Register an engine with this task-broker
        /// </summary>
        /// <param name="engineId">engine id</param>
        [OperationContract]
        void RegisterEngineId(string engineId);

        /// <summary>
        /// Unregister an engine from this task-broker
        /// </summary>
        /// <param name="engineId">Id of the engine to unregister</param>
        [OperationContract]
        void UnregisterEngine(string engineId);

        /// <summary>
        /// Update engine status on the taskbroker
        /// </summary>
        /// <param name="engineId">engine to update the status for</param>
        /// <param name="status">status</param>
        [OperationContract]
        void UpdateEngineStatus(string engineId, EngineStatus status);

        /// <summary>
        /// Starts the taskbroker so that it begins accepting 
        /// ExecuteTask() and CreateObject() requests
        /// </summary>
        [OperationContract]
        void Start();

        /// <summary>
        /// Stops the taskbroker so that it stops accepting 
        /// ExecuteTask() and CreateObject() requests
        /// </summary>
        [OperationContract]
        void Stop();
        #endregion
    }

    /// <summary>
    /// Method to be run by the taskbroker client 
    /// after a task complete notification has come from the TaskBroker
    /// </summary>
    /// <param name="handle">request handle</param>
    public delegate void RunOnTaskComplete(RequestHandle handle);

    /// <summary>
    /// Callback contract for ITaskBroker services to communicate with their clients
    /// </summary>
    public interface ITaskBrokerCallback
    {
        [OperationContract(IsOneWay=true)]
        void TaskComplete(RequestHandle handle);
    }
}
