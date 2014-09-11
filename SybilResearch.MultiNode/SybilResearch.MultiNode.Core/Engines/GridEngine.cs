using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using log4net;
using SybilResearch.MultiNode;

namespace SybilResearch.MultiNode.Core.Engines
{
    [ServiceBehavior(ConfigurationName="ComputeEngine"
                    , InstanceContextMode=InstanceContextMode.Single)]    
    public class GridEngine: IComputeEngine, IDisposable
    {
        #region Data Members
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly ILog _log = LogManager.GetLogger(typeof(GridEngine));
        /// <summary>
        /// Collection of objects persistent on this system
        /// </summary>
        private Dictionary<string, object> _objectCollection = new Dictionary<string, object>();
        /// <summary>
        /// Task attached to this engine
        /// </summary>
        private Task _task = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets/sets engine status
        /// </summary>
        public EngineStatus Status { get; set; }

        /// <summary>
        /// Task currently attached to this engine
        /// </summary>
        public Task Task
        {
            get { return _task; }
        }

        /// <summary>
        /// Task broker instance to which this engine is attached
        /// </summary>
        public ITaskBroker TaskBroker { get; set; }

        /// <summary>
        /// Engine Id
        /// </summary>
        public string EngineId { get; set; }

        /// <summary>
        /// Gets/sets the base resource path for the client code assemblies
        /// </summary>   
        public string ResourcePath { get; set; }

        /// <summary>
        /// Gets/sets the machine hosting the engine
        /// </summary>
        public string EngineHost { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new app-domain engine
        /// </summary>
        public GridEngine()
        {
            EngineHost = Environment.MachineName;
            EngineId = String.Format("Engine_{0}_{1}", EngineHost, Guid.NewGuid().ToString());
            _objectCollection = new Dictionary<string, object>();
        }


        /// <summary>
        /// Execute the method specified in the task.
        /// If the task-object contains an object-host pair data, then the method
        /// will be executed on that object - otherwise, a brand new non-persistent
        /// object of the type specified in the task will be created and the method
        /// will be executed in that object.
        /// </summary>
        /// <param name="task">task</param>
        public void ExecuteTask(Task task)
        {
            try
            {
                // mark engine as busy for the taskbroker
                TaskBroker.UpdateEngineStatus(this.EngineId, EngineStatus.Busy);
                
                _task = task;
                object objectToProcess = null;
                bool persistent = false;

                // Check if the task contains a reference to a specific object                
                if (task.ObjectData != null)
                {
                    if (_objectCollection.ContainsKey(task.ObjectData.ObjectId))
                    {
                        persistent = true;
                        objectToProcess = _objectCollection[task.ObjectData.ObjectId];
                    }
                }
                // otherwise we instantiate a (non-persistent) object of the specified type                
                else
                {
                    Assembly assembly = Assembly.LoadFrom(String.Format(@"{0}\{1}.dll", this.ResourcePath, task.Assembly));
                    objectToProcess = assembly.CreateInstance(task.TypeName);
                    _log.DebugFormat("Create non-persistent object: {0}", task.TypeName);
                }

                // execute the method on the relevant object
                objectToProcess.GetType().GetMethod(task.MethodName).Invoke(objectToProcess, new object[] { task.Args });

                // dispose the object if it's non-persistent
                if (!persistent && (objectToProcess as IDisposable) != null)
                {
                    (objectToProcess as IDisposable).Dispose();
                }
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error occured while executing task {0}:{1}\n{2}\nAssembly:{3}\nType:{4}\nMethod:{5}"
                                , task.TaskId, e.Message, e.StackTrace, task.Assembly, task.TypeName, task.MethodName);
            }
            finally
            {
                // mark engine as idle for the taskbroker
                TaskBroker.UpdateEngineStatus(this.EngineId, EngineStatus.Idle);
            }
        }

        /// <summary>
        /// Create persistent object on this engine
        /// </summary>
        /// <param name="task">task</param>
        /// <returns>host-object pair containing object data</returns>
        public HostObjectPair CreateObject(Task task)
        {
            HostObjectPair hoPair = null;
            _task = task;

            try
            {
                string objectId = String.Format("object_{0}_{1}", this.EngineId, Guid.NewGuid().ToString());
                Assembly assembly = Assembly.LoadFrom(String.Format(@"{0}\{1}.dll", this.ResourcePath, task.Assembly));
                _objectCollection[objectId] = assembly.CreateInstance(task.TypeName);

                hoPair = new HostObjectPair();
                hoPair.HostId = this.EngineId.ToString();
                hoPair.ObjectId = objectId;
                _log.DebugFormat("Create persistent object: {0} with id: {1}", task.TypeName, objectId);
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error occured while creating object for task {0}:{1}\n{2}\nAssembly:{3}\nType:{4}"
                                , task.TaskId, e.Message, e.StackTrace, task.Assembly, task.TypeName);
            }
            return hoPair;
        }

        /// <summary>
        /// Dispose all persistent objects and unmanaged resources associated with this engine
        /// </summary>
        public void Dispose()
        {
            foreach (string id in _objectCollection.Keys)
            {
                IDisposable disposableObject = _objectCollection[id] as IDisposable;
                if (disposableObject != null) disposableObject.Dispose();
            }
        }
        #endregion        
    }
}