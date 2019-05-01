using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;

namespace MongoDbSample
{
    public static class IMongoDbExtension
    {
        public static T FetchDbRef<T>(this IMongoDatabase db, MongoDBRef dbRef) where T : Entity
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, dbRef.Id);
            var collection = db.GetCollection<T>(dbRef.CollectionName);

            return collection.Find(filter).FirstOrDefault();
        }
    }
}
