using System;

namespace GameTemplate.Core.Tutorial
{
    /// <summary>
    /// 1 bước tutorial. Engine (TutorialSequencer) chỉ biết 3 method này,
    /// không quan tâm step làm gì cụ thể - đó là phần game-specific.
    ///
    /// Lifecycle:
    ///   1. Enter()      - bắt đầu step: hiện highlight, text bubble, block input...
    ///   2. IsComplete() - gọi mỗi frame, return true khi điều kiện qua
    ///   3. Exit()       - cleanup: ẩn highlight, unblock input...
    ///
    /// Tự viết step game-specific bằng cách implement interface này,
    /// hoặc dùng các built-in step (WaitForClick, WaitForEvent, WaitForCondition, Message).
    /// </summary>
    public interface ITutorialStep
    {
        /// <summary>Id duy nhất của step - dùng để lưu tiến độ. Vd: "move_tutorial".</summary>
        string StepId { get; }

        /// <summary>Gọi 1 lần khi step bắt đầu.</summary>
        void Enter(TutorialContext context);

        /// <summary>Gọi mỗi frame. Return true = step xong, chuyển step tiếp theo.</summary>
        bool IsComplete();

        /// <summary>Gọi 1 lần khi step kết thúc (trước khi sang step tiếp).</summary>
        void Exit();
    }

    /// <summary>
    /// Context truyền vào mỗi step - cho step truy cập overlay UI + callback.
    /// Tách context để step không cần reference trực tiếp Sequencer.
    /// </summary>
    public class TutorialContext
    {
        /// <summary>Overlay UI để highlight, hiện text, block input.</summary>
        public ITutorialOverlay Overlay { get; }

        /// <summary>Gọi khi step muốn skip toàn bộ tutorial (vd: nút Skip).</summary>
        public Action RequestSkipAll { get; }

        public TutorialContext(ITutorialOverlay overlay, Action requestSkipAll)
        {
            Overlay = overlay;
            RequestSkipAll = requestSkipAll;
        }
    }
}
