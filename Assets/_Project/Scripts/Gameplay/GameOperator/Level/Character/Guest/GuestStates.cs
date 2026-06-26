using UnityEngine;

namespace GameTemplate.Gameplay
{
    // ─────────────────────────────────────────────────────────────────────
    //  1. MoveToCounterState
    //     Gọi pathfinding tới quầy, khi tới nơi chuyển sang BuyingState
    // ─────────────────────────────────────────────────────────────────────
    public class MoveToCounterState : ICharacterState
    {
        private readonly GuestContext _ctx;

        public MoveToCounterState(GuestContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Vector3 destination = _ctx.CounterPosition.Invoke();

            _ctx.PathfindingService.FindPath(destination, OnReached);
        }

        public void Execute() { }

        public void Exit() { }

        private void OnReached()
        {
            _ctx.StateMachine.ChangeState(new BuyingState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. BuyingState
    //     Đứng chờ Farmer đem đồ tới, khi nhận hàng thì chuyển MoveToDespawnState
    //     Farmer sẽ gọi CompleteTransaction() từ bên ngoài
    // ─────────────────────────────────────────────────────────────────────
    public class BuyingState : ICharacterState
    {
        private readonly GuestContext _ctx;

        public BuyingState(GuestContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Debug.Log($"[BuyingState] Guest đang chờ item: {_ctx.ItemToBuy}");
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// Farmer gọi hàm này khi đã deliver item thành công.
        /// </summary>
        public void CompleteTransaction()
        {
            _ctx.StateMachine.ChangeState(new MoveToDespawnState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. MoveToDespawnState
    //     Gọi pathfinding tới điểm despawn, khi tới nơi chuyển DespawnState
    // ─────────────────────────────────────────────────────────────────────
    public class MoveToDespawnState : ICharacterState
    {
        private readonly GuestContext _ctx;

        public MoveToDespawnState(GuestContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Vector3 destination = _ctx.DespawnPosition.Invoke();

            _ctx.PathfindingService.FindPath(destination, OnReached);
        }

        public void Execute() { }

        public void Exit() { }

        private void OnReached()
        {
            _ctx.StateMachine.ChangeState(new DespawnState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4. DespawnState
    //     Destroy GameObject, có thể publish event cho GameManager biết
    // ─────────────────────────────────────────────────────────────────────
    public class DespawnState : ICharacterState
    {
        private readonly GuestContext _ctx;

        public DespawnState(GuestContext ctx) => _ctx = ctx;

        public void Enter()
        {
            // Nếu cần thông báo ra ngoài thì publish EventBus ở đây
            // EventBus.Publish(new GuestDespawnedEvent { Item = _ctx.ItemToBuy });
        }

        public void Execute() { }

        public void Exit() { }
    }
}
