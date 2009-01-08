using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SvnQuery
{
    class PendingReads: EventWaitHandle
    {
        int pending;

        public PendingReads(): base(false, EventResetMode.ManualReset)
        {}

        public void Increment()
        {
            Interlocked.Increment(ref pending);
        }

        public void Decrement()
        {
            if (Interlocked.Decrement(ref pending) == 0)
                Set();            
        }

        public void Wait()
        {
            WaitOne();
        }

        public bool HasFinished
        {
            get
            {
                return WaitOne(0, false);
            }
        }

    }
}
