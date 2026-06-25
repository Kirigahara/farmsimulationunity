using UnityEngine;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// DefaultCharacterStats - chỉ số GỐC của character do designer thiết kế.
    ///
    /// Đặc điểm:
    ///   - Là ScriptableObject (asset trong Project) - chỉnh trong Editor, không sửa runtime
    ///   - IMMUTABLE - mọi field readonly hoặc serialized private setter
    ///   - 1 asset = 1 character class (Warrior, Mage, Archer...)
    ///   - Có thể edit bằng tay hoặc import từ Google Sheets/Excel
    ///
    /// Workflow:
    ///   - Designer: chỉnh balance trong Editor, không cần code
    ///   - Save game KHÔNG chứa thông tin này - chỉ chứa "id của default đã chọn"
    ///   - Update game: thay default values trong patch, player nhận stat mới
    ///
    /// Tạo asset: Right-click Project → Create → Game → Character → Default Stats
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Character/Default Stats", fileName = "DefaultStats_")]
    public class DefaultCharacterStats : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] private string _characterId = "warrior";
        [SerializeField] private string _displayName = "Warrior";

        [Header("Base Stats - Level 1")]
        [SerializeField] private int _baseHp = 100;
        [SerializeField] private int _baseAttack = 10;
        [SerializeField] private int _baseDefense = 5;
        [SerializeField] private float _baseMoveSpeed = 5f;
        [SerializeField] private int _baseCritRate = 5; // percent

        [Header("Level-up Increment")]
        [SerializeField] private int _hpPerLevel = 20;
        [SerializeField] private int _attackPerLevel = 3;
        [SerializeField] private int _defensePerLevel = 2;

        [Header("Caps")]
        [SerializeField] private int _maxLevel = 100;
        [SerializeField] private int _maxCritRate = 75; // crit rate không vượt 75%

        // ===== Read-only properties =====
        public string CharacterId => _characterId;
        public string DisplayName => _displayName;
        public int BaseHp => _baseHp;
        public int BaseAttack => _baseAttack;
        public int BaseDefense => _baseDefense;
        public float BaseMoveSpeed => _baseMoveSpeed;
        public int BaseCritRate => _baseCritRate;
        public int HpPerLevel => _hpPerLevel;
        public int AttackPerLevel => _attackPerLevel;
        public int DefensePerLevel => _defensePerLevel;
        public int MaxLevel => _maxLevel;
        public int MaxCritRate => _maxCritRate;
    }
}
