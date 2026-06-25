using UnityEngine;

namespace GameTemplate.Core.UI
{
    /// <summary>
    /// Base class cho mọi UI Panel. Mỗi panel là 1 prefab có script kế thừa UIPanel.
    /// UIManager quản lý stack panel: push, pop, back button mobile.
    ///
    /// Lưu ý: Class UIManager nằm trong file UIManager.cs riêng (cùng folder),
    /// </summary>
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] protected float _fadeDuration = 0.2f;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            OnShow();
        }

        public virtual void Hide()
        {
            IsVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            OnHide();
            gameObject.SetActive(false);
        }

        /// <summary>Override để xử lý khi panel hiện ra (vd: fetch data, play anim).</summary>
        protected virtual void OnShow() { }

        /// <summary>Override để cleanup khi panel ẩn đi.</summary>
        protected virtual void OnHide() { }

        /// <summary>Xử lý back button mobile (Android). Return true nếu đã handle.</summary>
        public virtual bool OnBackPressed() => false;
    }
}
