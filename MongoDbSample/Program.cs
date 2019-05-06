using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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

        [BsonIgnore]
        public DateTime StartTime { get; set; }

        [BsonIgnore]
        public List<MongoDBRef> Modules { get; set; }

        [BsonIgnore]
        public int ModuleCount { get; set; }
    }

    class Module : Entity
    {
        public string Name { get; set; }
    }

    class Program
    {
        static string DatabaseName = "TranslationManager";
        static void Main(string[] args)
        {
            SimpleSample();
            //DBRefSample();
            //OperatorSample();
            //AggregationSample();
            //TransactionSample();
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        static void SimpleSample()
        {
            var id = Guid.NewGuid();
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase(DatabaseName);
            var projCollection = db.GetCollection<Project>("projects");
            projCollection.InsertOne(new Project { Id = id, Name = "Proj2", StartTime = DateTime.Now });
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
        static void OperatorSample()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("i18nMgr");
            var projCollection = db.GetCollection<Project>("projects");

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj1",
                StartTime = new DateTime(2018, 2, 4)
            });

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj1",
                StartTime = new DateTime(2019, 2, 4)
            });

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj1",
                StartTime = new DateTime(2019, 4, 4)
            });

            // operator.
            var projs = projCollection.Find(new BsonDocument
                                            {
                                                { "StartTime", new BsonDocument("$gt",new DateTime(2018,5,1))}
                                            }).ToList();
            Console.WriteLine($"Find with operator result count:{projs.Count}");

            // filter definition.
            var builder = Builders<Project>.Filter;
            var filter = builder.Gt(x => x.StartTime, new DateTime(2018, 5, 1));
            projs = projCollection.Find(filter).ToList();

            Console.WriteLine($"Find with filter definition result count:{projs.Count}");
        }

        /// <summary>
        /// Sample for aggregation stage operator.
        /// https://docs.mongodb.com/manual/aggregation/
        /// </summary>
        static void AggregationSample()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("i18nMgr");
            var projCollection = db.GetCollection<Project>("projects");

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj1",
                ModuleCount = 2,
                StartTime = new DateTime(2018, 2, 4)
            });

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj2",
                ModuleCount = 4,
                StartTime = new DateTime(2019, 2, 4)
            });

            projCollection.InsertOne(new Project
            {
                Id = Guid.NewGuid(),
                Name = "Proj3",
                ModuleCount = 1,
                StartTime = new DateTime(2019, 2, 4)
            });

            var pipelineDefinition = new BsonDocument[]
            {
                new BsonDocument{{"$match", new BsonDocument("StartTime",new DateTime(2019,2,4))}},
                new BsonDocument{{"$group",new BsonDocument
                {
                    { "_id",new BsonDocument("Name","$Name")},
                    { "TotalModules",new BsonDocument("$sum","$ModuleCount")}
                }}},
                new BsonDocument{{"$sort",new BsonDocument("_id.Name", -1)}},
            };
            var projModuleCounts = projCollection.Aggregate<BsonDocument>(pipelineDefinition).ToList();
            var result = projCollection.Aggregate<Project>()
                .Match(x => x.StartTime == new DateTime(2019, 2, 4))
                .Group(x => x.Name, g => new { Name = g.Key, Count = g.Sum(x => x.ModuleCount) }).ToList();

        }

        /// <summary>
        /// Sample for transaction
        /// https://docs.mongodb.com/manual/core/transactions/
        /// </summary>
        static void TransactionSample()
        {
            var id = Guid.NewGuid();
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("i18nMgr");
            var session = client.StartSession();
            session.StartTransaction(new TransactionOptions(ReadConcern.Snapshot,
                                                            new Optional<ReadPreference>(),
                                                            WriteConcern.WMajority));
            try
            {
                var projCollection = db.GetCollection<Project>("projects");
                var moduleCollection = db.GetCollection<Module>("modules");

                // Insert document separately.
                var moduleId = Guid.NewGuid();
                moduleCollection.InsertOne(session, new Module() { Id = moduleId, Name = "Module1" });
                //projCollection.InsertOne(session, new Project
                //{
                //    Id = id,
                //    Name = "Proj1",
                //    Modules = new List<MongoDBRef>() { new MongoDBRef("modules", moduleId) }
                //});

                session.CommitTransaction();
            }
            catch (Exception e)
            {
                session.AbortTransaction();
                Console.WriteLine(e);
            }
        }
    }
}
