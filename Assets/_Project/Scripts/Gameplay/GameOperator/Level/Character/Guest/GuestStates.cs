using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Async;
using System;
using System.Threading.Tasks;
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
            //Vector3 destination = _ctx.CounterPosition.Invoke();
            
            PathSmoother.FindPath(
                _ctx.Coordinate.Invoke(),
                _ctx.CurrentNode,
                _ctx.FinishNode, (path) => 
                {
                    //for (int i = 0; i < path.Count - 1; i++)
                    //{
                    //    Debug.DrawLine(path[i], path[i + 1], Color.blue, 20.0f);
                    //}

                    _ctx.PathFollower.SetPath(path); 
                });
        }

        public void Execute() 
        {
            if (_ctx.PathFollower._PathExist == false) return;

            if (_ctx.PathFollower.IsFinished)
            {
                _ctx.PathFollower.EmptyPath();
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
            _ctx.OnReachedCounter.Invoke();
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
            _ctx.PlayIdle.Invoke();
            Debug.Log($"[BuyingState] Guest đang chờ item: {_ctx.ItemToBuy}");
        }

        public void Execute() { }

        public void Exit() { }

        /// <summary>
        /// Farmer gọi hàm này khi đã deliver item thành công.
        /// </summary>
        public void CompleteTransaction()
        {
            GameplayManager.PlayerDataRuntime.UpGold(
                GameplayManager.GetFruitSalePrice(_ctx.ItemToBuy.ProductID));

            _ctx.StateMachine.ChangeState(new MoveToDespawnState(_ctx));
        }

        public async Task MakeFetch(
            System.Collections.Generic.List<ProductionController> listFruit,
            Action FetchComplete)
        {
            _ctx.Productions = new System.Collections.Generic.List<ProductionController>();

            for(int i = 0; i < listFruit.Count; i++)
            {
                listFruit[i].MoveSequence(_ctx.ProductGroup.GetChild(i));
                await AsyncOp.Delay(GameConfig.ProductGetTime);
                _ctx.Productions.Add(listFruit[i]);
            }

            await AsyncOp.Delay(GameConfig.ProductMoveTime);

            FetchComplete?.Invoke();
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
            //Vector3 destination = _ctx.DespawnPosition.Invoke();

            _ctx.DockController.EmptyGuest();

            PathSmoother.FindPath(
                _ctx.Coordinate.Invoke(),
                _ctx.CurrentNode,
                GameplayManager.MainLevel.ExitGuestNode,
                (path) => { _ctx.PathFollower.SetPath(path); });
        }

        public void Execute() 
        {
            if (_ctx.PathFollower._PathExist == false) return;

            if (_ctx.PathFollower.IsFinished)
            {
                _ctx.PathFollower.EmptyPath();
                _ctx.CurrentNode = null;
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

            _ctx.PlayIdle.Invoke();
            _ctx.DeSpawn.Invoke();
        }

        public void Execute() { }

        public void Exit() { }
    }
}
