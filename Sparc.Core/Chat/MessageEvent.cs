using System;
using System.Collections.Generic;
using System.Text;

namespace Sparc.Core.Chat
{
    public class MessageEvent : Event
    {
        public string MsgType { get; set; }
        public string Body { get; set; }
    }
}
