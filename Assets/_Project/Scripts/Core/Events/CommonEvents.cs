namespace GameTemplate.Core.Events
{
    /// <summary>
    /// Các event chung cho hầu hết game. Mỗi game con có thể thêm event riêng
    /// trong namespace GameTemplate.Gameplay.Events.
    /// </summary>

    public struct GameStartedEvent : IGameEvent { }
    public struct GamePausedEvent : IGameEvent { public bool IsPaused; }
    public struct GameOverEvent : IGameEvent { public bool IsWin; public int Score; }
    public struct LevelLoadedEvent : IGameEvent { public int LevelIndex; }
    public struct SceneLoadStartedEvent : IGameEvent { public string SceneName; }
    public struct SceneLoadCompletedEvent : IGameEvent { public string SceneName; }
}
