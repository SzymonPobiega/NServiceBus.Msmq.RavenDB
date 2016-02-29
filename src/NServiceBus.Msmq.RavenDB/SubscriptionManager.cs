using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Transports;
using Raven.Client;

namespace NServiceBus.Msmq.RavenDB
{
    class SubscriptionManager : IManageSubscriptions
    {
        IDocumentStore documentStore;
        string localEndpoint;
        string receiveAddress;

        public SubscriptionManager(string localEndpoint, string receiveAddress, IDocumentStore documentStore)
        {
            this.localEndpoint = localEndpoint;
            this.receiveAddress = receiveAddress;
            this.documentStore = documentStore;
        }

        public async Task Subscribe(Type eventType, ContextBag context)
        {
            var doc = new MsmqSubscription()
            {
                TypeName = eventType.FullName,
                Endpoint = localEndpoint,
                TransportAddress = receiveAddress
            };
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = false; //We don't care who creates that document first.
                await session.StoreAsync(doc, MsmqSubscription.MakeDocumentId(eventType.FullName, localEndpoint, receiveAddress)).ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        

        public async Task Unsubscribe(Type eventType, ContextBag context)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Delete(MsmqSubscription.MakeDocumentId(eventType.FullName, localEndpoint, receiveAddress));
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}