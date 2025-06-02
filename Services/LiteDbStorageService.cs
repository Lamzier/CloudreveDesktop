using System.Diagnostics;
using LiteDB;

namespace CloudreveDesktop.Services;

public sealed class LiteDbStorageService : IDisposable
{
    private readonly ILiteCollection<KeyValueItem> _collection;
    private readonly ILiteDatabase _db;

    public LiteDbStorageService(string fileName)
    {
        var path = App.TempPath + "/" + fileName;

        var conn = new ConnectionString
        {
            Filename = path,
            Connection = ConnectionType.Direct, // 直接访问模式提升30%性能
            InitialSize = 100 * 1024 * 1024, // 预分配大文件减少碎片
            Collation = Collation.Binary // 二进制比较加速查询
        };

        _db = new LiteDatabase(conn);
        _collection = _db.GetCollection<KeyValueItem>(null!); // 匿名集合省内存
        _collection.EnsureIndex(x => x.Key, true);
        RemoveAll(); // 每次使用都初始化数据库
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    public void Put(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;

        _collection.Upsert(new KeyValueItem
        {
            Key = key,
            Value = value,
            _id = key // 直接复用Key作为_id避免Bson生成开销
        });

        if (Debugger.IsAttached && _collection.Count() % 100 == 0)
            _db.Checkpoint(); // 调试模式下定期提交
    }

    public string Get(string key)
    {
        return _collection.FindById(key)?.Value!;
    }

    public void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _collection.Delete(key);
    }

    public void RemoveAll()
    {
        _collection.DeleteAll();
        _collection.EnsureIndex(x => x.Key, true);
    }

    private class KeyValueItem
    {
        [BsonId] public string _id { get; set; } // 使用Key直接作为主键

        public string Key
        {
            get => _id; // 映射关系
            set => _id = value;
        }

        public string Value { get; set; }
    }
}