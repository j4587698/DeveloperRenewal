using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphLib.Entity;
using LiteDB;

namespace GraphLib.Utils
{
    public class LiteDbHelper
    {
        public static string dbUrl = "db.db";

        private readonly ILiteDatabase db;

        private static LiteDbHelper instance;

        private static object lockObj = new object();

        public static LiteDbHelper Instance
        {
            get
            {
                lock (lockObj)
                {
                    return instance ?? (instance = new LiteDbHelper());
                }
            }
        }

        public static void InitDb(ILiteDatabase liteDatabase)
        {
            instance = new LiteDbHelper(liteDatabase);
        }

        private LiteDbHelper()
        {
            db = new LiteDatabase(dbUrl);
        }

        private LiteDbHelper(ILiteDatabase liteDatabase)
        {
            db = liteDatabase;
        }

        public ILiteCollection<T> GetCollection<T>(string tableName) where T : IBaseEntity
        {
            return db.GetCollection<T>(tableName);
        }

        public bool Any<T>(string tableName, Expression<Func<T, bool>> expression) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.FindOne(expression) != null;
        }

        /// <summary>
        /// 插入或更新数据库内容
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="data">要处理的数据</param>
        public void InsertOrUpdate<T>(string tableName, T data) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            col.Upsert(data);
        }

        public void InsertOrUpdateBatch<T>(string tableName, IEnumerable<T> datas) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            var insertDatas = datas.Where(x => x.Id == 0);
            if (insertDatas.Any())
            {
                col.Insert(insertDatas);
            }
            var updateDatas = datas.Where(x => x.Id != 0);
            if (updateDatas.Any())
            {
                col.Update(updateDatas);
            }
        }

        public void Delete<T>(string tableName, T data) where T : IBaseEntity
        {
            Delete(tableName, data.Id);
        }

        public void Delete(string tableName, int id) 
        {
            var col = db.GetCollection(tableName);
            col.Delete(id);
        }

        public void Delete<T>(string tableName, IEnumerable<T> datas) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            col.DeleteMany(Query.In("_id", IDBDaoToBsonArray(datas)));
        }

        public void Delete(string tableName, int[] ids)
        {
            var col = db.GetCollection<IBaseEntity>(tableName);
            col.DeleteMany(x => ids.Contains(x.Id));
        }

        private BsonArray IDBDaoToBsonArray<T>(IEnumerable<T> datas) where T : IBaseEntity
        {
            BsonArray ba = new BsonArray();
            foreach (var data in datas)
            {
                ba.Add(data.Id);
            }
            return ba;
        }

        public void DeleteAll<T>(string tableName) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            col.DeleteMany(x => true);
        }

        public T GetFirstData<T>(string tableName) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.FindOne(Query.All());
        }

        public IEnumerable<T> GetAllData<T>(string tableName) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.FindAll();
        }

        public IEnumerable<T> GetAllData<T>(string tableName, string includePath1) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.Include(includePath1).FindAll();
        }

        public IEnumerable<T> GetAllData<T>(string tableName, string includePath1, string includePath2) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.Include(includePath1).Include(includePath2).FindAll();
        }

        public IEnumerable<T> GetAllData<T>(string tableName, params string[] includePaths) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            foreach (var includePath in includePaths)
            {
                col = col.Include(includePath);
            }
            return col.FindAll();
        }

        public List<T> GetAllDataToList<T>(string tableName) where T : IBaseEntity
        {
            return GetAllData<T>(tableName).ToList();
        }

        public T GetDataById<T>(string tableName, int id) where T : IBaseEntity
        {
            var col = db.GetCollection<T>(tableName);
            return col.FindById(id);
        }
    }
}