using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class GuestBehavior : MonoBehaviour, ICharacterBehavior
    {
        // ── Inspector ─────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField] private PathFinding _pathfindingService;
        [SerializeField] private Transform _counterTransform;
        [SerializeField] private Transform _despawnTransform;

        // ── Runtime ───────────────────────────────────────────────────────
        private CharacterStateMachine _stateMachine;
        private GuestContext _context;
        private BuyingState _buyingState;

        // ─────────────────────────────────────────────────────────────────
        void Awake()
        {
            //_stateMachine = new CharacterStateMachine();
        }

        void Start()
        {
            
        }

        public void SetupItem(
            PathFinding pathfindingService, 
            CharacterStateMachine stateMachine,
            ContructionController contruction)
        {
            _stateMachine = new CharacterStateMachine();

            _context = new GuestContext(_pathfindingService, _stateMachine)
            {
                CounterPosition = () => _counterTransform.position,
                DespawnPosition = () => _despawnTransform.position,
                ItemToBuy = contruction,
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

            _context.ItemToBuy.CallFarmer();
        }

        // ── Public API cho Farmer gọi khi deliver xong ───────────────────
        public void OnItemDelivered()
        {
            if (_stateMachine.CurrentState is BuyingState buyingState)
                buyingState.CompleteTransaction();
            else
                Debug.LogWarning("[GuestBehavior] OnItemDelivered gọi sai state!");
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private string DecideItem()
        {
            // Logic chọn item mua — mở rộng sau
            return "Apple";
        }
    }
}
