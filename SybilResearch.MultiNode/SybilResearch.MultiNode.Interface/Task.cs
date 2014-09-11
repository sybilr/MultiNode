using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SybilResearch.MultiNode
{
    [DataContract(Namespace = "http://www.sybilresearch.com/SybilResearch/Core/2012/08/")]
    public class Task
    {
        [DataMember]
        private string _assembly = null;
        [DataMember]
        private string _methodName = null;
        [DataMember]
        private string _typeName = null;
        [DataMember]
        private Dictionary<string, object> _args = new Dictionary<string, object>();
        [DataMember]
        private string _taskId = null;
        [DataMember]
        private HostObjectPair _objectData = null;


        #region Properties
        public string Assembly { get { return _assembly; } set { _assembly = value; } }

        public string MethodName { get { return _methodName; } set { _methodName = value; } }

        public string TypeName { get { return _typeName; } set { _typeName = value; } }

        public Dictionary<string, object> Args { get { return _args; } set { _args = value; } }

        public string TaskId { get { return _taskId; } set { _taskId = value; } }

        public HostObjectPair ObjectData { get { return _objectData; } set { _objectData = value; } }
        #endregion

        #region Methods
        public Task()
        {
            Args = new Dictionary<string, object>();
        }
        #endregion

    }
}
