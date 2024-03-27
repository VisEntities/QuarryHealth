using Newtonsoft.Json;
using System.Collections;
using System.Linq;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Previous maintenance and contributions by Arainrr
 * Originally created by Waizujin
 */

namespace Oxide.Plugins
{
    [Info("Quarry Health", "VisEntities", "2.0.0")]
    [Description("Changes the health of quarries and pump jacks.")]
    public class QuarryHealth : RustPlugin
    {
        #region Fields

        private Coroutine _healthRefreshCoroutine;
        private static Configuration _config;

        private const string PREFAB_PUMP_JACK = "assets/prefabs/deployable/oil jack/mining.pumpjack.prefab";
        private const string PREFAB_MINING_QUARRY = "assets/prefabs/deployable/quarry/mining_quarry.prefab";

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Mining Quarry Hit Points")]
            public float MiningQuarryHitPoints { get; set; }

            [JsonProperty("Pump Jack Hit Points")]
            public float PumpJackQuarryHitPoints { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                MiningQuarryHitPoints = 2500f,
                PumpJackQuarryHitPoints = 2500f
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            StartHealthRefreshCoroutine();
        }

        private void Unload()
        {
            StopHealthRefreshCoroutine();
            _config = null;
        }

        private void OnEntitySpawned(BaseResourceExtractor resourceExtractor)
        {
            if (resourceExtractor != null)
                InitializeResourceExtractorHealth(resourceExtractor);
        }

        #endregion Oxide Hooks

        #region Coroutine

        private void StartHealthRefreshCoroutine()
        {
            _healthRefreshCoroutine = ServerMgr.Instance.StartCoroutine(RefreshAllResourceExtractorsHealth());
        }
        
        private void StopHealthRefreshCoroutine()
        {
            if (_healthRefreshCoroutine != null)
            {
                ServerMgr.Instance.StopCoroutine(_healthRefreshCoroutine);
                _healthRefreshCoroutine = null;
            }
        }

        #endregion Coroutine

        #region Health

        private IEnumerator RefreshAllResourceExtractorsHealth()
        {
            foreach (BaseResourceExtractor resourceExtractor in BaseNetworkable.serverEntities.OfType<BaseResourceExtractor>())
            {
                if (resourceExtractor != null)
                    InitializeResourceExtractorHealth(resourceExtractor);

                yield return CoroutineEx.waitForSeconds(0.5f);
            }
        }

        private void InitializeResourceExtractorHealth(BaseResourceExtractor resourceExtractor)
        {
            if (resourceExtractor.PrefabName == PREFAB_MINING_QUARRY)
            {
                resourceExtractor.InitializeHealth(_config.MiningQuarryHitPoints, _config.MiningQuarryHitPoints);
            }
            else if (resourceExtractor.PrefabName == PREFAB_PUMP_JACK)
            {
                resourceExtractor.InitializeHealth(_config.MiningQuarryHitPoints, _config.PumpJackQuarryHitPoints);
            }

            resourceExtractor.SendNetworkUpdateImmediate();
        }

        #endregion Health
    }
}