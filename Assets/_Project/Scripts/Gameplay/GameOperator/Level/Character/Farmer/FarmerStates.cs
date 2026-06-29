using NUnit.Framework;
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
            _ctx.PlayIdle.Invoke();
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

            _ctx.ContructionBehavior.FetchFruit(
                _ctx.ProductGroup, OnFetched);
            _ctx.PlayIdleCarry.Invoke();
        }

        public void Execute() { }

        public void Exit() { }

        private void OnFetched(Transform[] items)
        {
            _ctx.FetchedItems = items;
            _ctx.StateMachine.ChangeState(new MoveToGuestState(_ctx));
        }

        private void OnFetched(System.Collections.Generic.List<ProductionController> listFruit)
        {
            _ctx.Productions = new System.Collections.Generic.List<ProductionController>(listFruit);
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
            //_ctx.PathfindingService.FindPath(destination, OnReached);

            PathSmoother.FindPath(
                _ctx.Coordinate.Invoke(),
                _ctx.CurrentNode,
                _ctx.FinishNode,
                (path) => { _ctx.PathFollower.SetPath(path); });
        }

        public void Execute() 
        {
            if (_ctx.PathFollower.IsFinished)
            {
                _ctx.CurrentNode = _ctx.FinishNode;
                OnReached();
                return;
            }

            _ctx.UpdateRotation.Invoke(
                Quaternion.LookRotation(_ctx.PathFollower.MoveDirection));
            _ctx.UpdatePosition.Invoke(
                _ctx.PathFollower.Tick(
                    _ctx.Coordinate.Invoke(), 
                    _ctx.Stat.MoveSpeed));
            _ctx.PlayMoveCarry.Invoke();
        }

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
            _ctx.CurrentGuest.OnItemDelivered(
                _ctx.Productions,
                () => 
                {
                    // TODO: animation nhận tiền, sound...
                    _ctx.StateMachine.ChangeState(new MoveToTreeState(_ctx));

                    // Remove khách sau khi trả tiền
                    _ctx.ContructionBehavior.RemoveGuest();

                    _ctx.Productions = new System.Collections.Generic.List<ProductionController>();
                });

            _ctx.PlayIdle.Invoke();
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

            //_ctx.PathfindingService.FindPath(destination, OnReached);

            PathSmoother.FindPath(
                _ctx.Coordinate.Invoke(),
                _ctx.CurrentNode,
                _ctx.ContructionBehavior.StandFarmer,
                (path) => { _ctx.PathFollower.SetPath(path); });
        }

        public void Execute() 
        {
            if (_ctx.PathFollower.IsFinished)
            {
                _ctx.CurrentNode = _ctx.FinishNode;
                OnReached();
                return;
            }

            _ctx.UpdateRotation.Invoke(
                Quaternion.LookRotation(_ctx.PathFollower.MoveDirection));
            _ctx.UpdatePosition.Invoke(
                _ctx.PathFollower.Tick(
                    _ctx.Coordinate.Invoke(),
                    _ctx.Stat.MoveSpeed));
            _ctx.PlayMove.Invoke();
        }

        public void Exit() { }

        private void OnReached()
        {
            // Reset guest hiện tại
            _ctx.CurrentGuest = null;
            _ctx.GuestPosition = null;
            _ctx.FetchedItems = null;

            // Hỏi LevelController xem có khách đang đợi không
            //GuestBehavior nextGuest = _ctx.ContructionBehavior.GetGuest();

            //if (nextGuest != null)
            //{
            //    // Có khách trong queue → phục vụ luôn, không cần qua WaitState
            //    _ctx.CurrentGuest = nextGuest;
            //    _ctx.GuestPosition = () => nextGuest.transform.position;
            //    _ctx.StateMachine.ChangeState(new FetchFromTreeState(_ctx));
            //}
            //else
            //{
            //    // Không có khách → về WaitState chờ LevelController push
            //    _ctx.StateMachine.ChangeState(new WaitState(_ctx));
            //}

            _ctx.ResetTransform.Invoke();

            _ = _ctx.ContructionBehavior.CheckGuest(
                () =>
                {
                    // Không có khách → về WaitState chờ LevelController push
                    _ctx.StateMachine.ChangeState(new WaitState(_ctx));
                });
        }
    }
}
