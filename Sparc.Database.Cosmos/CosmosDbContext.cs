using Microsoft.Azure.Cosmos;
using Sparc.Core;
using System;
using System.Collections.Generic;

namespace Sparc.Database.Cosmos
{
    public class CosmosDbContext
    {
        Microsoft.Azure.Cosmos.Database Database { get; set; }
        protected static Dictionary<Type, string> ContainerMapping { get; set; } = new Dictionary<Type, string>();
        public bool AllowSynchronousQueries { get; set; }

        public CosmosDbContext(Microsoft.Azure.Cosmos.Database database)
        {
            Database = database;
        }

        public static void Map<T>(string containerName) where T : Root
        {
            if (!ContainerMapping.ContainsKey(typeof(T)))
                ContainerMapping.Add(typeof(T), containerName);
        }

        public Container Container(string containerName)
        {
            return Database.GetContainer(containerName);
        }

        public Container Container<T>()
        {
            return Container(ContainerMapping[typeof(T)]);
        }
    }
}
