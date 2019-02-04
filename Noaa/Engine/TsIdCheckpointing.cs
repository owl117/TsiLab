using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Engine
{
    public sealed class TsidCheckpointing
    {
        private readonly CloudTable _cloudTable;
        private readonly string _partitionKey;

        private TsidCheckpointing(CloudTable cloudTable, string partitionKey)
        {
            _cloudTable = cloudTable;
            _partitionKey = partitionKey;
        }

        public static async Task<TsidCheckpointing> CreateAsync(AzureUtils.StorageAccountInfo storageAcountInfo, string tableName, string partitionKey)
        {
            return new TsidCheckpointing(
                await AzureUtils.GetOrCreateTableAsync(storageAcountInfo, tableName),
                partitionKey);
        }

        public async Task<DateTimeOffset?> GetLastCommittedTimestampAsync(string tsid)
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<LastCommittedTimestampByTsid>(_partitionKey, tsid);

            TableResult retrievedResult = await _cloudTable.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                return ((LastCommittedTimestampByTsid)retrievedResult.Result).LastCommittedTimestamp;
            }
            else
            {
                return null;
            }
        }

        public async Task SetLastCommittedTimestampAsync(string tsid, DateTimeOffset timestamp)
        {
            TableOperation replaceOperation = TableOperation.InsertOrMerge(
                new LastCommittedTimestampByTsid(_partitionKey, tsid, timestamp));

            await _cloudTable.ExecuteAsync(replaceOperation);
        }

        public sealed class LastCommittedTimestampByTsid : TableEntity
        {
            public LastCommittedTimestampByTsid(string partitionKey, string tsid, DateTimeOffset lastCommittedTimestamp) 
                : base(partitionKey: partitionKey, rowKey: tsid)
            {
                LastCommittedTimestamp = lastCommittedTimestamp;
            }

            public LastCommittedTimestampByTsid()
            {
            }

            public DateTimeOffset LastCommittedTimestamp { get; set; }
        }
    }
}