using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class ContructionController : MonoBehaviour
    {
        [SerializeField] GameObject[] _LevelContruction;
        

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
    }
}
