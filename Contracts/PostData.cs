using System;
using System.Runtime.Serialization;

namespace Contracts
{
    [DataContract]
    public class PostData
    {
        [DataMember(Order = 0)]
        public long Id { get; set; }

        [DataMember(Order = 1)]
        public string UserId { get; set; }

        [DataMember(Order = 2)]
        public DateTime Timestamp { get; set; }

        [DataMember(Order = 3)]
        public string Content { get; set; }
    }
}
