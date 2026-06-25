using UnityEngine;

namespace GameTemplate.Core.Patterns.MVP
{
    /// <summary>
    /// MVP cho Unity UI - tách 3 phần:
    ///   - Model: data thuần (ScriptableObject hoặc class), không biết gì về Unity
    ///   - View: MonoBehaviour, chỉ làm việc với UI component (Text, Image, Button), KHÔNG có logic
    ///   - Presenter: pure C# class, cầu nối Model <-> View, chứa toàn bộ logic
    ///
    /// Lợi ích cho team:
    ///   - Designer sửa View không sợ phá logic (Presenter)
    ///   - Test logic không cần Unity Editor (Presenter là pure C#)
    ///   - Đổi UI hoàn toàn mà không đụng Presenter (vd reskin)
    ///
    /// Khi nào dùng:
    ///   - HUD đơn giản (1-2 field) -> không cần MVP, overkill
    ///   - Inventory, shop, settings, quest log -> NÊN dùng MVP
    ///   - RPG có nhiều panel phức tạp -> phải dùng MVP
    /// </summary>
    public abstract class ViewBase : MonoBehaviour
    {
        /// <summary>View không tự khởi tạo Presenter - Presenter inject vào View.</summary>
        public virtual void Bind() { }
        public virtual void Unbind() { }
    }

    /// <summary>
    /// Presenter base. Generic theo View và Model để type-safe.
    /// Lifecycle: ctor -> Init() -> ... -> Dispose()
    /// </summary>
    public abstract class PresenterBase<TView, TModel>
        where TView : ViewBase
        where TModel : class
    {
        protected readonly TView View;
        protected readonly TModel Model;

        protected PresenterBase(TView view, TModel model)
        {
            View = view;
            Model = model;
        }

        /// <summary>Gọi sau khi tạo presenter - subscribe event, init UI.</summary>
        public virtual void Init()
        {
            View.Bind();
            OnInit();
        }

        /// <summary>Cleanup - unsub event, hủy reference.</summary>
        public virtual void Dispose()
        {
            OnDispose();
            View.Unbind();
        }

        protected virtual void OnInit() { }
        protected virtual void OnDispose() { }
    }
}
