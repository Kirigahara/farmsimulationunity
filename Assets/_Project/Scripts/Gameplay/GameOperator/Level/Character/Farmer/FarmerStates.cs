using UnityEngine;

namespace GameTemplate.Gameplay
{
    // ─────────────────────────────────────────────────────────────────────
    //  1. WaitState
    //     Đứng cạnh cây, chờ LevelController push khách tới
    //     LevelController gọi StartServing(guest) khi có khách mới
    // ─────────────────────────────────────────────────────────────────────
    public class WaitState : ICharacterState
    {
        private readonly FarmerContext _ctx;

        public WaitState(FarmerContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Debug.Log("[WaitState] Farmer đang chờ khách.");
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// LevelController gọi hàm này khi có khách mới cần phục vụ.
        /// </summary>
        public void StartServing(GuestBehavior guest)
        {
            _ctx.CurrentGuest = guest;
            _ctx.GuestPosition = () => guest.transform.position;
            _ctx.StateMachine.ChangeState(new FetchFromTreeState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. FetchFromTreeState
    //     Gọi TreeBehavior lấy items, khi xong chuyển MoveToGuestState
    // ─────────────────────────────────────────────────────────────────────
    public class FetchFromTreeState : ICharacterState
    {
        private readonly FarmerContext _ctx;

        public FetchFromTreeState(FarmerContext ctx) => _ctx = ctx;

        public void Enter()
        {
            //_ctx.TreeBehavior.FetchItem(OnFetched);
        }

        public void Execute() { }

        public void Exit() { }

        private void OnFetched(Transform[] items)
        {
            _ctx.FetchedItems = items;
            _ctx.StateMachine.ChangeState(new MoveToGuestState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. MoveToGuestState
    //     Tìm đường tới vị trí Guest, khi tới nơi chuyển ReceivePaymentState
    // ─────────────────────────────────────────────────────────────────────
    public class MoveToGuestState : ICharacterState
    {
        private readonly FarmerContext _ctx;

        public MoveToGuestState(FarmerContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Vector3 destination = _ctx.GuestPosition.Invoke();
            _ctx.PathfindingService.FindPath(destination, OnReached);
        }

        public void Execute() { }

        public void Exit() { }

        private void OnReached()
        {
            _ctx.StateMachine.ChangeState(new ReceivePaymentState(_ctx));
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4. ReceivePaymentState
    //     Deliver item cho Guest, trigger Guest chuyển state, nhận tiền
    // ─────────────────────────────────────────────────────────────────────
    public class ReceivePaymentState : ICharacterState
    {
        private readonly FarmerContext _ctx;

        public ReceivePaymentState(FarmerContext ctx) => _ctx = ctx;

        public void Enter()
        {
            // Trigger Guest chuyển sang MoveToDespawnState
            _ctx.CurrentGuest.OnItemDelivered();

            // TODO: animation nhận tiền, sound...

            _ctx.StateMachine.ChangeState(new MoveToTreeState(_ctx));
        }

        public void Execute() { }

        public void Exit() { }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5. MoveToTreeState
    //     Quay về vị trí cạnh cây, khi tới nơi check queue từ LevelController
    // ─────────────────────────────────────────────────────────────────────
    public class MoveToTreeState : ICharacterState
    {
        private readonly FarmerContext _ctx;

        public MoveToTreeState(FarmerContext ctx) => _ctx = ctx;

        public void Enter()
        {
            Vector3 destination = _ctx.TreePosition.Invoke();
            _ctx.PathfindingService.FindPath(destination, OnReached);
        }

        public void Execute() { }

        public void Exit() { }

        private void OnReached()
        {
            // Reset guest hiện tại
            _ctx.CurrentGuest = null;
            _ctx.GuestPosition = null;
            _ctx.FetchedItems = null;

            // Hỏi LevelController xem có khách đang đợi không
            GuestBehavior nextGuest = _ctx.ContructionBehavior.GetGuest();

            if (nextGuest != null)
            {
                // Có khách trong queue → phục vụ luôn, không cần qua WaitState
                _ctx.CurrentGuest = nextGuest;
                _ctx.GuestPosition = () => nextGuest.transform.position;
                _ctx.StateMachine.ChangeState(new FetchFromTreeState(_ctx));
            }
            else
            {
                // Không có khách → về WaitState chờ LevelController push
                _ctx.StateMachine.ChangeState(new WaitState(_ctx));
            }
        }
    }
}
