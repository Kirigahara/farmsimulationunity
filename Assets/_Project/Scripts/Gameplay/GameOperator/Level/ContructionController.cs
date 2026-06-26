using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ContructionController : MonoBehaviour
    {
        [SerializeField] GameObject[] _LevelContruction;
        [SerializeField] GameTemplate.Core.EnumManager.ProductType _ProductType;
        [SerializeField] FarmerBehavior _MainFarmer;

        Queue<GuestBehavior> _QueueGuest;

        public GameTemplate.Core.EnumManager.ProductType ProductType => _ProductType;
        public FarmerBehavior MainFarmer => _MainFarmer;

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

        public void CallFarmer()
        {
            if (_MainFarmer.IsWaiting == false) return;
        }

        public void AddGuest(GuestBehavior guest)
        {
            if (_QueueGuest == null) { _QueueGuest = new Queue<GuestBehavior>(); }

            _QueueGuest.Enqueue(guest);
        }

        public GuestBehavior GetGuest() => _QueueGuest.Count == 0 ? null : _QueueGuest.Dequeue();
    }
}
