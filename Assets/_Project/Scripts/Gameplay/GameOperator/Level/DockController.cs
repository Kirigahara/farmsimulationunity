using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class DockController : MonoBehaviour
    {
        [SerializeField] PathNode _DeliveryPoint;
        [SerializeField] PathNode _GuestStandPoint;
        [SerializeField] Transform _CurrencyPoint;

        GuestBehavior _CurrentGuest;

        public PathNode DeliveryPoint => _DeliveryPoint;
        public PathNode GuestStandPoint => _GuestStandPoint;
        public Transform CurrencyPoint => _CurrencyPoint;
        public GuestBehavior CurrentGuest => _CurrentGuest;

        public void SetGuest(GuestBehavior guest)
        {
            _CurrentGuest = guest;
        }
    }
}
