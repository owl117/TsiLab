using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;

namespace Engine
{
    public static class AzureUtils
    {
        public static async Task<CloudTable> GetOrCreateTableAsync(AzureUtils.StorageAccountInfo storageAcountInfo, string name)
        {
            var storageCredentials = new StorageCredentials(storageAcountInfo.StorageAccountName, storageAcountInfo.StorageAccountKey);
		    var cloudStorageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
            CloudTableClient cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            CloudTable cloudTable = cloudTableClient.GetTableReference(name);
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

        public static async Task<string> AadLoginAsApplicationAsync(string resource, AzureUtils.ApplicationClientInfo applicationClientInfo)
        {
            var authenticationContext = 
                new AuthenticationContext($"https://login.windows.net/{applicationClientInfo.DirectoryId}", TokenCache.DefaultShared);

            AuthenticationResult token = await authenticationContext.AcquireTokenAsync(
                resource,
                new ClientCredential(applicationClientInfo.ClientId, applicationClientInfo.ClientSecret));

            return token.AccessToken;
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
    }
}