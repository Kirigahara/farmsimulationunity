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
        [SerializeField] GameTemplate.Core.EnumManager.ProductType _ProductType;
        [SerializeField] FarmerBehavior _MainFarmer;
        [SerializeField] PathNode _StandFarmer;

        Queue<GuestBehavior> _QueueGuest;

        public GameTemplate.Core.EnumManager.ProductType ProductType => _ProductType;
        public FarmerBehavior MainFarmer => _MainFarmer;
        public PathNode StandFarmer => _StandFarmer;

        private void Start()
        {
            ShowContruction();
        }

        /// <summary>
        /// Hiện hình dạng contruction theo level trong save data
        /// </summary>
        public void ShowContruction()
        {

        }

        /// <summary>
        /// Gọi Farmer khi thêm khách vào Queue của công trình này
        /// </summary>
        public void CallFarmer()
        {
            if (!_MainFarmer.IsWaiting) return;
            var guest = _QueueGuest.Peek();// Peek thay vì Dequeue — Farmer tự lấy sau
            _MainFarmer.ServeGuest(guest);
        }

        /// <summary>
        /// 
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
                EmptyGuest.Invoke();
            }
        }
    }
}
