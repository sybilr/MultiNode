using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace SybilResearch.MultiNode
{
    /// <summary>
    /// Engines will be able to interact with their respective TaskBrokers using EngineMessages
    /// </summary>
    [DataContract(Namespace="http://www.sybilresearch.com/SybilResearch/Core/2012/08/")]
    public class EngineMessage
    {
        [DataMember]
        public TaskStatus Status { get; set; }
        [DataMember]
        public string RequestHandle { get; set; }
    }
}
