using UnityEngine;

namespace GameTemplate.Core.UI.Buttons
{
    /// <summary>
    /// Interface để EnhancedButton lookup preset SFX.
    /// Tách interface để có thể mock trong test.
    /// </summary>
    public interface IUIButtonSfxLibrary
    {
        AudioClip GetClip(ButtonSfxPreset preset);
    }

    /// <summary>
    /// Library map từ preset enum sang AudioClip - 1 asset duy nhất cho cả project.
    ///
    /// Setup:
    ///   1. Create asset: Right-click Project → Create → Game → UI → Button SFX Library
    ///   2. Kéo 4 clip vào: Click, Confirm, Cancel, Error
    ///   3. Đăng ký vào ServiceLocator trong Bootstrap:
    ///      ServiceLocator.Register&lt;IUIButtonSfxLibrary&gt;(libraryAsset);
    ///   4. EnhancedButton tự dùng - không cần kéo clip vào từng nút
    ///
    /// Lợi ích so với kéo clip riêng mỗi button:
    ///   - Designer đổi 1 chỗ, áp dụng toàn game
    ///   - Tiết kiệm memory (1 instance shared)
    ///   - Đảm bảo audio consistency (mọi Confirm button cùng sound)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/UI/Button SFX Library", fileName = "UIButtonSfxLibrary")]
    public class UIButtonSfxLibrary : ScriptableObject, IUIButtonSfxLibrary
    {
        [Header("Preset SFX Clips")]
        [SerializeField] private AudioClip _click;
        [SerializeField] private AudioClip _confirm;
        [SerializeField] private AudioClip _cancel;
        [SerializeField] private AudioClip _error;

        public AudioClip GetClip(ButtonSfxPreset preset)
        {
            return preset switch
            {
                ButtonSfxPreset.Click   => _click,
                ButtonSfxPreset.Confirm => _confirm,
                ButtonSfxPreset.Cancel  => _cancel,
                ButtonSfxPreset.Error   => _error,
                _ => null,
            };
        }
    }
}
