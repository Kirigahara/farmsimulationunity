using GameTemplate.Core.Patterns.Factory;
using GameTemplate.Gameplay.Stats;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class FarmerBehavior : ChatacterBehavior, ICharacterBehavior, IConfigurable<FarmerData>
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("References")]
        private PathFollower _PathFollower;
        private ContructionController _ContructionController;
        private LevelController _levelController;

        // ── Runtime ───────────────────────────────────────────────────────
        private CharacterStateMachine _stateMachine;
        private FarmerContext _context;
        private WaitState _waitState;

        // ─────────────────────────────────────────────────────────────────
        [SerializeField] Quaternion _DefaultRotation;

        void Awake()
        {
            
        }

        void Start()
        {
            // Đăng ký với LevelController để nhận khách mới
            //_levelController.RegisterFarmer(this, _treeBehavior);
        }

        public void SetUp()
        {
            PlayIdle();

            Configure((FarmerData)_DefaultData);
            _stateMachine = new CharacterStateMachine();
            _PathFollower = new PathFollower();

            _context = new FarmerContext(
                _PathFollower,
                _stateMachine,
                _ContructionController,
                _levelController)
            {
                Coordinate = () => this.transform.position,
                TreePosition = () => _ContructionController.transform.position,
                UpdatePosition = UpdatePosition,
                UpdateRotation = UpdateRotation,
                OnReachedCounter = OnReachedCounter,
                ResetTransform = ResetTransform,
                PlayIdle = PlayIdle,
                PlayMove = PlayMove,
                PlayIdleCarry = PlayIdleCarry,
                PlayMoveCarry = PlayMoveCarry,
                ProductGroup = _ProductGroup,
                Stat = _CharacterStat,
                CurrentNode = _CurrentPathNode,
            };

            _waitState = new WaitState(_context);
            _stateMachine.ChangeState(_waitState);
        }

        void Update()
        {
            _stateMachine.Update();
        }

        void OnDestroy()
        {
            //_levelController.UnregisterFarmer(this, _treeBehavior);
        }

        // ── ICharacterBehavior ────────────────────────────────────────────
        public void OnReachedCounter()
        {
            // Farmer không tới quầy thanh toán nên để trống
        }

        // ── Public API cho LevelController gọi khi có khách mới ──────────
        public bool IsWaiting => _stateMachine.CurrentState is WaitState;

        public void ServeGuest(GuestBehavior guest)
        {
            if (_stateMachine.CurrentState is WaitState waitState)
                waitState.StartServing(guest);
            else
                Debug.LogWarning("[FarmerBehavior] ServeGuest gọi khi Farmer không ở WaitState!");
        }

        //---Data & Buff---------------
        public void Configure(FarmerData data)
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


        //--------
        void ResetTransform()
        {
            this.transform.rotation = _DefaultRotation;
        }
#if UNITY_EDITOR
        [ContextMenu(nameof(SetDefaultRotation))]
        public void SetDefaultRotation()
        {
            _DefaultRotation = this.transform.rotation;
        }
#endif
    }
}
