using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassData
{

    [Serializable]
    public class StampNotFoundException : Exception
    {
        public StampNotFoundException() : this("Could not locate stamp on assembly") { }
        public StampNotFoundException(string message) : base(message) { }
        public StampNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected StampNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
