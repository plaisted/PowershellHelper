using System;
using System.Threading;

namespace Plaisted.PowershellHelper.FunctionalTests
{
    public class Timeout : IDisposable
    {
        private Timer timer;

        public Timeout(int milliSeconds)
        {
            timer = new Timer(this.Trigger, null, milliSeconds, milliSeconds);
        }

        private void Trigger(Object stateinfo)
        {
            throw new TimeoutException("Timeout reached before finishing.");
        }
        public void Dispose()
        {
            timer.Dispose();
        }
    }
}
