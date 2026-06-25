using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameTemplate.Core.Events;
using GameTemplate.Core.Logger;

namespace GameTemplate.Core.SceneManagement
{
    public interface ISceneLoader
    {
        Task LoadSceneAsync(string sceneName, bool showLoading = true);
        float Progress { get; }
    }

    /// <summary>
    /// Scene Loader async, publish event để UI loading screen subscribe.
    /// Không tự tạo loading UI - tách trách nhiệm: UI tự lắng nghe event và hiển thị.
    /// </summary>
    public class SceneLoader : ISceneLoader
    {
        public float Progress { get; private set; }

        public async Task LoadSceneAsync(string sceneName, bool showLoading = true)
        {
            GameLog.Info(LogCategory.Scene, $"Loading scene: {sceneName}");

            if (showLoading)
                EventBus.Publish(new SceneLoadStartedEvent { SceneName = sceneName });

            Progress = 0f;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            // Unity load đến 0.9 thì pause, đợi allowSceneActivation = true
            while (op.progress < 0.9f)
            {
                Progress = op.progress;
                await Task.Yield();
            }

            Progress = 1f;
            op.allowSceneActivation = true;

            // Đợi activate xong
            while (!op.isDone)
                await Task.Yield();

            GameLog.Info(LogCategory.Scene, $"Loaded scene: {sceneName}");
            EventBus.Publish(new SceneLoadCompletedEvent { SceneName = sceneName });
        }
    }
}
