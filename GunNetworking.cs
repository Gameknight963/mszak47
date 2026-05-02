using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using MultiSide.shared;
using UnityEngine;

namespace mszguns
{
    public class GunNetworking
    {
        public static GunNetworking? Instance { get; private set; }

        private readonly Dictionary<int, string> _remoteGuns = new();
        private readonly Dictionary<int, Dictionary<string, GameObject>> _remoteGunObjects = new();
        private readonly string _modResources;
        private readonly List<Gun> _guns;
        private GunNetworkLogger? _logger;

        bool _loggingEnabled = false;
        public bool LoggingEnabled 
        {
            get => _loggingEnabled;
            set
            {
                _loggingEnabled = value;
                _logger?.SetLoggingEnabled(value);
            }
        }

        private GunNetworking(string modResources, List<Gun> guns)
        {
            _modResources = modResources;
            _guns = guns;
        }

        private class GunNetworkLogger(MelonLogger.Instance logger)
        {
            public void SetLoggingEnabled(bool enabled) => _loggingEnabled = enabled;
            public bool _loggingEnabled = false;
            public void Msg(object obj) { if (_loggingEnabled) logger.Msg(obj); }
            public void Warning(object obj) { if (_loggingEnabled) logger.Warning(obj); }
            public void Error(object obj) { if (_loggingEnabled) logger.Warning(obj); }
        }

        public static void Init(string modResources, List<Gun> guns, MelonLogger.Instance logger)
        {
            bool available = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "Multiside.shared");
            if (!available) return;
            Instance = new GunNetworking(modResources, guns);

            Instance._logger = new GunNetworkLogger(logger);
            Instance._logger.SetLoggingEnabled(Instance.LoggingEnabled);
            Instance._logger.Msg("Starting in Online mode");
            Instance.InitNetwork();
        }

        // shot data layout: [0-2] position/hitPoint, [3-5] direction/hitNormal, [6] duration, [7] effect
        // for cube: position is spawn position, direction is forward
        // for normal/shotgun: missed shots are not sent

        private void InitNetwork()
        {
            if (NetworkRegistry.Provider != null)
            {
                Subscribe(NetworkRegistry.Provider);
                return;
            }
            NetworkRegistry.OnProviderRegistered += Subscribe;
        }

        private void Subscribe(INetworkProvider provider)
        {
            _logger?.Msg("GunNetworking: subscribing to network events");

            provider.OnReceived += (actor, channel, data) =>
            {
                if (channel == "mszguns.equip")
                {
                    string gunId = (string)data;
                    _logger?.Msg($"GunNetworking: equip received from {actor}: '{gunId}'");
                    if (string.IsNullOrEmpty(gunId))
                        _remoteGuns.Remove(actor);
                    else
                        _remoteGuns[actor] = gunId;
                    GameObject? playerObj = provider.GetPlayerObject(actor);
                    if (playerObj == null)
                        _logger?.Warning($"GunNetworking: no player object for actor {actor}");
                    else
                        SetRemoteGun(actor, playerObj, gunId);
                    return;
                }

                if (channel == "mszguns.audio")
                {
                    GameObject? playerObj = provider.GetPlayerObject(actor);
                    if (playerObj != null)
                        PlayRemoteShot(playerObj);
                    return;
                }

                if (channel != "mszguns.shot") return;

                float[] d = (float[])data;
                ShotEffect effect = (ShotEffect)(int)d[7];

                if (effect == ShotEffect.Cube)
                {
                    SpawnRemoteCube(
                        new Vector3(d[0], d[1], d[2]),
                        new Vector3(d[3], d[4], d[5]));
                }
                else
                {
                    if (d.Length < 7) return;
                    if (Core.BulletHoleTexture == null) return;
                    Core.SpawnBulletHole(
                        new Vector3(d[0], d[1], d[2]),
                        new Vector3(d[3], d[4], d[5]),
                        Core.BulletHoleTexture,
                        d[6]);
                }
            };

            provider.OnPlayerLeft += actor =>
            {
                _logger?.Msg($"GunNetworking: player {actor} left, cleaning up");
                _remoteGuns.Remove(actor);
                _remoteGunObjects.Remove(actor);
            };

            provider.OnRoomJoined += () =>
            {
                _logger?.Msg($"GunNetworking: room joined, broadcasting equip: '{Core.ActiveGunId}'");
                SendEquip(Core.ActiveGunId);
            };

            provider.OnPlayerJoined += actor =>
            {
                _logger?.Msg($"GunNetworking: player {actor} joined, sending equip: '{Core.ActiveGunId}'");
                if (!string.IsNullOrEmpty(Core.ActiveGunId))
                    provider.SendTo(actor, "mszguns.equip", Core.ActiveGunId);
            };
        }

