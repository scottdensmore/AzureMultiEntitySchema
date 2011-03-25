namespace AExpense.Data.Process
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class CommandHandler
    {
        private readonly TimeSpan interval;

        protected CommandHandler() : this(TimeSpan.FromMilliseconds(200))
        {
        }

        protected CommandHandler(TimeSpan interval)
        {
            this.interval = interval;
        }

        public static CommandHandler Every(TimeSpan intervalBetweenRuns)
        {
            return new CommandHandler(intervalBetweenRuns);
        }

        public virtual void Do(ICommand command)
        {
            Task.Factory.StartNew(
                () =>
                    {
                        while (true)
                        {
                            this.Cycle(command);
                        }
                    },
                TaskCreationOptions.LongRunning);
        }

        protected void Cycle(ICommand command)
        {
            try
            {
                command.Run();

                Thread.Sleep(this.interval);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}