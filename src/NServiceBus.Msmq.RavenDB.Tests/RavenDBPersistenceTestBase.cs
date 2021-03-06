﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Raven.Client;
using Raven.Tests.Helpers;

namespace NServiceBus.Msmq.RavenDB.Tests
{
    public class RavenDBPersistenceTestBase : RavenTestBase
    {
        [SetUp]
        public virtual void SetUp()
        {
            store = DocumentStoreFactory(this);
            Helpers.SafelyCreateIndex(store, new MsmqSubscriptionsIndex());
        }

        [TearDown]
        public virtual void TearDown()
        {
            sessions.ForEach(s => s.Dispose());
            sessions.Clear();
            store.Dispose();
        }

        protected internal IAsyncDocumentSession OpenAsyncSession()
        {
            var documentSession = store.OpenAsyncSession();
            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;
            sessions.Add(documentSession);
            return documentSession;
        }

        protected static Task<Exception> Catch(Func<Task> action)
        {
            return Catch<Exception>(action);
        }

        /// <summary>
        ///     This helper is necessary because RavenTestBase doesn't like Assert.Throws, Assert.That... with async void methods.
        /// </summary>
        protected static async Task<TException> Catch<TException>(Func<Task> action) where TException : Exception
        {
            try
            {
                await action();
                return default(TException);
            }
            catch (TException ex)
            {
                return ex;
            }
        }

        List<IAsyncDocumentSession> sessions = new List<IAsyncDocumentSession>();
        protected IDocumentStore store;
        protected Func<RavenTestBase, IDocumentStore> DocumentStoreFactory { get; set; } = t => t.NewDocumentStore(configureStore: s =>
        {
            s.Configuration.DefaultStorageTypeName = "esent";
        });
    }
}