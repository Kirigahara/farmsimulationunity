using System;
using System.Collections.Generic;

namespace GameTemplate.Core.Patterns.Reactive
{
    /// <summary>
    /// Reactive Property - giá trị có thể observe.
    /// UI subscribe vào property này, mỗi khi value đổi thì callback chạy.
    /// Tiện cho RPG (HP, MP, XP, Score, Coins...) vì không phải nhớ "đổi xong rồi gọi UpdateUI()".
    ///
    /// Cách dùng:
    ///   // Trong PlayerStats
    ///   public ReactiveProperty<int> Hp = new ReactiveProperty<int>(100);
    ///
    ///   // Trong HpBarUI
    ///   private void OnEnable()
    ///   {
    ///       _player.Hp.Subscribe(OnHpChanged);
    ///       OnHpChanged(_player.Hp.Value); // init UI
    ///   }
    ///   private void OnDisable() => _player.Hp.Unsubscribe(OnHpChanged);
    ///   private void OnHpChanged(int hp) => _hpText.text = hp.ToString();
    ///
    ///   // Khi cần đổi
    ///   _player.Hp.Value = 80; // tự động gọi mọi subscriber
    /// </summary>
    public class ReactiveProperty<T>
    {
        private T _value;
        private readonly List<Action<T>> _subscribers = new List<Action<T>>(4);

        public ReactiveProperty(T initial = default)
        {
            _value = initial;
        }

        public T Value
        {
            get => _value;
            set
            {
                // EqualityComparer tránh notify khi gán giá trị y chang
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                _value = value;
                NotifyAll();
            }
        }

        /// <summary>Force set không notify - dùng khi load save, tránh trigger UI animation lúc khởi tạo.</summary>
        public void SetSilent(T value) => _value = value;

        /// <summary>Force notify dù value không đổi - dùng khi muốn refresh UI.</summary>
        public void ForceNotify() => NotifyAll();

        public void Subscribe(Action<T> callback)
        {
            if (callback == null) return;
            _subscribers.Add(callback);
        }

        /// <summary>Subscribe và gọi luôn callback với value hiện tại - tiện cho init UI.</summary>
        public void SubscribeWithInit(Action<T> callback)
        {
            if (callback == null) return;
            _subscribers.Add(callback);
            callback.Invoke(_value);
        }

        public void Unsubscribe(Action<T> callback)
        {
            _subscribers.Remove(callback);
        }

        public void ClearSubscribers() => _subscribers.Clear();

        private void NotifyAll()
        {
            int count = _subscribers.Count;
            for (int i = 0; i < count; i++)
            {
                try { _subscribers[i].Invoke(_value); }
                catch (Exception ex) { UnityEngine.Debug.LogError($"[ReactiveProperty] Subscriber crash: {ex}"); }
            }
        }

        // Implicit conversion: dùng như value thường được
        public static implicit operator T(ReactiveProperty<T> rp) => rp._value;
    }

    /// <summary>
    /// ReactiveCollection - list có thể observe Add/Remove/Clear.
    /// Tiện cho inventory, quest list, party member...
    /// </summary>
    public class ReactiveCollection<T>
    {
        private readonly List<T> _items = new List<T>();

        public event Action<T> OnAdd;
        public event Action<T> OnRemove;
        public event Action OnClear;
        public event Action OnChange; // bất kỳ thay đổi nào

        public int Count => _items.Count;
        public T this[int i] => _items[i];
        public IReadOnlyList<T> Items => _items;

        public void Add(T item)
        {
            _items.Add(item);
            OnAdd?.Invoke(item);
            OnChange?.Invoke();
        }

        public bool Remove(T item)
        {
            if (_items.Remove(item))
            {
                OnRemove?.Invoke(item);
                OnChange?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _items.Clear();
            OnClear?.Invoke();
            OnChange?.Invoke();
        }
    }
}
