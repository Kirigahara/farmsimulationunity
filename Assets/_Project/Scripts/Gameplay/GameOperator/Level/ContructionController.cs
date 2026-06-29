using GameTemplate.Core.Patterns.Async;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ContructionController : MonoBehaviour
    {
        [SerializeField] ContructionData _Data;
        [SerializeField] GameObject[] _LevelContruction;
        [SerializeField] Transform _GroupGrowPoint;
        [SerializeField] FarmerBehavior _MainFarmer;
        [SerializeField] PathNode _StandFarmer;

        Queue<GuestBehavior> _QueueGuest;
        List<ProductionController> _ListProduct = new List<ProductionController>();

        public string ProductID => _Data.Id;
        public FarmerBehavior MainFarmer => _MainFarmer;
        public PathNode StandFarmer => _StandFarmer;
        public PlantSaveData SaveData => GameplayManager.CurrentLevelSaveData.GetPlant(_Data.Id);
        public int GuestCount => _QueueGuest.Count;
        public int PlantLevel => SaveData.PlantLevel;

        private void Start()
        {
            ShowContruction();
        }

        /// <summary>
        /// Hiện hình dạng contruction theo level trong save data
        /// </summary>
        public void ShowContruction()
        {
            _LevelContruction[PlantLevel].gameObject.SetActive(true);
            if (PlantLevel > 0)
            {
                GrowFruit();
                _MainFarmer.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Gọi Farmer khi thêm khách vào Queue của công trình này
        /// </summary>
        public void CallFarmer()
        {
            if (!_MainFarmer.IsWaiting) return;
            if (_ListProduct.Count < _GroupGrowPoint.childCount) return;

            var guest = _QueueGuest.Peek();// Peek thay vì Dequeue — Farmer tự lấy sau
            _MainFarmer.ServeGuest(guest);
        }

        /// <summary>
        /// Thêm khách vào Queue khi tới quầy
        /// </summary>
        /// <param name="guest"></param>
        public async Task AddGuest(GuestBehavior guest)
        {
            if (_QueueGuest == null) { _QueueGuest = new Queue<GuestBehavior>(); }

            _QueueGuest.Enqueue(guest);

            await AsyncOp.Delay(Time.fixedDeltaTime); // cố ý Delay 1 khoản fixedDeltaTime để đảm bảo Guest đã chắc chắn vào Queue (Just in case) 

            CallFarmer();
        }

        /// <summary>
        /// Farmer sẽ gọi vào đây để remove khách vừa được phục vụ xong
        /// </summary>
        /// <returns></returns>
        public void RemoveGuest() { if (_QueueGuest.Count > 0) _QueueGuest.Dequeue(); }

        /// <summary>
        /// Farmer sẽ gọi vào đây để check xem còn khách nào đang đợi không
        /// </summary>
        /// <param name="EmptyGuest">Callback nếu không có khách </param>
        /// <returns></returns>
        public async Task CheckGuest(Action EmptyGuest)
        {
            await AsyncOp.Delay(Time.fixedDeltaTime); // cố ý Delay 1 khoản fixedDeltaTime để đảm bảo Queue ổn định sau khi Dequeue (Just in case) 

            if (_QueueGuest.Count > 0)
            {
                CallFarmer();
            }
            else
            {
                EmptyGuest?.Invoke();
            }
        }

        async void GrowFruit()
        {
            //Delay 1 khoảng thời gian để cây có quả
            await AsyncOp.Delay(_Data.GrowTime);

            for (int i = 0; i < _GroupGrowPoint.childCount; i++)
            {
                _ListProduct.Add(
                    GameplayManager.MainLevel.SpawnProducion(
                        _Data.Id,
                        _GroupGrowPoint.GetChild(i).position));

                await AsyncOp.Delay(GameConfig.ProductGrowDelay);
            }

            _ = CheckGuest(() => { });
        }

        /// <summary>
        /// Farmer lấy quả
        /// </summary>
        /// <param name="GroupPosition"></param>
        /// <param name="FetchComplete"></param>
        public async void FetchFruit(
            Transform GroupPosition, 
            Action<List<ProductionController>> FetchComplete)
        {
            for (int i = 0; i < _ListProduct.Count; i++)
            {
                _ListProduct[i].MoveSequence(GroupPosition.GetChild(i));
                await AsyncOp.Delay(GameConfig.ProductGetTime);
            }

            await AsyncOp.Delay(GameConfig.ProductMoveTime);

            FetchComplete?.Invoke(_ListProduct);

            await AsyncOp.Delay(Time.fixedDeltaTime);

            _ListProduct = new List<ProductionController>();
            GrowFruit();
        }
    }
}
