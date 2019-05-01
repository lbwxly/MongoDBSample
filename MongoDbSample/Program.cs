using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;

namespace MongoDbSample
{
    public abstract class Entity
    {
        public Guid Id { get; set; }
    }
    class Project : Entity
    {
        public Project()
        {
            //Modules = new List<Module>();
        }
        public string Name { get; set; }
        public List<MongoDBRef> Modules { get; set; }
    }

    class Module : Entity
    {
        public string Name { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DBRefSample();
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static void EmbedDocumentSample()
        {
            var id = Guid.NewGuid();
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("i18nMgr");
            var projCollection = db.GetCollection<BsonDocument>("projects");
            projCollection.InsertOne(new BsonDocument
            {
                {"_id",id },
                {"Name","Proj1"},
                {"Modules",new BsonArray
                    {
                        new BsonDocument{ {"Name","Module1"}}
                    }
                }
            });
        }

        /// <summary>
        /// Sample for DBRef
        /// https://docs.mongodb.com/manual/reference/database-references/#dbref-explanation
        /// </summary>
        static void DBRefSample()
        {
            var id = Guid.NewGuid();
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("i18nMgr");
            var projCollection = db.GetCollection<Project>("projects");
            var moduleCollection = db.GetCollection<Module>("modules");

            // Insert document separately.
            var moduleId = Guid.NewGuid();
            moduleCollection.InsertOne(new Module() { Id = moduleId, Name = "Module1" });
            projCollection.InsertOne(new Project
            {
                Id = id,
                Name = "Proj1",
                Modules = new List<MongoDBRef>() { new MongoDBRef("modules", moduleId) }
            });

            // Find with id.
            var project = projCollection.Find(new BsonDocument { { "_id", id } }).FirstOrDefault();

            // Fetch DbRef.
            var module = db.FetchDbRef<Module>(project.Modules[0]);

            // Update
            var updateDef = Builders<Project>.Update.Set(x => x.Name, "Pro1(Updated)");
            var filter = Builders<Project>.Filter.Eq(x => x.Id, id);
            var updateResult = projCollection.UpdateOne(filter, updateDef);

            // Find again.
            project = projCollection.Find(new BsonDocument { { "_id", id } }).FirstOrDefault();
        }

        /// <summary>
        /// Sample for operator.
        /// https://docs.mongodb.com/manual/reference/operator/
        /// Operator can be used when query with BsonDocument as parameter.
        /// C# driver also provide the BuilderDefinition instead.
        /// http://mongodb.github.io/mongo-csharp-driver/2.7/reference/driver/definitions/#definitions-and-builders
        /// </summary>
        static void StageOperatorSample()
        {

        }

        /// <summary>
        /// Sample for aggregation
        /// https://docs.mongodb.com/manual/aggregation/
        /// </summary>
        static void AggregationSample()
        {

        }

        /// <summary>
        /// Sample for transaction
        /// https://docs.mongodb.com/manual/core/transactions/
        /// </summary>
        static void TransactionSample()
        {

        }
    }
}
