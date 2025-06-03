namespace CloudreveDesktop.pojo;

/**
 * 任务实体
 */
public class TaskPojo
{
    private string? Type { get; set; } // 任务类型
    private string? OldPathName { get; set; } // 旧路径
    private string? NewPathName { get; set; } // 信路径
}