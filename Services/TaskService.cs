namespace CloudreveDesktop.Services;

/**
 * 任务列表，异步处理增删改
 */
public class TaskService
{
    public static readonly TaskService Instance = new(); // 需要持久化

    /**
     * 添加修改名称任务
     */
    public void AddReNameTask(string oldName, string newName)
    {
    }
}