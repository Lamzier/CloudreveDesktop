namespace CloudreveDesktop.pojo;

public class NfsInfoPojo
{
    public bool IsEnable { get; set; }
    public string NfsPath { get; set; }
    public DateTimeOffset Date { get; set; } // 修改时间
    public DateTimeOffset CreateDate { get; set; } // 创建时间
}