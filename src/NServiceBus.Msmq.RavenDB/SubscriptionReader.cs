using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Transports.Msmq;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Client.Linq;

namespace NServiceBus.Msmq.RavenDB
{
    class SubscriptionReader : IQuerySubscriptions
    {
        IDocumentStore documentStore;

        public SubscriptionReader(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public Task<IEnumerable<Subscriber>> GetSubscribersFor(IEnumerable<Type> eventTypes)
        {
            var typeNames = eventTypes.Select(t => t.FullName);
            using (var session = documentStore.OpenSession())
            {
                var subscriptions = session.Query<MsmqSubscription, MsmqSubscriptionsIndex>()
                    .Where(s => s.TypeName.In(typeNames))
                    .Customize(c => c.WaitForNonStaleResultsAsOfNow())
                    .ToList()
                    .Select(s => new Subscriber(s.Endpoint, s.TransportAddress));
                return Task.FromResult(subscriptions);
            }
        }
    }

    class MsmqSubscriptionsIndex : AbstractIndexCreationTask<MsmqSubscription>
    {
        public MsmqSubscriptionsIndex()
        {
            Map = docs => from doc in docs
                          select new
                          {
                              doc.TypeName,
                          };

            DisableInMemoryIndexing = true;
        }
    }
}