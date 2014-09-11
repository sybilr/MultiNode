using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace SybilResearch.MultiNode
{
    /// <summary>
    /// Engine status
    /// </summary>
    [DataContract(Namespace="http://www.sybilresearch.com/SybilResearch/Core/EngineStatus/")]
    public enum EngineStatus
    {
        [EnumMember]
        Idle,
        [EnumMember]
        Busy,
        [EnumMember]
        Faulted
    }

    [ServiceContract(Namespace="http://www.sybilresearch.com/SybilResearch/Core/IComputeEngine/"
                    , SessionMode=SessionMode.Required)]
    public interface IComputeEngine
    {
        #region Properties
        /// <summary>
        /// Gets/sets engine status
        /// </summary>        
        EngineStatus Status
        {
            [OperationContract]
            get;            
            set;
        }

        /// <summary>
        /// Task currently attached to this engine
        /// </summary>
        Task Task { get; }

        /// <summary>
        /// Task broker instance to which this engine is attached
        /// </summary>
        ITaskBroker TaskBroker { get; set; }

        /// <summary>
        /// Engine Id
        /// </summary>
        string EngineId
        {
            [OperationContract]
            get;
            [OperationContract]
            set;
        }

        /// <summary>
        /// Machine Uri hosting the engine
        /// </summary>
        string EngineHost
        {
            [OperationContract]
            get;
            [OperationContract]
            set;
        }

        /// <summary>
        /// Gets/sets the base resource path for the client code assemblies
        /// </summary>        
        ///<remarks>
        /// It is assumed that all the assemblies lie in a directory identified by the "ResourcePath" property.
        /// The engine looks for a particular resource in a sub-directory of the "ResourcePath" directory, e.g. 
        /// "C:\Engines\MyResourcePath\My.Client.Assembly-1.0.0.1"
        /// By default, the client code would be searched for in a file having its path in the following format:
        /// ${ResourcePath}\(AssemblyName)-(HighestVersionNumber)\(AssemblyName).dll
        /// The config that would be loaded would have the following name: (AssemblyName).dll.config
        /// </remarks>
        string ResourcePath
        {
            [OperationContract]
            get;
            [OperationContract]
            set;
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
        /// <returns>a HostObjectPair object containing object/host id</returns>
        [OperationContract]
        HostObjectPair CreateObject(Task task);

        /// <summary>
        /// Execute the method specified in the task.
        /// If the task-object contains an object-host pair data, then the method
        /// will be executed on that object - otherwise, a brand new non-persistent
        /// object of the type specified in the task will be created and the method
        /// will be executed in that object. This method will be called by the TaskBroker
        ///  - so preferably, this should be a non-blocking method in any implementation.
        /// </summary>
        /// <param name="task">task</param>
        [OperationContract]
        void ExecuteTask(Task task);
        #endregion
    }
}
