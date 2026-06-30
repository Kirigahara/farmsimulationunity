using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Factory;
using GameTemplate.Gameplay.Stats;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GuestBehavior : ChatacterBehavior, ICharacterBehavior, IConfigurable<GuestData>
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("References")]
        private PathFollower _PathFollower;
        private Transform _counterTransform;
        private Transform _despawnTransform;

        // ── Runtime ───────────────────────────────────────────────────────
        private CharacterStateMachine _stateMachine;
        private GuestContext _context;
        private BuyingState _buyingState;

        // ─────────────────────────────────────────────────────────────────

        public GuestContext Context=> _context;

        void Awake()
        {
            //_stateMachine = new CharacterStateMachine();
        }

        void Start()
        {
            
        }

        public void Setup( 
            ContructionController contruction,
            PathNode startNode, DockController dock)
        {
            _stateMachine = new CharacterStateMachine();

            PlayIdle();

            Configure((GuestData)_DefaultData);
            _stateMachine = new CharacterStateMachine();
            _PathFollower = new PathFollower();

            _context = new GuestContext(_PathFollower, _stateMachine)
            {
                CounterPosition = () => _counterTransform.position,
                DespawnPosition = () => _despawnTransform.position,
                Coordinate = () => this.transform.position,
                UpdatePosition = UpdatePosition,
                UpdateRotation = UpdateRotation,
                OnReachedCounter = OnReachedCounter,
                DeSpawn = Despawn,
                PlayIdle = PlayIdle,
                PlayMove = PlayMove,
                PlayIdleCarry = PlayIdleCarry,
                PlayMoveCarry = PlayMoveCarry,
                ProductGroup = _ProductGroup,
                ItemToBuy = contruction,
                Stat = _CharacterStat,
                CurrentNode = startNode,
                FinishNode = dock.GuestStandPoint,
                DockController = dock
            };

            _stateMachine.ChangeState(new MoveToCounterState(_context));
        }

        void Update()
        {
            _stateMachine.Update();
        }

        // ── ICharacterBehavior ────────────────────────────────────────────
        public void OnReachedCounter()
        {
            // CharacterController gọi hàm này khi tới quầy
            // BuyingState đã được chuyển tự động qua pathfinding callback
            // Hàm này có thể dùng để trigger animation hoặc sound

            _ = _context.ItemToBuy.AddGuest(this);
        }

        // ── Public API cho Farmer gọi khi deliver xong ───────────────────
        public void OnItemDelivered(
            System.Collections.Generic.List<ProductionController> products,
            System.Action deliverComplete)
        {
            if (_stateMachine.CurrentState is BuyingState buyingState)
            {
                _ = buyingState.MakeFetch(
                    products,
                    () =>
                    {
                        deliverComplete?.Invoke();
                        buyingState.CompleteTransaction();
                    });
            }
            else
            {
                Debug.LogWarning("[GuestBehavior] OnItemDelivered gọi sai state!");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private string DecideItem()
        {
            // Logic chọn item mua — mở rộng sau
            return "Apple";
        }

        //---Data & Buff---------------
        public void Configure(GuestData data)
        {
            //_MoveSpeed = data.MoveSpeed;

            _CharacterStat = new CharacterStat2
            {
                _MoveSpeed = data.MoveSpeed,
            };
        }

        public void ApplyBuff(BuffDefinition buff)
        {
            _CharacterStat.BuffSet.Apply(buff);
        }
        
        //-----
        void Despawn()
        {
            EventBus.Publish(new GuestExit { _Guest = this });
        }
    }
}
