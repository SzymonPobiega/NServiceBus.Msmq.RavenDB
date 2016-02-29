using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Transports.Msmq;
using Raven.Client;

namespace NServiceBus
{
    /// <summary>
    /// Provides methods for configuring RavenDB MSMQ subscription store.
    /// </summary>
    public static class RavenDBSubscriptionStoreSettingsExtension
    {
        internal const string SettingsKey = "RavenDb.Subscriptions.DocumentStore";

        /// <summary>
        /// Configures the given document store to be used when storing subscriptions.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="documentStore">The document store to use</param>
        /// <returns></returns>
        public static SubscriptionStoreSettings<RavenDBSubscriptionStore> UseDocumentStore(this SubscriptionStoreSettings<RavenDBSubscriptionStore> cfg, IDocumentStore documentStore)
        {
            cfg.GetSettings().Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}