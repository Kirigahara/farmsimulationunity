using UnityEngine;
using GameTemplate.Core.DI;
using GameTemplate.Core.Audio;
using GameTemplate.Core.Mobile.Haptic;
using GameTemplate.Core.Mobile.Localization;
using GameTemplate.Core.UI;

namespace GameTemplate.Gameplay.UI.SettingsPanel
{
    /// <summary>
    /// SettingsPanel - kế thừa UIPanel của template để dùng được với UIManager stack.
    /// Đây là composition root cho Settings UI:
    ///   - Tạo Model + Presenter
    ///   - Gắn View
    ///   - Dispose khi panel close
    ///
    /// Cách dùng:
    ///   1. Tạo prefab với SettingsPanel.cs + SettingsView.cs gắn lên cùng GameObject
    ///   2. Reference _view trong Inspector
    ///   3. ServiceLocator.Get<UIManager>().Push(settingsPanel) để mở
    ///   4. UIManager.Pop() để đóng
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [SerializeField] private SettingsView _view;

        private SettingsModel _model;
        private SettingsPresenter _presenter;

        protected override void OnShow()
        {
            // Lazy init - chỉ tạo Model + Presenter khi panel hiện ra lần đầu
            if (_presenter == null)
            {
                _model = new SettingsModel(
                    ServiceLocator.Get<IAudioService>(),
                    ServiceLocator.Get<IHapticService>(),
                    ServiceLocator.Get<ILocalizationService>()
                );

                _presenter = new SettingsPresenter(_view, _model);
                _presenter.Init();
            }
        }

        // Khi panel destroy hoàn toàn (vd quit game), dispose cleanly
        private void OnDestroy()
        {
            _presenter?.Dispose();
            _model?.Dispose();
        }
    }
}
