using System;
using Raven.Client;
using Raven.Client.Indexes;

namespace NServiceBus
{
    class Helpers
    {
        /// <summary>
        /// Safely add the index to the RavenDB database, protect against possible failures caused by documented
        /// and undocumented possibilities of failure.
        /// Will throw iff index registration failed and index doesn't exist or it exists but with a non-current definition.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="index"></param>
        internal static void SafelyCreateIndex(IDocumentStore store, AbstractIndexCreationTask index)
        {
            try
            {
                index.Execute(store);
            }
            catch (Exception) // Apparently ArgumentException can be thrown as well as a WebException; not taking any chances
            {
                var existingIndex = store.DatabaseCommands.GetIndex(index.IndexName);
                if (existingIndex == null || !index.CreateIndexDefinition().Equals(existingIndex))
                    throw;
            }
        }
    }
}