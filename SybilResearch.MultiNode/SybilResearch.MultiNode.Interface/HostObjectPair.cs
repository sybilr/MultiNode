using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SybilResearch.MultiNode
{
    /// <summary>
    /// Class containing an object's id and the 
    /// id of the Host(engine) on which the object exists
    /// </summary>
    [DataContract(Namespace = "http://www.sybilresearch.com/SybilResearch/Core/2012/08/")]
    public class HostObjectPair
    {
        [DataMember]
        private string _objectId = null;
        [DataMember]
        private string _hostId = null;

        /// <summary>
        /// Object Id
        /// </summary>        
        public string ObjectId { get { return _objectId; } set { _objectId = value; } }
        /// <summary>
        /// Host Id
        /// </summary>        
        public string HostId { get { return _hostId; } set { _hostId = value; } }
    }
}
