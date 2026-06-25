using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Core.UI
{
    /// <summary>
    /// UI Manager stack-based: push panel mới đè lên panel cũ, pop để quay lại.
    /// Xử lý nút Back của Android tự động.
    ///
    /// Lưu ý:
    ///   - File này phải tên đúng "UIManager.cs" để Unity Add Component search được.
    ///   - Đặt component này lên Canvas root, không lên panel con.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private readonly Stack<UIPanel> _stack = new Stack<UIPanel>();

        public UIPanel CurrentPanel => _stack.Count > 0 ? _stack.Peek() : null;

        public void Push(UIPanel panel)
        {
            if (panel == null) return;

            if (_stack.Count > 0)
                _stack.Peek().Hide();

            _stack.Push(panel);
            panel.Show();
        }

        public void Pop()
        {
            if (_stack.Count == 0) return;

            var top = _stack.Pop();
            top.Hide();

            if (_stack.Count > 0)
                _stack.Peek().Show();
        }

        public void PopAll()
        {
            while (_stack.Count > 0)
                _stack.Pop().Hide();
        }

        private void Update()
        {
            // Android back button
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (_stack.Count == 0) return;
                var top = _stack.Peek();
                if (!top.OnBackPressed())
                    Pop();
            }
        }
    }
}
