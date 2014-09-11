using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using log4net;
using SybilResearch.MultiNode;

namespace SybilResearch.MultiNode.Core.TaskBrokers
{
    /// <summary>
    /// Allows tasks to be submitted to engines resident in the same process
    /// as the task broker
    /// </summary>
    public class LocalTaskBroker: ITaskBroker
    {
        #region Data Members
        /// <summary>
        /// logger
        /// </summary>
        private static readonly ILog _log = LogManager.GetLogger(typeof(LocalTaskBroker));
        /// <summary>
        /// Engine collection
        /// </summary>
        private Dictionary<string, IComputeEngine> _engines = new Dictionary<string, IComputeEngine>();
        /// <summary>
        /// Queue for pending "CreateObject" task requests
        /// </summary>
        private Queue<Task> _createQueue = new Queue<Task>();
        /// <summary>
        /// Queue for pending "ExecuteTask" requests
        /// </summary>
        private Queue<Task> _executeQueue = new Queue<Task>();
        /// <summary>
        /// Thread to process the CreateObject request queue
        /// </summary>
        private Thread _createObjectThread;
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
        #endregion

        #region Properties
        public List<string> RegisteredEngines { get { return _engines.Keys.ToList<string>(); } }

        public int TotalEngines { get { return _engines.Count; } set { throw new NotImplementedException(); } }

        public int AvailableEngines { get; set; }
        #endregion

        #region Methods
        public RequestHandle ExecuteTask(Task task)
        {
            // create a new request handle
            RequestHandle handle = new RequestHandle();
            handle.Handle = String.Format("TB-{0}-{1}", Environment.MachineName,  Guid.NewGuid().ToString());
            _taskRequestHandleMap[task.TaskId] = handle;            
            handle.TaskStatus = TaskStatus.Queued;

            // enqueue the task
            _executeQueue.Enqueue(task);

            return handle;
        }

        public HostObjectPair CreateObject(Task task)
        {
            while (_runTaskBroker)
            {
                // iterate through all engines
                foreach (IComputeEngine engine in _engines.Values)
                {
                    if (engine.Status == EngineStatus.Idle)
                    {
                        // found an idle engine - let it create the object
                        // specified in the task
                        return engine.CreateObject(task);
                    }
                }
                Thread.Sleep(1);
            }
            return null;
        }

        public TaskStatus GetTaskStatus(string taskId)
        {
            throw new NotImplementedException();
        }


        public EngineStatus GetEngineStatus(string engineId)
        {
            return _engines[engineId].Status;
        }

        public void RegisterEngine(IComputeEngine engine)
        {
            _engines[engine.EngineId] = engine;
        }

        public void UnregisterEngine(string engineId)
        {
            _engines[engineId] = null;
        }

        /// <summary>
        /// Starts the CreateObject/ExecuteTask queue-processing threads
        /// </summary>
        public void Start()
        {   
            _runTaskBroker = true;
            _createObjectThread = new Thread(new ThreadStart(ExecuteTaskThread));
            _executeTaskThread = new Thread(new ThreadStart(CreateObjectThread));

            // start processing create/execute requests in separate threads
            _createObjectThread.Start();
            _executeTaskThread.Start();
        }

        /// <summary>
        /// Stops the CreateObject/ExecuteTask queue-processing threads
        /// </summary>
        public void Stop()
        {
            _runTaskBroker = false;

            // stop threads to process create/execute requests
            _createObjectThread.Join();
            _executeTaskThread.Join();
        }

        /// <summary>
        /// Method that processes ExecuteTask requests in a thread
        /// </summary>
        private void ExecuteTaskThread()
        {
            while (_runTaskBroker)
            {
                // iterate through all engines
                foreach (IComputeEngine engine in _engines.Values)
                {
                    if (engine.Status == EngineStatus.Idle && _executeQueue.Count > 0)
                    {
                        // found an idle engine - let's see if we can execute this task or not
                        Task task = _executeQueue.Peek();
                        if (task.ObjectData == null)
                        {
                            ExecuteTaskOnEngine(_executeQueue.Dequeue(), engine);
                        }
                        else
                        {
                            // this task relies on an object hosted on a particular engine
                            // so if the current engine is that host, then execute this task
                            if (task.ObjectData.HostId == engine.EngineId.ToString())
                            {
                                ExecuteTaskOnEngine(_executeQueue.Dequeue(), engine);                                
                            }
                        }
                    }
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Method that processes CreateObject requests in a thread
        /// </summary>
        private void CreateObjectThread()
        {
            
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

            // execute task
            engine.ExecuteTask(task);
        }

        /// <summary>
        /// Update engine status
        /// </summary>
        /// <param name="engineId"></param>
        /// <param name="status"></param>
        public void UpdateEngineStatus(string engineId, EngineStatus status)
        {
            throw new NotImplementedException();
        }


        public void RegisterEngineId(string engineId)
        {
            throw new NotImplementedException("Not implemented for local task-broker. Use RegisterEngine(IComputeEngine) instead.");
        }
        #endregion
    }
}
