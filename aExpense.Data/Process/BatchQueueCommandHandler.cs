namespace AExpense.Data.Process
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AExpense.Data.Storage;

    public static class BatchQueueCommandHandler
    {
        public static BatchProcessingQueueHandler<T> For<T>(IAzureQueue<T> queue) where T : AzureQueueMessage
        {
            return BatchProcessingQueueHandler<T>.For(queue);
        }
    }

    public class BatchProcessingQueueHandler<T> : GenericQueueHandler<T> where T : AzureQueueMessage
    {
        private readonly IAzureQueue<T> queue;
        private TimeSpan interval;

        protected BatchProcessingQueueHandler(IAzureQueue<T> queue)
        {
            this.queue = queue;
            this.interval = TimeSpan.FromMilliseconds(200);
        }

        public static BatchProcessingQueueHandler<T> For(IAzureQueue<T> queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            return new BatchProcessingQueueHandler<T>(queue);
        }

        public BatchProcessingQueueHandler<T> Every(TimeSpan intervalBetweenRuns)
        {
            this.interval = intervalBetweenRuns;

            return this;
        }

        public virtual void Do(IBatchQueueCommand<T> batchQueueCommand)
        {
            Task.Factory.StartNew(
                () =>
                    {
                        while (true)
                        {
                            this.Cycle(batchQueueCommand);
                        }
                    },
                TaskCreationOptions.LongRunning);
        }

        protected void Cycle(IBatchQueueCommand<T> batchQueueCommand)
        {
            try
            {
                batchQueueCommand.PreRun();

                bool continueProcessing;
                do
                {
                    var messages = this.queue.GetMessages(32);
                    ProcessMessages(this.queue, messages, batchQueueCommand.Run);

                    continueProcessing = messages.Count() > 0;
                } 
                while (continueProcessing);
                
                batchQueueCommand.PostRun();

                this.Sleep(this.interval);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}