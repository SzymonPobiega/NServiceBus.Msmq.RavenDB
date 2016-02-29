using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Transports;
using EndpointConfiguration = NServiceBus.EndpointConfiguration;
using Raven.Client.Document;

public class ConfigureMsmqTransport : IConfigureTestExecution
{
    EndpointConfiguration endpointConfiguration;

    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new []
    {
        typeof(AllTransportsWithMessageDrivenPubSub)
    };

    public Task Configure(EndpointConfiguration configuration, IDictionary<string, string> settings)
    {
        documentStore = GetDocumentStore();
        endpointConfiguration = configuration;
        configuration.UseTransport<MsmqTransport>()
            .UseSubscriptionStore<RavenDBSubscriptionStore>().UseDocumentStore(documentStore);
        return Task.FromResult(0);
    }

    public static DocumentStore GetDocumentStore()
    {
        var databaseName = "NServiceBus.Msmq.RavenDB";

        var documentStore = new DocumentStore
        {
            Url = "http://localhost:8083",
            DefaultDatabase = databaseName,
            ResourceManagerId = Guid.NewGuid() /* This is OK for ATT purposes */
        };

        documentStore.Initialize();

        return documentStore;
    }

    public Task Cleanup()
    {
        var bindings = endpointConfiguration.GetSettings().Get<QueueBindings>();
        var allQueues = MessageQueue.GetPrivateQueuesByMachine("localhost");
        var queuesToBeDeleted = new List<string>();

        foreach (var messageQueue in allQueues)
        {
            using (messageQueue)
            {
                if (bindings.ReceivingAddresses.Any(ra =>
                {
                    var indexOfAt = ra.IndexOf("@", StringComparison.Ordinal);
                    if (indexOfAt >= 0)
                    {
                        ra = ra.Substring(0, indexOfAt);
                    }
                    return messageQueue.QueueName.StartsWith(@"private$\" + ra, StringComparison.OrdinalIgnoreCase);
                }))
                {
                    queuesToBeDeleted.Add(messageQueue.Path);
                }
            }
        }

        foreach (var queuePath in queuesToBeDeleted)
        {
            try
            {
                MessageQueue.Delete(queuePath);
                Console.WriteLine("Deleted '{0}' queue", queuePath);
            }
            catch (Exception)
            {
                Console.WriteLine("Could not delete queue '{0}'", queuePath);                
            }
        }

        MessageQueue.ClearConnectionCache();

        return DeleteDatabase(documentStore);
    }

    public static async Task DeleteDatabase(DocumentStore documentStore)
    {
        // Periodically the delete will throw an exception because Raven has the database locked
        // To solve this we have a retry loop with a delay
        var triesLeft = 3;

        while (--triesLeft > 0)
        {
            try
            {
                await documentStore.AsyncDatabaseCommands.GlobalAdmin.DeleteDatabaseAsync(documentStore.DefaultDatabase, true);
                break;
            }
            catch
            {
                if (triesLeft < 1)
                {
                    throw;
                }

                await Task.Delay(250);
            }
        }

        Console.WriteLine("Deleted '{0}' database", documentStore.DefaultDatabase);
    }

    DocumentStore documentStore;
}
