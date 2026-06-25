using System;
using System.Collections.Generic;
using UnityEngine;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.Tutorial
{
    /// <summary>
    /// Chạy tuần tự các ITutorialStep, lưu tiến độ vào PlayerPrefs, hỗ trợ skip/resume.
    ///
    /// Cách dùng:
    ///   var tutorial = new TutorialSequencer("rpg_intro", overlay);
    ///   tutorial
    ///       .AddStep(new MessageStep("welcome", "Chào mừng!"))
    ///       .AddStep(new WaitForClickStep("open_shop", () => shopBtn, "Bấm để mở shop"))
    ///       .AddStep(new WaitForConditionStep("move", () => player.HasMoved, "Di chuyển"));
    ///   tutorial.OnCompleted += () => Debug.Log("Tutorial xong!");
    ///   tutorial.Start();
    ///
    ///   // Trong Update của 1 MonoBehaviour:
    ///   tutorial.Tick();
    ///
    /// Tiến độ lưu PlayerPrefs: lần sau mở game, tutorial đã hoàn thành sẽ KHÔNG chạy lại.
    /// </summary>
    public class TutorialSequencer
    {
        private const string COMPLETED_PREFIX = "Tutorial_Completed_";
        private const string PROGRESS_PREFIX = "Tutorial_Progress_";

        private readonly string _tutorialId;
        private readonly ITutorialOverlay _overlay;
        private readonly List<ITutorialStep> _steps = new List<ITutorialStep>();
        private readonly TutorialContext _context;

        private int _currentIndex = -1;
        private bool _isRunning;
        private bool _stepEntered;

        // ===== Events =====
        /// <summary>Fire khi toàn bộ tutorial hoàn thành.</summary>
        public event Action OnCompleted;

        /// <summary>Fire khi tutorial bị skip.</summary>
        public event Action OnSkipped;

        /// <summary>Fire khi 1 step hoàn thành. Param: step vừa xong.</summary>
        public event Action<ITutorialStep> OnStepCompleted;

        // ===== Public state =====
        public bool IsRunning => _isRunning;
        public string TutorialId => _tutorialId;
        public ITutorialStep CurrentStep =>
            (_currentIndex >= 0 && _currentIndex < _steps.Count) ? _steps[_currentIndex] : null;

        public TutorialSequencer(string tutorialId, ITutorialOverlay overlay)
        {
            _tutorialId = tutorialId;
            _overlay = overlay;
            _context = new TutorialContext(overlay, SkipAll);
        }

        // ============================================================
        // BUILDER
        // ============================================================
        public TutorialSequencer AddStep(ITutorialStep step)
        {
            _steps.Add(step);
            return this;
        }

        // ============================================================
        // CONTROL
        // ============================================================

        /// <summary>
        /// Bắt đầu tutorial. Nếu đã hoàn thành trước đó (saved), KHÔNG chạy lại
        /// (trừ khi forceRestart = true).
        /// </summary>
        public void Start(bool forceRestart = false)
        {
            if (!forceRestart && IsCompleted(_tutorialId))
            {
                GameLog.Info(LogCategory.UI,
                    $"[Tutorial] '{_tutorialId}' đã hoàn thành trước đó - skip.");
                return;
            }

            if (_steps.Count == 0)
            {
                GameLog.Warning(LogCategory.UI, $"[Tutorial] '{_tutorialId}' không có step nào.");
                return;
            }

            _isRunning = true;
            _overlay.Show();

            // Resume từ step đã lưu (nếu có), hoặc bắt đầu từ 0
            int savedProgress = forceRestart ? 0 : GetSavedProgress(_tutorialId);
            _currentIndex = Mathf.Clamp(savedProgress, 0, _steps.Count - 1);
            _stepEntered = false;

            GameLog.Info(LogCategory.UI,
                $"[Tutorial] '{_tutorialId}' start tại step {_currentIndex} ({CurrentStep?.StepId}).");
        }

        /// <summary>
        /// Gọi mỗi frame (từ Update của MonoBehaviour host).
        /// Engine sẽ Enter step → check IsComplete → Exit → sang step tiếp.
        /// </summary>
        public void Tick()
        {
            if (!_isRunning) return;
            if (_currentIndex < 0 || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];

            // Enter step lần đầu
            if (!_stepEntered)
            {
                step.Enter(_context);
                _stepEntered = true;
                return; // đợi frame sau mới check complete (cho UI render)
            }

            // Check complete
            if (step.IsComplete())
            {
                step.Exit();
                OnStepCompleted?.Invoke(step);

                _currentIndex++;
                _stepEntered = false;

                // Lưu tiến độ
                SaveProgress(_tutorialId, _currentIndex);

                // Hết step → hoàn thành
                if (_currentIndex >= _steps.Count)
                {
                    Complete();
                }
            }
        }

        /// <summary>Skip toàn bộ tutorial (vd: nút Skip).</summary>
        public void SkipAll()
        {
            if (!_isRunning) return;

            // Exit step hiện tại nếu đang chạy
            if (_stepEntered && CurrentStep != null)
            {
                CurrentStep.Exit();
            }

            MarkCompleted(_tutorialId);
            _isRunning = false;
            _overlay.Hide();

            GameLog.Info(LogCategory.UI, $"[Tutorial] '{_tutorialId}' bị skip.");
            OnSkipped?.Invoke();
        }

        private void Complete()
        {
            MarkCompleted(_tutorialId);
            _isRunning = false;
            _overlay.Hide();

            GameLog.Info(LogCategory.UI, $"[Tutorial] '{_tutorialId}' hoàn thành.");
            OnCompleted?.Invoke();
        }

        // ============================================================
        // PERSISTENCE (PlayerPrefs)
        // ============================================================

        /// <summary>Check tutorial đã hoàn thành chưa (static - gọi không cần instance).</summary>
        public static bool IsCompleted(string tutorialId)
            => PlayerPrefs.GetInt(COMPLETED_PREFIX + tutorialId, 0) == 1;

        private static void MarkCompleted(string tutorialId)
        {
            PlayerPrefs.SetInt(COMPLETED_PREFIX + tutorialId, 1);
            PlayerPrefs.DeleteKey(PROGRESS_PREFIX + tutorialId); // xoá progress vì đã xong
            PlayerPrefs.Save();
        }

        private static int GetSavedProgress(string tutorialId)
            => PlayerPrefs.GetInt(PROGRESS_PREFIX + tutorialId, 0);

        private static void SaveProgress(string tutorialId, int stepIndex)
        {
            PlayerPrefs.SetInt(PROGRESS_PREFIX + tutorialId, stepIndex);
            PlayerPrefs.Save();
        }

        /// <summary>Reset tutorial về chưa hoàn thành (cho cheat/test).</summary>
        public static void ResetTutorial(string tutorialId)
        {
            PlayerPrefs.DeleteKey(COMPLETED_PREFIX + tutorialId);
            PlayerPrefs.DeleteKey(PROGRESS_PREFIX + tutorialId);
            PlayerPrefs.Save();
        }
    }
}
