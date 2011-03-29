namespace AExpense.Workers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using AExpense.Data;
    using AExpense.Data.Messages;
    using AExpense.Data.Process;
    using AExpense.Data.Storage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Diagnostics;
    using Microsoft.WindowsAzure.ServiceRuntime;

    public class WorkerRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            DiagnosticMonitor.Start(AzureConnectionStrings.Diagnostics);
            RoleEnvironment.Changing += RoleEnvironmentChanging;
            ApplicationStorageInitializer.Initialize();
            return base.OnStart();
        }

        public override void Run()
        {
            try
            {
                CloudStorageAccount account = CloudConfiguration.GetStorageAccount(AzureConnectionStrings.DataConnection);

                // Receipt Queue
                var receiptQueue = new AzureQueue<NewReceiptMessage>(account, AzureStorageNames.NewReceiptMessage);
                var receiptQueueCommand = new ReceiptThumbnailQueueCommand();
                QueueCommandHandler.For(receiptQueue).Every(TimeSpan.FromSeconds(5)).Do(receiptQueueCommand);

                // ExpenseExportQueueCommand
                var exportQueue = new AzureQueue<ApprovedExpenseMessage>(account, AzureStorageNames.ApprovedExpenseMessage);
                var exportQueueCommand = new ExpenseExportQueueCommand();
                QueueCommandHandler.For(exportQueue).Every(TimeSpan.FromSeconds(5)).Do(exportQueueCommand);


                // Expense Export
                CommandHandler.Every(TimeSpan.FromSeconds(60)).Do(new ExpenseExportCommand());

                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception e)
            {
                Log.Write(EventKind.Error, e.Message);
                throw;
            }
        }

        private static void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
            {
                e.Cancel = true;
            }
        }
    }
}