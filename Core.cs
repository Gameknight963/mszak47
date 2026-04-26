using Il2Cpp;
using Il2CppDG.Tweening;
using InventoryFramework;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(mszguns.Core), "Miside Zero AK47", "1.0.0", "gameknight963")]

namespace mszguns
{
    public class Core : MelonMod
    {
        // gun model credit: https://sketchfab.com/3d-models/ak-47-384565b1779c450b90397232163e4e6d
        // bullet hole credit: https://opengameart.org/content/bullet-decal
        // gunshot sound: https://www.youtube.com/watch?v=dMhAdVPt3bY

        public static string ModResources { get; set; } = Path.Combine(MelonEnvironment.ModsDirectory, "mszguns");

        List<Gun> guns = [];
        List<GameObject> gunObjects = [];
        List<AudioClip> gunShots = [];
        List<AudioSource> gunSources = [];

        Gun? activeGun;
        GameObject? activeGunObject;
        AudioSource? activeSource;

        SettingsManager? settingsManager;
        Texture2D? bulletHoleTexture;

        float fireTimer = 0f;

        private const string MoveTweenId = "gun_move";
        private const string RotateTweenId = "gun_rotate";

        public override void OnInitializeMelon()
        {
            guns = GunLoader.LoadAll(ModResources);

            foreach (Gun g in guns)
                InventoryManager.Instance.RegisterItem(new ItemDefinition(g.Id, g.DisplayName, LoadSprite(GunLoader.GetIconPath(ModResources, g))));

            InventoryManager.Instance.OnItemSelected += Instance_OnItemSelected;

            bulletHoleTexture = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(bulletHoleTexture, File.ReadAllBytes(GunLoader.GetDefaultHolePath(ModResources)));
            bulletHoleTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        public override void OnLateInitializeMelon()
        {
            settingsManager = Il2Cpp.Void.instance.settings;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Version 1.9 POST") return;

            Transform t = Camera.main.transform;

            gunObjects.Clear();
            gunShots.Clear();
            gunSources.Clear();

            foreach (Gun g in guns)
            {
                GameObject go = GunLoader.LoadGun(GunLoader.GetModelPath(ModResources, g));
                go.transform.parent = t;
                go.transform.eulerAngles = t.eulerAngles;
                go.transform.position = t.position;
                go.transform.localPosition += g.NormalPosition.ToVector3();
                go.active = false;

                AudioClip clip = AudioImporter.Load(GunLoader.GetAudioPath(ModResources, g));
                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.volume = g.AudioVolume;
                source.playOnAwake = false;

                gunObjects.Add(go);
                gunShots.Add(clip);
                gunSources.Add(source);
            }
        }

        public override void OnUpdate()
        {
            if (activeGun == null || activeGunObject == null) return;
            if (InventoryManager.Instance.SelectedItem?.Definition.Id != activeGun.Id) return;

            if (Input.GetMouseButtonDown(1))
            {
                DOTween.Kill(MoveTweenId);
                activeGunObject.transform.DOLocalMove(activeGun.AdsPosition.ToVector3(), activeGun.AdsSpeed)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
                Camera.main.DOFieldOfView(activeGun.AdsFov, activeGun.AdsSpeed)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButtonUp(1))
            {
                DOTween.Kill(MoveTweenId);
                activeGunObject.transform.DOLocalMove(activeGun.NormalPosition.ToVector3(), activeGun.AdsSpeed)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
                Camera.main.DOFieldOfView(settingsManager!.fov, activeGun.AdsSpeed)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButton(0) && fireTimer <= 0)
            {
                fireTimer = activeGun.FireRate;
                Shoot(activeGun, activeGunObject, activeSource!, bulletHoleTexture!);
            }

            fireTimer -= Time.deltaTime;
        }

        void Shoot(Gun gun, GameObject gunObject, AudioSource source, Texture2D holeTexture)
        {
            source.PlayOneShot(source.clip);

            switch (gun.Effect)
            {
                case ShotEffect.Normal:
                    if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit normalHit, gun.Range))
                        SpawnBulletHole(normalHit, holeTexture, gun.BulletHoleDuration);
                    break;

                case ShotEffect.Shotgun:
                    LoggerInstance.Msg("shotgun shot");
                    Enumerable.Range(0, 8).ToList().ForEach(_ =>
                    {
                        Vector3 spread = Camera.main.transform.forward + new Vector3(
                            UnityEngine.Random.Range(-0.1f, 0.1f),
                            UnityEngine.Random.Range(-0.1f, 0.1f),
                            0f);
                        if (Physics.Raycast(Camera.main.transform.position, spread, out RaycastHit shotgunHit, gun.Range))
                            SpawnBulletHole(shotgunHit, holeTexture, gun.BulletHoleDuration);
                    });
                    break;

                case ShotEffect.Cube:
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                    cube.transform.localScale = Vector3.one * 0.2f;
                    Rigidbody rb = cube.AddComponent<Rigidbody>();
                    rb.velocity = Camera.main.transform.forward * 20f;
                    UnityEngine.Object.Destroy(cube, 5f);
                    break;
            }

            DOTween.Kill(RotateTweenId);
            gunObject.transform.DOLocalRotate(gun.RecoilAngle.ToVector3(), gun.RecoilKickDuration)
                .SetEase(Ease.OutQuad)
                .SetId(RotateTweenId)
                .OnComplete((TweenCallback)(() =>
                {
                    gunObject.transform.DOLocalRotate(gun.NormalAngle.ToVector3(), gun.RecoilRecoverDuration)
                        .SetEase(Ease.OutQuad)
                        .SetId(RotateTweenId);
                }));
        }

        private void Instance_OnItemSelected(InventoryItem? item)
        {
            if (Camera.main == null) return;

            foreach (GameObject go in gunObjects)
                go.active = false;

            if (item == null)
            {
                activeGun = null;
                activeGunObject = null;
                activeSource = null;
                Camera.main.fieldOfView = settingsManager!.fov;
                return;
            }

            int index = guns.FindIndex(g => g.Id == item.Definition.Id);
            if (index == -1) return;

            activeGun = guns[index];
            activeGunObject = gunObjects[index];
            activeSource = gunSources[index];
            activeGunObject.active = true;
            fireTimer = 0;
        }

        static void SpawnBulletHole(RaycastHit hit, Texture2D texture, float duration)
        {
            GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hole.name = "bullet hole";
            hole.transform.position = hit.point + hit.normal * 0.01f;
            hole.transform.rotation = Quaternion.LookRotation(-hit.normal);
            hole.transform.Rotate(0f, 0f, UnityEngine.Random.Range(0f, 360f));
            hole.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.1f, 0.3f);
            hole.transform.SetParent(hit.transform, true);
            UnityEngine.Object.Destroy(hole.GetComponent<MeshCollider>());

            MeshRenderer renderer = hole.GetComponent<MeshRenderer>();
            Material mat = new(Shader.Find("Unlit/Transparent"));
            mat.SetTexture("_MainTex", texture);
            renderer.material = mat;

            renderer.material.DOColor(new Color(1f, 1f, 1f, 0f), 1f)
                .SetDelay(duration)
                .OnComplete((TweenCallback)(() => UnityEngine.Object.Destroy(hole)));
        }

        public Sprite LoadSprite(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture, bytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}