using Sparc.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

//namespace Kuvio.Plugins.Database.Cosmos
//{
//    public class CosmosDbQuery<T> : IQuery<T> where T : class
//    {
//        private readonly IQueryable<T> BaseQuery;

//        public CosmosDbQuery(DbContext context, string? partitionKey = null)
//        {
//            BaseQuery = context.Set<T>().AsNoTracking();
//            if (!string.IsNullOrWhiteSpace(partitionKey))
//                BaseQuery = BaseQuery.WithPartitionKey(partitionKey);
//        }

//        public Type ElementType => BaseQuery.ElementType;

//        public Expression Expression => BaseQuery.Expression;

//        public IQueryProvider Provider => BaseQuery.Provider;

//        public IEnumerator<T> GetEnumerator() => BaseQuery.GetEnumerator();

//        IEnumerator IEnumerable.GetEnumerator() => BaseQuery.GetEnumerator();
//    }
//}
