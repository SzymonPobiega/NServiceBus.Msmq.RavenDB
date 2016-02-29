using System;
using System.Security.Cryptography;
using System.Text;

namespace NServiceBus.Msmq.RavenDB
{
    class MsmqSubscription
    {
        public string Id { get; set; }
        public string Endpoint { get; set; }
        public string TransportAddress { get; set; }
        public string TypeName { get; set; }

        public static string MakeDocumentId(string eventType, string endpoint, string address)
        {
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(endpoint + "/" + address + "/" + eventType);
                var hashBytes = provider.ComputeHash(inputBytes);
                var id = new Guid(hashBytes);
                return $"MsmqSubscriptions/{id}";
            }
        }
    }
}