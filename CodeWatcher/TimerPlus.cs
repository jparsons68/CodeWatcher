using System;

namespace CodeWatcher
{
    public class TimerPlus : System.Timers.Timer
    {

        public TimerPlus()
        {
            this.Elapsed += this.ElapsedAction;
        }

        protected new void Dispose()
        {
            this.Elapsed -= this.ElapsedAction;
            base.Dispose();
        }

        public double MsRemaining =>
            DueTime != null ? ((DateTime) this.DueTime - DateTime.Now).TotalMilliseconds : double.NaN;
        public DateTime? DueTime { get; private set; }

        public new void Start() 
        { 
            this.DueTime = DateTime.Now.AddMilliseconds(this.Interval);
            base.Start();
        }
        public new void Stop()
        {
            this.DueTime = null;
            base.Stop();
        }
        private void ElapsedAction(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (this.AutoReset)
                this.DueTime = DateTime.Now.AddMilliseconds(this.Interval);
            else
                this.DueTime = null;
        }
    }
}