namespace CloudreveDesktop.pojo;

public class FilePojo
{
    public string Id { get; set; } // id
    public string Name { get; set; } // 名称
    public string Path { get; set; } // 路径
    public long Size { get; set; } // 大小 byte
    public string Type { get; set; } // 类型
    public DateTimeOffset Date { get; set; } // 修改时间
    public DateTimeOffset CreateDate { get; set; } // 创建时间
}