using System.Threading.Tasks;
using GameTemplate.Core.Data;
using GameTemplate.Core.DI;

namespace GameTemplate.Gameplay
{
    /// <summary>
    /// Service quản lý PlayerData — nằm trong Gameplay.asmdef.
    /// Wrap DataManager (Core) để xử lý data cụ thể của player.
    ///
    /// Cách dùng:
    ///   var service = new PlayerDataService();
    ///   var data = await service.GetAsync();
    ///   data._Gold += 100;
    ///   await service.SaveAsync();
    /// </summary>
    public class PlayerDataService
    {
        private const string KEY = "PLAYERDATA";

        private readonly DataManager _dataManager
            = ServiceLocator.Get<DataManager>();

        private PlayerData _cache;

        public PlayerData PlayerData => _cache;

        public async Task CheckPlayerData()
        {
            _cache ??= await _dataManager.LoadAsync<PlayerData>(KEY);
            if(_cache == null)
            {
                //Create new playerData
                _cache = new PlayerData()
                {
                    _Gem = 0,
                    _Gold = BigNumber.FromRaw(100),
                    _Level = 1,
                };

                _cache.GetOrCreateLevel(1);

                await SaveAsync();
            }
        }

        /// <summary>Lấy PlayerData. Load từ file lần đầu, cache lại cho các lần sau.</summary>
        public async Task<PlayerData> GetAsync()
        {
            return _cache ??= await _dataManager.LoadAsync<PlayerData>(KEY);
        }

        /// <summary>Ghi PlayerData xuống file.</summary>
        public async Task SaveAsync()
        {
            if (_cache == null) return;
            await _dataManager.SaveAsync(KEY, _cache);
        }
    }
}
