using GameTemplate.Core.DevTools;

namespace GameTemplate.Gameplay.Core
{
    /// <summary>
    /// Cheat commands cho GameStateManager.
    /// 
    /// Đây là lý do mình chọn Singleton cho GameStateManager thay vì ServiceLocator:
    /// CheatCommand là method static, nếu dùng ServiceLocator phải gọi 
    /// ServiceLocator.Get<IGameStateManager>() mỗi lần - dài dòng.
    /// 
    /// Với Singleton: GameStateManager.Instance.X - ngắn gọn, ai cũng đọc hiểu.
    /// 
    /// Trade-off: code này couple chặt với class GameStateManager cụ thể. 
    /// Không test được riêng. Nhưng đây là code DEV TOOLS (strip khỏi release build) 
    /// nên không cần test nghiêm túc.
    /// </summary>
    public static class GameplayCheats
    {
        [CheatCommand("god_mode", "Toggle god mode")]
        static void GodMode(string[] args)
        {
            var gsm = GameStateManager.Instance;
            gsm.IsGodMode = !gsm.IsGodMode;
            UnityEngine.Debug.Log($"God mode: {gsm.IsGodMode}");
        }

        [CheatCommand("add_score", "add_score <amount>")]
        static void AddScore(string[] args)
        {
            if (args.Length == 0) return;
            if (int.TryParse(args[0], out var amount))
            {
                GameStateManager.Instance.AddScore(amount);
                UnityEngine.Debug.Log($"Added {amount} score.");
            }
        }

        [CheatCommand("end_game", "Force end game. Usage: end_game [win|lose]")]
        static void EndGame(string[] args)
        {
            bool isWin = args.Length > 0 && args[0] == "win";
            GameStateManager.Instance.EndSession(isWin);
        }

        [CheatCommand("pause", "Toggle pause")]
        static void TogglePause(string[] args)
        {
            GameStateManager.Instance.TogglePause();
        }

        [CheatCommand("session_info", "Print current session info")]
        static void SessionInfo(string[] args)
        {
            var gsm = GameStateManager.Instance;
            UnityEngine.Debug.Log(
                $"State: {gsm.CurrentState.Value}\n" +
                $"Score: {gsm.Score.Value}\n" +
                $"Combo: {gsm.Combo.Value}\n" +
                $"Time: {gsm.SessionTime.Value:F1}s\n" +
                $"Paused: {gsm.IsPaused.Value}\n" +
                $"GodMode: {gsm.IsGodMode}");
        }
    }
}
