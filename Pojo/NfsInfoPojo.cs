namespace CloudreveDesktop.pojo;

public class NfsInfoPojo
{
    public NfsInfoPojo()
    {
        Instance = this;
    }

    public long Id { get; set; }
    public string Name { get; set; } // 卷名称
    public bool IsEnable { get; set; }
    public string NfsPath { get; set; } = null!;
    public DateTimeOffset Date { get; set; } // 修改时间
    public DateTimeOffset CreateDate { get; set; } // 创建时间

    public NfsInfoPojo Instance { get; set; }

    public string Drive { get; set; } = null!; // 挂载的盘符
}