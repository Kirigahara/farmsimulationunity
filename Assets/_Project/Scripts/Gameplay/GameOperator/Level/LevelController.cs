using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] List<ContructionController> _ListContruction;
        
        public List<ContructionController> ListContruction => _ListContruction;

    }
}
