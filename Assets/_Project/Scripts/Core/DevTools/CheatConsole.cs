using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameTemplate.Core.DevTools
{
    // ====================================================================
    // CHEAT CONSOLE - runtime debug overlay.
    //
    // QUAN TRỌNG: Toàn bộ file này chỉ compile khi:
    //   - Đang ở Unity Editor
    //   - HOẶC build có flag DEVELOPMENT_BUILD
    //
    // -> Release build production tự strip hoàn toàn, không tăng size build.
    //
    // Cách bật/tắt trong code khi build:
    //   File > Build Settings > Development Build checkbox
    // ====================================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD

    /// <summary>
    /// Console runtime hiện overlay GUI, type lệnh để chạy.
    /// Vd: "add_coin 1000", "set_level 5", "god_mode on"
    ///
    /// Toggle bằng nút Tab trên Editor, hoặc 3-finger tap trên mobile dev build.
    ///
    /// Cách thêm command:
    ///   CheatConsole.RegisterCommand("kill_all", "Diệt mọi enemy", args => { ... });
    ///
    /// Hoặc dùng attribute trên static method:
    ///   [CheatCommand("god_mode", "Bật/tắt god mode")]
    ///   static void GodMode(string[] args) { ... }
    /// </summary>
    public class CheatConsole : MonoBehaviour
    {
        private static CheatConsole _instance;
        private static readonly Dictionary<string, CommandInfo> _commands = new Dictionary<string, CommandInfo>();
        private readonly List<string> _outputLines = new List<string>(64);
        private string _input = "";
        private bool _isVisible;
        private Vector2 _outputScroll;

        // Touch detection cho mobile
        private float _lastMultiTouchTime;

        public static void Show()
        {
            EnsureInstance();
            _instance._isVisible = true;
        }

        public static void Hide()
        {
            if (_instance != null) _instance._isVisible = false;
        }

        public static void Toggle()
        {
            EnsureInstance();
            _instance._isVisible = !_instance._isVisible;
        }

        private static void EnsureInstance()
        {
            if (_instance != null) return;
            var go = new GameObject("[CheatConsole]");
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<CheatConsole>();
            DontDestroyOnLoad(go);
            _instance.RegisterBuiltInCommands();
            _instance.DiscoverAttributeCommands();
        }

        public static void RegisterCommand(string name, string description, Action<string[]> handler)
        {
            EnsureInstance();
            _commands[name.ToLowerInvariant()] = new CommandInfo
            {
                Name = name,
                Description = description,
                Handler = handler
            };
        }

        private void Awake()
        {
            // Auto-init khi runtime start (nếu chưa được tạo qua RegisterCommand)
            if (_instance == null) _instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        private void Update()
        {
            // Editor: Tab key
            if (Application.isEditor && Input.GetKeyDown(KeyCode.Tab))
                Toggle();

            // Mobile: 3-finger tap
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount >= 3 && Time.time - _lastMultiTouchTime > 1f)
            {
                _lastMultiTouchTime = Time.time;
                Toggle();
            }
#endif
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            const float padding = 10f;
            float width = Screen.width - padding * 2;
            float height = Screen.height * 0.5f;

            GUI.Box(new Rect(padding, padding, width, height), "Cheat Console (type 'help')");

            // Output area
            var outputRect = new Rect(padding + 5, padding + 25, width - 10, height - 70);
            GUILayout.BeginArea(outputRect);
            _outputScroll = GUILayout.BeginScrollView(_outputScroll);
            foreach (var line in _outputLines)
                GUILayout.Label(line, GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Input field
            var inputRect = new Rect(padding + 5, padding + height - 40, width - 80, 30);
            GUI.SetNextControlName("CheatInput");
            _input = GUI.TextField(inputRect, _input);

            var executeRect = new Rect(padding + width - 70, padding + height - 40, 60, 30);
            bool execute = GUI.Button(executeRect, "Run");

            // Enter key
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                execute = true;

            if (execute && !string.IsNullOrWhiteSpace(_input))
            {
                Execute(_input);
                _input = "";
                GUI.FocusControl("CheatInput");
            }

            // Auto focus
            if (GUI.GetNameOfFocusedControl() == "")
                GUI.FocusControl("CheatInput");
        }

        private void Execute(string commandLine)
        {
            _outputLines.Add($"> {commandLine}");
            var parts = commandLine.Trim().Split(' ');
            var name = parts[0].ToLowerInvariant();
            var args = parts.Skip(1).ToArray();

            if (!_commands.TryGetValue(name, out var cmd))
            {
                _outputLines.Add($"Unknown command: {name}. Type 'help' for list.");
                return;
            }

            try
            {
                cmd.Handler(args);
            }
            catch (Exception ex)
            {
                _outputLines.Add($"Error: {ex.Message}");
            }

            // Cap output history
            while (_outputLines.Count > 100) _outputLines.RemoveAt(0);
            _outputScroll.y = float.MaxValue; // auto scroll
        }

        private void RegisterBuiltInCommands()
        {
            RegisterCommand("help", "List all commands", args =>
            {
                _outputLines.Add("=== Available commands ===");
                foreach (var c in _commands.Values.OrderBy(c => c.Name))
                    _outputLines.Add($"  {c.Name} - {c.Description}");
            });

            RegisterCommand("clear", "Clear console", args => _outputLines.Clear());

            RegisterCommand("close", "Close console", args => Hide());

            RegisterCommand("fps", "Show current FPS", args =>
            {
                _outputLines.Add($"FPS: {(1f / Time.smoothDeltaTime):F1}");
            });

            RegisterCommand("scene", "Print active scene name", args =>
            {
                _outputLines.Add($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            });

            RegisterCommand("time_scale", "Set Time.timeScale. Usage: time_scale 0.5", args =>
            {
                if (args.Length > 0 && float.TryParse(args[0], out var ts))
                {
                    Time.timeScale = ts;
                    _outputLines.Add($"Time.timeScale = {ts}");
                }
                else _outputLines.Add($"Current Time.timeScale = {Time.timeScale}");
            });
        }

        private void DiscoverAttributeCommands()
        {
            // Tự discover các method có [CheatCommand] attribute
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    var methods = type.GetMethods(
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var attr = (CheatCommandAttribute)Attribute.GetCustomAttribute(method, typeof(CheatCommandAttribute));
                        if (attr == null) continue;
                        var capturedMethod = method;
                        RegisterCommand(attr.Name, attr.Description, args =>
                        {
                            capturedMethod.Invoke(null, new object[] { args });
                        });
                    }
                }
            }
        }

        private class CommandInfo
        {
            public string Name;
            public string Description;
            public Action<string[]> Handler;
        }
    }

    /// <summary>
    /// Đánh dấu method static để CheatConsole tự register.
    /// Cách dùng:
    ///   [CheatCommand("add_coin", "Thêm coin: add_coin 100")]
    ///   static void AddCoin(string[] args)
    ///   {
    ///       int amount = int.Parse(args[0]);
    ///       PlayerData.Coins += amount;
    ///   }
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CheatCommandAttribute : Attribute
    {
        public string Name;
        public string Description;
        public CheatCommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }

#else

    // Release build: stub trống. Code gameplay vẫn có thể gọi CheatConsole.RegisterCommand
    // nhưng compiler optimize ra hết. Không tăng size build.
    public static class CheatConsole
    {
        public static void Show() { }
        public static void Hide() { }
        public static void Toggle() { }
        public static void RegisterCommand(string name, string description, System.Action<string[]> handler) { }
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CheatCommandAttribute : System.Attribute
    {
        public CheatCommandAttribute(string name, string description = "") { }
    }

#endif
}
