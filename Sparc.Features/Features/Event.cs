using System;

namespace Sparc.Features
{
    public class Event
    {
        public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
    }
}
