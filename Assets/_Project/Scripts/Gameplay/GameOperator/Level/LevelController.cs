using GameTemplate.Core.Data;
using GameTemplate.Core.DI;
using GameTemplate.Core.Events;
using GameTemplate.Core.Patterns.Async;
using GameTemplate.Core.Patterns.Factory;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public struct GuestExit : IGameEvent { public GuestBehavior _Guest; };

    public class LevelController : MonoBehaviour
    {
        [Header("Tree")]
        [SerializeField] List<ContructionController> _ListContruction;

        [Header("Counter Dock")]
        [SerializeField] List<DockController> _ListDock;

        [Header("Guest Node")]
        [SerializeField] PathNode _EnterGuestNode;
        [SerializeField] PathNode _ExitGuestNode;

        [Header("Touch Layer")]
        [SerializeField] private LayerMask _TargetLayer;

        PrefabFactory<GuestData, GuestBehavior> _GuestFactory;
        PrefabFactory<ContructionData, ProductionController> _ProductionFactory;

        public List<ContructionController> ListContruction => _ListContruction;
        public List<DockController> ListDock => _ListDock;
        public PathNode EnterGuestNode => _EnterGuestNode;
        public PathNode ExitGuestNode => _ExitGuestNode;

        int _CurrentGuestCount = 0;
        int _CurrentMaxGuestAmout = 
            (int)GameConfig.LevelConfigs[GameplayManager.PlayerDataService.PlayerData._Level].
            CustomerUpgrade.Levels[GameplayManager.CurrentLevelSaveData.CustomerUpgradeLevel].Value;

        public static LevelController CreateLevel(int level, Vector3 position)
        {
            return GameObject.Instantiate(
                GameConfig.LevelConfigs[level]._PrefabLevel,
                position, Quaternion.identity).GetComponent<LevelController>();
        }

        private void Awake()
        {
            _GuestFactory = new PrefabFactory<GuestData, GuestBehavior>(
                GameConfig.AllGuestData);
            _ProductionFactory = new PrefabFactory<ContructionData, ProductionController>(
                GameConfig.LevelConfigs[GameplayManager.PlayerDataService.PlayerData._Level].ContructionDatas);
        }

        private async void Start()
        {
            EventBus.Subscribe<GuestExit>(GuestExit);

            await AsyncOp.Delay(GameConfig.DelayStartGameplay);
            OpenGuest();
        }

        public GuestBehavior SpawnGuest()
        {
            return _GuestFactory.Create("0", _EnterGuestNode.GetRandomPosition());
        }
        public ProductionController SpawnProducion(string id, Vector3 Position)
        {
            return _ProductionFactory.Create(id, Position);
        }

        async void OpenGuest()
        {
            var guest = SpawnGuest();
            var contruction = GetEmptyContruction();
            var dock = GetEmptyDock();

            dock.SetGuest(guest);

            guest.Setup(
                contruction, _EnterGuestNode,
                dock.GuestStandPoint);

            await AsyncOp.Delay(GameConfig.DelaySpawnGuest);

            _CurrentGuestCount++;
            if (_CurrentGuestCount < _CurrentMaxGuestAmout)
            {
                OpenGuest();
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RaycastBox(
                    Camera.main.transform.position, Camera.main.transform.forward,
                    out RaycastHit hit, 100f, _TargetLayer))
                {
                    Debug.Log($"Hit box: {hit.collider.gameObject.name} tại {hit.point}");
                }
            }
        }

        /// <summary>
        /// Raycast từ origin theo direction, chỉ hit BoxCollider.
        /// </summary>
        /// <param name="origin">Điểm bắt đầu ray.</param>
        /// <param name="direction">Hướng bắn ray.</param>
        /// <param name="hit">Thông tin hit nếu có.</param>
        /// <param name="maxDistance">Khoảng cách tối đa.</param>
        /// <param name="layerMask">Layer filter — nên dùng để tối ưu performance.</param>
        public static bool RaycastBox(
            Vector3 origin,
            Vector3 direction,
            out RaycastHit hit,
            float maxDistance = Mathf.Infinity,
            int layerMask = Physics.DefaultRaycastLayers)
        {
            if (Physics.Raycast(origin, direction, out hit, maxDistance, layerMask))
            {
                if (hit.collider is BoxCollider)
                    return true;

                hit = default;
            }
            return false;
        }

        public DockController GetEmptyDock()
        {
            return StaticFunction.GetRandomList(_ListDock.FindAll(x => x.CurrentGuest == null));
        }

        public ContructionController GetEmptyContruction()
        {
            return StaticFunction.GetRandomList(_ListContruction.FindAll(x => x.PlantLevel > 0));
        }

        public void GuestExit(GuestExit e)
        {
            for (int i = 0; i < e._Guest.Context.Productions.Count; i++)
            {
                _ProductionFactory.Return(
                    e._Guest.Context.ItemToBuy.ProductID,
                    e._Guest.Context.Productions[i]);
            }
            _GuestFactory.Return("0", e._Guest);

            _CurrentGuestCount--;
            OpenGuest();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GuestExit>(GuestExit);
        }
    }
}