        public void SendFireAudioMessage()
        {
            NetworkRegistry.Provider?.Send("mszguns.audio", -1);
        }

        private void SetRemoteGun(int actor, GameObject playerObj, string gunId)
        {
            _logger?.Msg($"GunNetworking: setting remote gun for actor {actor}: '{gunId}'");

            if (_remoteGunObjects.TryGetValue(actor, out Dictionary<string, GameObject>? models))
            {
                foreach (GameObject m in models.Values)
                    m.SetActive(false);
                if (!string.IsNullOrEmpty(gunId) && models.TryGetValue(gunId, out GameObject? cached))
                {
                    _logger?.Msg($"GunNetworking: using cached model for '{gunId}'");
                    cached.SetActive(true);
                    UpdateAudioClip(playerObj, gunId);
                    return;
                }
            }

            if (string.IsNullOrEmpty(gunId)) return;

            Gun? gun = _guns.FirstOrDefault(g => g.Id == gunId);
            if (gun == null)
            {
                _logger?.Warning($"GunNetworking: gun '{gunId}' not found in registry");
                return;
            }

            _logger?.Msg($"GunNetworking: loading model for '{gunId}'");
            GameObject model = GunLoader.LoadGun(GunLoader.GetModelPath(_modResources, gun));
            model.name = "RemoteGun";
            Transform? camera = playerObj.transform.Find("Zero/PLAYER Armature/Rig Root/Hips/Spine/Chest/Neck2/Neck1/Head/CameraHoldHead/playerCamera");
            if (camera == null)
                _logger?.Warning($"GunNetworking: could not find camera transform on actor {actor}, parenting to root");
            model.transform.SetParent(camera ?? playerObj.transform, false);
            model.transform.localPosition = gun.NormalPosition.ToVector3();
            model.transform.localEulerAngles = gun.NormalAngle.ToVector3();

            if (!_remoteGunObjects.ContainsKey(actor))
                _remoteGunObjects[actor] = new();
            _remoteGunObjects[actor][gunId] = model;

            UpdateAudioClip(playerObj, gunId);
        }

        private void UpdateAudioClip(GameObject playerObj, string gunId)
        {
            Gun? gun = _guns.FirstOrDefault(g => g.Id == gunId);
            if (gun == null)
            {
                _logger?.Warning($"HandleGunAudio: gun '{gunId}' not found");
                return;
            }

            AudioSource? source = playerObj.GetComponent<AudioSource>();
            
            if (source == null)
            {
                _logger?.Msg($"GunNetworking: adding AudioSource to player object for '{gunId}'");
                source = playerObj.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.maxDistance = 50f;
                source.rolloffMode = AudioRolloffMode.Linear;
            }

            if (source.clip == null || source.clip.name != gun.Id)
            {
                source.clip = AudioImporter.Load(GunLoader.GetAudioPath(_modResources, gun));
            }
        }

        private void PlayRemoteShot(GameObject playerObj)
        {
            AudioSource? source = playerObj.GetComponent<AudioSource>();
            source.PlayOneShot(source.clip);
        }

        private static void SpawnRemoteCube(Vector3 spawnPos, Vector3 spawnDir)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = spawnPos;
            cube.transform.localScale = Vector3.one * 0.2f;
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.velocity = spawnDir * 20f;
            UnityEngine.Object.Destroy(cube, 5f);
        }

        public void SendShot(ShotEffect effect, Vector3 pos, Vector3 dir, float duration)
        {
            NetworkRegistry.Provider?.Send("mszguns.shot", new float[]
            {
            pos.x, pos.y, pos.z,
            dir.x, dir.y, dir.z,
            duration,
            (float)effect
            });
        }

        public void SendEquip(string? gunId)
        {
            NetworkRegistry.Provider?.Send("mszguns.equip", gunId ?? "", true);
        }
    }
}
