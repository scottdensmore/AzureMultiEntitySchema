namespace AExpense.Data.Process
{
    using System;
    using System.Threading.Tasks;
    using AExpense.Data.Storage;

    public static class QueueCommandHandler
    {
        public static QueueCommandHandler<T> For<T>(IAzureQueue<T> queue) where T : AzureQueueMessage
        {
            return QueueCommandHandler<T>.For(queue);
        }
    }

    public class QueueCommandHandler<T> : GenericQueueHandler<T> where T : AzureQueueMessage
    {
        private readonly IAzureQueue<T> queue;
        private TimeSpan interval;

        protected QueueCommandHandler(IAzureQueue<T> queue)
        {
            this.queue = queue;
            interval = TimeSpan.FromMilliseconds(200);
        }

        public static QueueCommandHandler<T> For(IAzureQueue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return new QueueCommandHandler<T>(queue);
        }

        public virtual void Do(IQueueCommand<T> queueCommand)
        {
            Task.Factory.StartNew(
                () =>
                    {
                        while (true)
                        {
                            Cycle(queueCommand);
                        }
                    },
                TaskCreationOptions.LongRunning);
        }

        public QueueCommandHandler<T> Every(TimeSpan intervalBetweenRuns)
        {
            interval = intervalBetweenRuns;

            return this;
        }

        protected void Cycle(IQueueCommand<T> queueCommand)
        {
            try
            {
                ProcessMessages(queue, queue.GetMessages(1), queueCommand.Run);

                Sleep(interval);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}