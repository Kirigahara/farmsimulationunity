using UnityEngine;

namespace GameTemplate.Core.Tutorial
{
    /// <summary>
    /// Vị trí text bubble so với highlight target.
    /// </summary>
    public enum BubblePlacement
    {
        Auto,    // tự chọn dựa vào vị trí target trên màn hình
        Above,
        Below,
        Left,
        Right,
        Center,  // giữa màn hình (cho message không gắn target)
    }

    /// <summary>
    /// Interface cho overlay UI tutorial - tách interface để step không phụ thuộc
    /// implementation cụ thể (dễ test, dễ swap UI khác).
    ///
    /// Capabilities:
    ///   - Highlight: mask tối màn hình + khoét lỗ vào element cần chỉ
    ///   - Text bubble: hiện hướng dẫn, optional gắn vào target
    ///   - Block input: chặn click ngoài vùng highlight
    /// </summary>
    public interface ITutorialOverlay
    {
        /// <summary>Hiện overlay (mask tối). Gọi khi tutorial bắt đầu.</summary>
        void Show();

        /// <summary>Ẩn overlay hoàn toàn. Gọi khi tutorial kết thúc.</summary>
        void Hide();

        /// <summary>
        /// Highlight 1 UI element: khoét lỗ trong mask để lộ element, block input xung quanh.
        /// </summary>
        /// <param name="target">RectTransform của element cần highlight.</param>
        /// <param name="allowClickThrough">True = cho phép click vào element (vd: chờ user bấm nút đó).</param>
        /// <param name="padding">Lề thêm quanh element (pixel).</param>
        void HighlightTarget(RectTransform target, bool allowClickThrough = true, float padding = 10f);

        /// <summary>
        /// Highlight 1 vùng world space (vd: chỉ vào enemy, item trong scene).
        /// Tự convert world → screen position.
        /// </summary>
        void HighlightWorldPosition(Vector3 worldPosition, float screenRadius = 80f, bool allowClickThrough = false);

        /// <summary>Xóa highlight hiện tại (mask phủ kín lại).</summary>
        void ClearHighlight();

        /// <summary>
        /// Hiện text bubble hướng dẫn.
        /// </summary>
        /// <param name="message">Nội dung text.</param>
        /// <param name="placement">Vị trí so với highlight target.</param>
        /// <param name="showTapHint">Hiện gợi ý "Tap để tiếp tục".</param>
        void ShowMessage(string message, BubblePlacement placement = BubblePlacement.Auto, bool showTapHint = false);

        /// <summary>Ẩn text bubble.</summary>
        void HideMessage();

        /// <summary>
        /// True nếu user vừa tap vào overlay trong frame này (cho MessageStep dismiss).
        /// </summary>
        bool WasTappedThisFrame();

        /// <summary>
        /// True nếu user vừa tap đúng vào vùng highlight (cho WaitForClickStep).
        /// </summary>
        bool WasHighlightTappedThisFrame();
    }
}
