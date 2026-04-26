using MelonLoader;
using MultiSide.shared;
using UnityEngine;

namespace mszguns
{
    public static class GunNetworking
    {
        private static bool _available;

        private static Dictionary<int, string> _remoteGuns = new();
        private static string _modResources = "";
        private static List<Gun> _guns = new();

        private static Dictionary<int, Dictionary<string, GameObject>> _remoteGunObjects = new();

        public static void Init(string modResources, List<Gun> guns)
        {
            _modResources = modResources;
            _guns = guns;
            _available = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "Multiside.shared");

            if (!_available) return;
            InitNetwork();
        }

        // shot data layout: [0-2] position/hitPoint, [3-5] direction/hitNormal, [6] duration, [7] effect
        // for cube: position is spawn position, direction is forward
        // for normal/shotgun: missed shots are not sent

        private static void InitNetwork()
        {
            if (NetworkRegistry.Provider != null)
            {
                Subscribe(NetworkRegistry.Provider);
                return;
            }
            NetworkRegistry.OnProviderRegistered += Subscribe;
        }

        private static void Subscribe(INetworkProvider provider)
        {
            provider.OnReceived += (actor, channel, data) =>
            {
                if (channel == "mszguns.equip")
                {
                    string gunId = (string)data;
                    if (string.IsNullOrEmpty(gunId))
                        _remoteGuns.Remove(actor);
                    else
                        _remoteGuns[actor] = gunId;

                    GameObject? playerObj = provider.GetPlayerObject(actor);
                    if (playerObj != null)
                        SetRemoteGun(actor, playerObj, gunId);
                    return;
                }

                if (channel != "mszguns.shot") return;

                float[] d = (float[])data;
                ShotEffect effect = (ShotEffect)(int)d[7];

                if (effect == ShotEffect.Cube)
                {
                    Vector3 spawnPos = new(d[0], d[1], d[2]);
                    Vector3 spawnDir = new(d[3], d[4], d[5]);
                    SpawnRemoteCube(spawnPos, spawnDir);
                }
                else
                {
                    if (d.Length < 7) return; // missed shot, nothing to show
                    Vector3 hitPoint = new(d[0], d[1], d[2]);
                    Vector3 hitNormal = new(d[3], d[4], d[5]);
                    float duration = d[6];

                    if (Core.BulletHoleTexture == null) return;
                    Core.SpawnBulletHole(hitPoint, hitNormal, Core.BulletHoleTexture, duration);

                    GameObject? playerObj = provider.GetPlayerObject(actor);
                    if (playerObj != null)
                        PlayRemoteShot(playerObj, actor);
                }
            };

            provider.OnPlayerLeft += actor =>
            {
                _remoteGuns.Remove(actor);
                _remoteGunObjects.Remove(actor);
            };

            provider.OnRoomJoined += () =>
            {
                SendEquip(Core.ActiveGunId);
            };

            provider.OnPlayerJoined += actor =>
            {
                if (!string.IsNullOrEmpty(Core.ActiveGunId))
                    NetworkRegistry.Provider?.SendTo(actor, "mszguns.equip", Core.ActiveGunId);
            };
        }

        private static void SetRemoteGun(int actor, GameObject playerObj, string gunId)
        {
            if (_remoteGunObjects.TryGetValue(actor, out Dictionary<string, GameObject>? models))
            {
                foreach (GameObject m in models.Values)
                    m.SetActive(false);

                // if we already have this gun cached, just enable it
                if (!string.IsNullOrEmpty(gunId) && models.TryGetValue(gunId, out GameObject? cached))
                {
                    cached.SetActive(true);
                    return;
                }
            }

            if (string.IsNullOrEmpty(gunId)) return;

            Gun? gun = _guns.FirstOrDefault(g => g.Id == gunId);
            if (gun == null) return;

            // load and cache
            GameObject model = GunLoader.LoadGun(GunLoader.GetModelPath(_modResources, gun));
            model.name = "RemoteGun";
            Transform? camera = playerObj.transform.Find("Zero/PLAYER Armature/Rig Root/Hips/Spine/Chest/Neck2/Neck1/Head/CameraHoldHead/playerCamera");
            model.transform.SetParent(camera != null ? camera : playerObj.transform, false);
            model.transform.localPosition = gun.NormalPosition.ToVector3();
            model.transform.localEulerAngles = gun.NormalAngle.ToVector3();

            if (!_remoteGunObjects.ContainsKey(actor))
                _remoteGunObjects[actor] = new Dictionary<string, GameObject>();
            _remoteGunObjects[actor][gunId] = model;
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

        public static void SendShot(ShotEffect effect, Vector3 pos, Vector3 dir, float duration)
        {
            if (!_available) return;
            NetworkRegistry.Provider?.Send("mszguns.shot", new float[]
            {
                pos.x, pos.y, pos.z,
                dir.x, dir.y, dir.z,
                duration,
                (float)effect
            });
        }

        private static void PlayRemoteShot(GameObject playerObj, int actor)
        {
            if (!_remoteGuns.TryGetValue(actor, out string? gunId))
            {
                MelonLogger.Warning($"PlayRemoteShot: no gun tracked for actor {actor}");
                return;
            }

            Gun? gun = _guns.FirstOrDefault(g => g.Id == gunId);
            if (gun == null) return;

            AudioSource? source = playerObj.GetComponent<AudioSource>();
            if (source == null)
            {
                source = playerObj.AddComponent<AudioSource>();
                source.spatialBlend = 1f;
                source.maxDistance = 50f;
                source.rolloffMode = AudioRolloffMode.Linear;
            }

            if (source.clip == null)
                source.clip = AudioImporter.Load(GunLoader.GetAudioPath(_modResources, gun));

            source.PlayOneShot(source.clip);
        }

        public static void SendEquip(string? gunId)
        {
            if (!_available) return;
            NetworkRegistry.Provider?.Send("mszguns.equip", gunId ?? "", true);
        }
    }
}
