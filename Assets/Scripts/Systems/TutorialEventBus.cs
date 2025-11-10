using System;

/// <summary>
/// Central event hub so gameplay systems can notify the tutorial about task completions
/// without taking a direct dependency on the UI controller.
/// </summary>
public static class TutorialEventBus
{
    /// <summary>Invoked every time a gameplay script reports that a tutorial task was completed.</summary>
    public static event Action<string> TaskCompleted;

    /// <summary>
    /// Report the completion of a task. The id must match one of the task ids configured in the UI sequence.
    /// </summary>
    public static void RaiseTaskCompleted(string taskId)
    {
        if (string.IsNullOrEmpty(taskId))
        {
            return;
        }

        TaskCompleted?.Invoke(taskId);
    }
}

/// <summary>
/// Shared ids for the default tutorial tasks so the same strings are reused everywhere.
/// </summary>
public static class TutorialTaskIds
{
    public const string MoveHorizontal = "move_horizontal";
    public const string Jump = "jump";
    public const string DrawLine = "draw_line";
    public const string KillWithLine = "kill_with_line";
    public const string ConvertAlly = "convert_enemy_ally";
    public const string FinishRoute = "finish_route";
}
