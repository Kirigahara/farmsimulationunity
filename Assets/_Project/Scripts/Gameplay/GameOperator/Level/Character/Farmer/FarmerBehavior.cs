using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class FarmerBehavior : MonoBehaviour, ICharacterBehavior
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private PathFinding _pathfindingService;
        [SerializeField] private ContructionController _ContructionController;
        [SerializeField] private LevelController _levelController;

        // ── Runtime ───────────────────────────────────────────────────────
        private CharacterStateMachine _stateMachine;
        private FarmerContext _context;
        private WaitState _waitState;

        // ─────────────────────────────────────────────────────────────────
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
            _stateMachine = new CharacterStateMachine();

            _context = new FarmerContext(
                _pathfindingService,
                _stateMachine,
                _ContructionController,
                _levelController)
            {
                TreePosition = () => _ContructionController.transform.position
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
    }
}
