using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace Engine
{
    public static class AzureUtils
    {
        private const int MaxEventHumMessageSize = (1024 - 1) * 1024; // Reserve 1k for metadata.

        public static async Task<CloudTable> GetOrCreateTableAsync(AzureUtils.StorageAccountInfo storageAcountInfo, string tableName)
        {
            var storageCredentials = new StorageCredentials(storageAcountInfo.StorageAccountName, storageAcountInfo.StorageAccountKey);
		    var cloudStorageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            CloudTableClient cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            CloudTable cloudTable = cloudTableClient.GetTableReference(tableName);
            if (await cloudTable.CreateIfNotExistsAsync())
            {
                Logger.TraceLine($"Cloud table '{cloudTable.Name}' created.");
            }
            else
            {
                Logger.TraceLine($"Cloud table '{cloudTable.Name}' already exists.");
            }

            return cloudTable;
        }

        public static async Task<bool> DeleteTableRowIfExistsAsync(CloudTable table, string partitionKey, string rowKey)
        {
            TableOperation deleteOperation = TableOperation.Delete(new TableEntity(partitionKey, rowKey) { ETag = "*" });

            try
            {
                await table.ExecuteAsync(deleteOperation);
                return true;
            }
            catch (Exception e)
            {
                StorageException storageException = Retry.UnwrapAggregateException<StorageException>(e);
                if (storageException == null || storageException.RequestInformation.HttpStatusCode != 404)
                {
                    throw;
                }

                return false;
            }
        }


        public static async Task<string> AadLoginAsApplicationAsync(string resource, AzureUtils.ApplicationClientInfo applicationClientInfo)
        {
            var authenticationContext = 
                new AuthenticationContext($"https://login.windows.net/{applicationClientInfo.DirectoryId}", TokenCache.DefaultShared);

            AuthenticationResult token = await authenticationContext.AcquireTokenAsync(
                resource,
                new ClientCredential(applicationClientInfo.ClientId, applicationClientInfo.ClientSecret));

            return token.AccessToken;
        }

        public static List<byte[]> SerializeToJsonForEventHub(object[] items, long maxSizeInBytes = MaxEventHumMessageSize)
        {
            long originalLength;
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                JsonUtils.WriteJson(streamWriter, items);
                streamWriter.Flush();
                memoryStream.Flush();
                originalLength = memoryStream.Length;
                if (originalLength <= maxSizeInBytes)
                {
                    return new List<byte[]> { memoryStream.ToArray() };
                }
            }

            Debug.Assert(originalLength > maxSizeInBytes, "originalLength > maxSizeInBytes");

            if (items.Length <= 1)
            {
                throw new ValueTooLargeForEventHubMessage();
            }
            else
            {
                int partitionCount = (int)Math.Ceiling((double)originalLength / (double)maxSizeInBytes);
                int partitionSize = items.Length / partitionCount;
                Debug.Assert(partitionSize > 0, "partitionSize > 0");

                var partitions = new List<byte[]>(partitionCount);
                for (int offset = 0; offset < items.Length; offset += partitionSize)
                {
                    partitions.AddRange(SerializeToJsonForEventHub(
                        items.Skip(offset).Take(partitionSize).ToArray(),
                        maxSizeInBytes));
                }

                return partitions;
            }
        }

        public sealed class StorageAccountInfo
        {
            public string StorageAccountName { get; set; }
            public string StorageAccountKey { get; set; }
        }

        public sealed class ApplicationClientInfo
        {
            public string DirectoryId { get; set; }
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        /// <summary>
        /// Max message size for EventHub is 1MB (https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-quotas)
        /// </summary>
        public sealed class ValueTooLargeForEventHubMessage : Exception
        {
        }
    }
}