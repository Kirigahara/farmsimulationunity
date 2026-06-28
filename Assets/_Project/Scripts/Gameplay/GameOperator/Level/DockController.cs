using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class DockController : MonoBehaviour
    {
        [SerializeField] Transform _DeliveryPoint;
        [SerializeField] Transform _GuestStandPoint;
        [SerializeField] Transform _CurrencyPoint;

        public Transform DeliveryPoint => _DeliveryPoint;
        public Transform GuestStandPoint => _GuestStandPoint;
        public Transform CurrencyPoint => _CurrencyPoint;
    }
}
