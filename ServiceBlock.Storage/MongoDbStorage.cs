using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using ServiceBlock.Interface.Resource;
using ServiceBlock.Interface;
using ServiceBlock.Interface.Storage;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ServiceBlock.Storage.Options;

namespace ServiceBlock.Storage
{
    public class MongoDbStorage<T> : Storage<T> where T : AbstractResource
    {
        private readonly IMongoCollection<T> resources;
        private readonly ILogger<MemoryStorage<T>> _logger;

        public MongoDbStorage(ILogger<MemoryStorage<T>> logger, IConfiguration config, IOptions<MongoDb> options, ResourceTransformer<T>? transformer = null) : base(logger, transformer)
        {
            var client = new MongoClient(config.GetConnectionString(nameof(MongoDb)));
            var database = client.GetDatabase(options.Value.DatabaseName);

            resources = database.GetCollection<T>(typeof(T).Name);

            _logger = logger;
        }


        protected override async Task<IEnumerable<T>> ReadItems(Dictionary<string, string> searchParams)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Empty;
            foreach (var kv in searchParams)
            {
                filter = filter & builder.Eq(kv.Key, kv.Value);
            }
            return (await resources.FindAsync(filter)).ToEnumerable();
        }

        private bool CheckSearchParam(T item, (string name, string val) queryParam)
        {

            var propInfo = typeof(T).GetProperty(queryParam.name);
            var propVal = propInfo.GetValue(item);

            var propString = propInfo.PropertyType == typeof(string) ? (string)propVal : JsonConvert.SerializeObject(propVal);
            var propStringNormalized = Regex.Replace(propString, @"\s", "");

            var queryParamValNormalized = Regex.Replace(queryParam.val, @"\s", "");


            return propStringNormalized.Equals(queryParamValNormalized, StringComparison.InvariantCultureIgnoreCase);
        }

        protected override async Task<T> ReadItem(Guid Id)
        {

            var resource = (await resources.FindAsync<T>(r => r.Id == Id)).FirstOrDefault();

            if (resource == null)
                throw new NotFoundException($"Could not find {typeof(T).Name} with ID {Id}");

            return resource;
        }

        protected override async Task<T> CreateItem(T resource)
        {
            await resources.InsertOneAsync(resource);
            return resource;
        }

        protected override async Task DeleteItem(Guid Id) => await resources.DeleteOneAsync(r => r.Id == Id);


        protected override async Task<T> UpdateItem(T resource)
        {
            var result = await resources.ReplaceOneAsync(r => r.Id == resource.Id, resource);

            if (result.IsAcknowledged && result.MatchedCount < 1)
            {
                throw new NotFoundException($"Could not find {typeof(T).Name} with ID {resource.Id}");
            }

            return resource;
        }
    }
}