using System;
using System.Linq;
using System.Text;
using NServiceBus.Msmq.RavenDB;
using NServiceBus.Settings;
using NServiceBus.Transports.Msmq;
using Raven.Client;

namespace NServiceBus
{
    /// <summary>
    /// RavenDB=based subscription store.
    /// </summary>
    public class RavenDBSubscriptionStore : SubscriptionStoreDefinition
    {
        protected override SubscriptionStoreInfrastructure Initialize(SettingsHolder settings)
        {
            IDocumentStore documentStore;
            if (!settings.TryGet(RavenDBSubscriptionStoreSettingsExtension.SettingsKey, out documentStore))
            {
                throw new Exception("Please specify the document store to use for RavenDB MSMQ subscription store.");
            }
            Helpers.SafelyCreateIndex(documentStore, new MsmqSubscriptionsIndex());

            return new SubscriptionStoreInfrastructure(
                () => new SubscriptionReader(documentStore),
                () => new SubscriptionManager(settings.EndpointName().ToString(), settings.LocalAddress(), documentStore));
        }
    }
}
