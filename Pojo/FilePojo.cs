namespace CloudreveDesktop.pojo;

public class FilePojo
{
    public FilePojo()
    {
        Instance = this;
    }

    public FilePojo Instance { get; set; }

    public string Id { get; set; } = null!; // id
    public string Name { get; set; } = null!; // 名称
    public string Path { get; set; } = null!; // 路径
    public long Size { get; set; } // 大小 byte
    public string Type { get; set; } = null!; // 类型
    public DateTimeOffset Date { get; set; } // 修改时间
    public DateTimeOffset CreateDate { get; set; } // 创建时间
}