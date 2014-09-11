using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SybilResearch.MultiNode
{
    /// <summary>
    /// Task status
    /// </summary>
    [DataContract(Namespace = "http://www.sybilresearch.com/SybilResearch/Core/2012/08/")]
    public enum TaskStatus
    {
        /// <summary>The task is queued for execution</summary>
        [EnumMember]
        Queued,
        /// <summary>The task is currently executing</summary>
        [EnumMember]
        Executing,
        /// <summary>The task is currently paused</summary>
        [EnumMember]
        Paused,
        /// <summary>The task has finished execution and is awaiting result collection</summary>
        [EnumMember]
        Finished,
        /// <summary>The task has finished and the results have been collected</summary>
        [EnumMember]
        Retrieved,
        /// <summary>The task has encountered a fault or status could not be retrieved</summary>
        [EnumMember]
        Faulted
    }


    /// <summary>
    /// This class stores information about a request that has been submitted to
    /// a task broker object. Use this to retrieve information about task status
    /// and to collect results after the task is finished.
    /// </summary>
    [DataContract(Namespace = "http://www.sybilresearch.com/SybilResearch/Core/2012/08/")]
    public class RequestHandle
    {
        #region Data members
        [DataMember]
        private string _handle = Guid.NewGuid().ToString();
        [DataMember]
        private DateTime _submitTime = DateTime.MaxValue;
        [DataMember]
        private DateTime _pauseTime = DateTime.MaxValue;
        [DataMember]
        private DateTime _finishTime = DateTime.MaxValue;
        [DataMember]
        private DateTime _collectTime = DateTime.MaxValue;
        [DataMember]
        private TaskStatus _taskStatus = TaskStatus.Queued;
        [DataMember]
        private string _engineId;
        #endregion

        #region Properties
        /// <summary>
        /// Gets/sets the request handle - this is also the id of the cache into which the data
        /// for the request will be stored
        /// </summary>
        public string Handle
        {
            get { return _handle; }
            set { _handle = value; }
        }

        /// <summary>
        /// Gets/sets the submission time for the request
        /// </summary>
        public DateTime SubmitTime
        {
            get { return _submitTime; }
            set { _submitTime = value; }
        }

        /// <summary>
        /// Gets/sets the time when the request was last paused
        /// </summary>
        public DateTime PauseTime
        {
            get { return _pauseTime; }
            set { _pauseTime = value; }
        }

        /// <summary>
        /// Gets/sets the finish time for the request
        /// </summary>
        public DateTime FinishTime
        {
            get { return _finishTime; }
            set { _finishTime = value; }
        }

        /// <summary>
        /// Gets/sets the collection time for the results of this request
        /// </summary>
        public DateTime CollectTime
        {
            get { return _collectTime; }
            set { _collectTime = value; }
        }

        /// <summary>
        /// Task status
        /// </summary>
        public TaskStatus TaskStatus
        {
            get { return _taskStatus; }
            set { _taskStatus = value; }
        }

        /// <summary>
        /// Gets/sets the id of the engine the task is executing on
        /// </summary>
        public string EngineId
        {
            get { return _engineId; }
            set { _engineId = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Collects the results
        /// </summary>
        public object CollectResults()
        {
            return null;
        }

        #endregion
    }
}
