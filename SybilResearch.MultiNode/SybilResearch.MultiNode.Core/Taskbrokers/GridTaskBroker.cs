using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using log4net;
using SybilResearch.MultiNode;
using SybilResearch.MultiNode.Core.Clients;

namespace SybilResearch.MultiNode.Core.TaskBrokers
{
    [ServiceBehavior(ConfigurationName = "GridTaskBroker"
                    , InstanceContextMode = InstanceContextMode.Single
                    , ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class GridTaskBroker : ITaskBroker
    {
        #region Constants
        private readonly string CLIENT_BASE_ADDRESS = ConfigurationManager.AppSettings["client.base.address"];
        #endregion

        #region Data Members
        /// <summary>
        /// logger
        /// </summary>
        private static ILog _log = LogManager.GetLogger(typeof(GridTaskBroker));
        /// <summary>
        /// engines registered with this taskbroker
        /// </summary>
        private Dictionary<string, IComputeEngine> _engineCollection = new Dictionary<string, IComputeEngine>();
        /// <summary>
        /// mutex to control changes to the engine collection
        /// </summary>
        private object _enginesMutex = new object();
        /// <summary>
        /// Queue for pending "ExecuteTask" requests
        /// </summary>
        private Queue<Task> _executeQueue = new Queue<Task>();
        /// <summary>
        /// Thread to process the ExeceuteTask request queue
        /// </summary>
        private Thread _executeTaskThread;
        /// <summary>
        /// Flag to indicate whether to keep the task broker running
        /// </summary>
        private bool _runTaskBroker = true;
        /// <summary>
        /// Map to track which tasks have been assigned what request handles
        /// </summary>
        private Dictionary<string, RequestHandle> _taskRequestHandleMap = new Dictionary<string, RequestHandle>();
        /// <summary>
        /// Map to track which requests have been assigned to which engines
        /// </summary>
        private Dictionary<string, RequestHandle> _taskEngineMap = new Dictionary<string, RequestHandle>();
        /// <summary>
        /// Map to track which callback channel belongs to which request handle
        /// </summary>
        private Dictionary<string, ITaskBrokerCallback> _handleCallbackMap = new Dictionary<string, ITaskBrokerCallback>();
        /// <summary>
        /// Event to control execute task loop - signalled when an engine is free
        /// </summary>
        private AutoResetEvent _waitTillEngineIdle = new AutoResetEvent(false);
        /// <summary>
        /// Event to control execute task loop - signallled when a task is added to the execute queue
        /// </summary>
        private AutoResetEvent _waitTillExecuteQueueHasTasks = new AutoResetEvent(false);
        #endregion

        #region Properties
        /// <summary>
        /// Returns the total number of engines registered with this broker
        /// </summary>
        public int TotalEngines
        {
            get
            {
                return _engineCollection.Count;
            }
        }

        /// <summary>
        /// Returns the number of available (Idle) engines registered with this broker
        /// </summary>
        public int AvailableEngines
        {
            get
            {
                int availableEngines = 0;
                foreach (string engineId in _engineCollection.Keys)
                {
                    if (_engineCollection[engineId].Status == EngineStatus.Idle) availableEngines++;
                }
                return availableEngines;
            }
        }

        /// <summary>
        /// Returns a list of engine id's associated with engines registered with this broker
        /// </summary>
        public List<string> RegisteredEngines
        {
            get { return _engineCollection.Keys.ToList<string>(); }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create an object of the type specified in the task
        /// </summary>
        /// <param name="task">
        /// The task containing info regarding the type to be created.
        /// The task will also retain info about the object/host on which it was created.
        /// </param>
        public HostObjectPair CreateObject(Task task)
        {
            while (_runTaskBroker)
            {
                foreach (IComputeEngine engine in _engineCollection.Values)
                {
                    if (engine.Status == EngineStatus.Idle)
                    {
                        return engine.CreateObject(task);
                    }
                }

                // if we went through all engines and found no idle ones,
                // then wait till one becomes idle
                _waitTillEngineIdle.WaitOne();
            }
            return null;
        }

        /// <summary>
        /// Execute the method specified in the task.
        /// If the task-object contains an object-host pair data, then the method
        /// will be executed on that object - otherwise, a brand new non-persistent
        /// object of the type specified in the task will be created and the method
        /// will be executed in that object.
        /// </summary>
        /// <returns>task request handle</returns>
        public RequestHandle ExecuteTask(Task task)
        {
            _log.InfoFormat("Received execute request for task: {0}", task.TaskId);

            // create a new request handle
            RequestHandle handle = new RequestHandle();
            handle.Handle = String.Format("TB-{0}-{1}", Environment.MachineName, Guid.NewGuid().ToString());
            _taskRequestHandleMap[task.TaskId] = handle;
            _handleCallbackMap[handle.Handle] = OperationContext.Current.GetCallbackChannel<ITaskBrokerCallback>();
            handle.TaskStatus = TaskStatus.Queued;

            // enqueue the task
            _executeQueue.Enqueue(task);
            _waitTillExecuteQueueHasTasks.Set();
            _log.DebugFormat("Task {0} enqueued with handle: {1}", task.TaskId, handle.Handle);
            return handle;
        }

        /// <summary>
        /// Get task status for a given task
        /// </summary>
        /// <param name="handle">request handle for the task</param>
        /// <returns>task status</returns>
        public TaskStatus GetTaskStatus(string taskId)
        {
            try
            {
                _log.DebugFormat("Received status-request for task: {0}", taskId);
                return _taskRequestHandleMap[taskId].TaskStatus;
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error retrieving status info for task {0}: {1}\n{2}", taskId, e.Message, e.StackTrace);
                return TaskStatus.Faulted;
            }
        }

        /// <summary>
        /// Get engine status
        /// </summary>
        /// <param name="engineId">engine id</param>
        /// <returns>engine status</returns>
        public EngineStatus GetEngineStatus(string engineId)
        {
            _log.DebugFormat("Received status-request for engine: {0}", engineId);
            try
            {
                return _engineCollection[engineId].Status;
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error retrieving status info for engine {0}: {1}\n{2}", engineId, e.Message, e.StackTrace);
                return EngineStatus.Faulted;
            }
        }

        /// <summary>
        /// Register an engine with this task-broker
        /// </summary>
        /// <param name="engine">engine</param>
        public void RegisterEngine(IComputeEngine engine)
        {
            throw new NotImplementedException("Not implemented for grid task-broker. Use RegisterEngineId(string engineId) instead.");
        }

        /// <summary>
        /// Register an engine with this task-broker
        /// </summary>
        /// <param name="engineId">engine id</param>
        public void RegisterEngineId(string engineId)
        {
            _log.DebugFormat("Received request to register engine {0}", engineId);
            try
            {
                // first create a grid-engine client
                IComputeEngine engine = new GridEngineClient();
                (engine as GridEngineClient).Endpoint.Address = new EndpointAddress(String.Format("{0}/{1}", CLIENT_BASE_ADDRESS, engineId));

                // *** critical section ***
                lock (_enginesMutex)
                {
                    if (!_engineCollection.ContainsKey(engineId))
                    {
                        _engineCollection[engineId] = engine;
                    }
                }
                _log.DebugFormat("Registered engine {0}", engineId);
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error registering engine {0}: {1}\n{2}", engineId, e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Unregister an engine from this task-broker
        /// </summary>
        /// <param name="engineId">Id of the engine to unregister</param>
        public void UnregisterEngine(string engineId)
        {
            _log.DebugFormat("Received request to unregister engine {0}", engineId);
            try
            {
                // *** critical section ***
                lock (_enginesMutex)
                {
                    _engineCollection.Remove(engineId);
                }
                _log.DebugFormat("Unregistered engine {0}", engineId);
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error unregistering engine {0}: {1}\n{2}", engineId, e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Update engine status on the taskbroker
        /// </summary>
        /// <param name="engineId">engine to update the status for</param>
        /// <param name="status">status</param>
        public void UpdateEngineStatus(string engineId, EngineStatus status)
        {
            _log.DebugFormat("Received request to update engine status for engine {0} to {1}", engineId, status.ToString());
            try
            {
                _engineCollection[engineId].Status = status;

                // if an engine wants to register its state as Idle, signal the execute task loop to continue. 
                // It also means the engine has finished processing a task - so let's update the corresponding task's status
                // and notify the client
                if (status == EngineStatus.Idle)
                {
                    _waitTillEngineIdle.Set();
                    RequestHandle request = _taskEngineMap[engineId];
                    request.TaskStatus = TaskStatus.Finished;

                    // notify client
                    _handleCallbackMap[request.Handle].TaskComplete(request);
                    _taskEngineMap.Remove(engineId);
                }
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error updating engine status for engine {0}: {1}\n{2}", engineId, e.Message, e.StackTrace);
            }
        }

        /// <summary>
        /// Starts the CreateObject/ExecuteTask queue-processing threads
        /// </summary>
        public void Start()
        {
            _log.DebugFormat("Starting task broker...");
            _runTaskBroker = true;
            _executeTaskThread = new Thread(new ThreadStart(ExecuteTaskThread));

            // start processing create/execute requests in separate threads            
            _executeTaskThread.Start();
            _log.DebugFormat("Task broker now ready.");
        }

        /// <summary>
        /// Stops the CreateObject/ExecuteTask queue-processing threads
        /// </summary>
        public void Stop()
        {
            _log.DebugFormat("Stopping task broker...");

            // break the loop and signal all blockers
            _runTaskBroker = false;
            _waitTillExecuteQueueHasTasks.Set();
            _waitTillEngineIdle.Set();

            // stop threads to process create/execute requests
            _executeTaskThread.Join();
            _log.DebugFormat("Task broker stopped.");
        }

        /// <summary>
        /// Method that processes ExecuteTask requests in a thread
        /// </summary>
        private void ExecuteTaskThread()
        {
            bool foundIdleEngine;
            while (_runTaskBroker)
            {
                foundIdleEngine = false;

                // if execute is empty, wait till it gets some tasks
                if (_executeQueue.Count <= 0) _waitTillExecuteQueueHasTasks.WaitOne();

                // get the task
                Task t = _executeQueue.Peek();

                // check if this task relies on an object hosted on a particular engine
                if (t.ObjectData != null)
                {
                    // so find that host, then execute this task
                    foundIdleEngine = true; // TODO: check if this should be set
                    var targetEngine = _engineCollection[t.ObjectData.HostId];
                    ExecuteTaskOnEngine(_executeQueue.Dequeue(), targetEngine);
                }
                else
                {
                    // just find the first idle engine and dispatch the task
                    foreach (var engine in _engineCollection.Values)
                    {
                        if (engine.Status == EngineStatus.Idle)
                        {
                            foundIdleEngine = true;
                            ExecuteTaskOnEngine(_executeQueue.Dequeue(), engine);
                            break;
                        }
                    }
                }

                // if we couldn't find an idle engine and the queue has tasks pending,
                // wait till an engine gets free
                if (!foundIdleEngine && _executeQueue.Count > 0) _waitTillEngineIdle.WaitOne();
            }
        }

        /// <summary>
        /// Execute a task on an engine
        /// </summary>
        /// <param name="task">task</param>
        /// <param name="engine">engine</param>
        private void ExecuteTaskOnEngine(Task task, IComputeEngine engine)
        {
            // update request handle
            RequestHandle handle = _taskRequestHandleMap[task.TaskId];
            handle.EngineId = engine.EngineId;
            handle.TaskStatus = TaskStatus.Executing;
            handle.SubmitTime = DateTime.Now;

            var engineId = (from e in _engineCollection where e.Value == engine select e.Key).FirstOrDefault();
            _taskEngineMap.Add(engineId, handle);

            // execute task
            engine.ExecuteTask(task);
        }
        #endregion
    }
}