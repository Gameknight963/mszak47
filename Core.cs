using Il2CppDG.Tweening;
using InventoryFramework;
using MelonLoader;
using MelonLoader.Utils;
using NAudio.CoreAudioApi;
using NAudio.Wave;
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
        public static string HolePath { get; set; } = Path.Combine(ModResources, "hole.png");

        static readonly List<Gun> guns =
        [
            new Gun
        {
            Id = "ak47",
            DisplayName = "AK47",
            ModelFile = "ak47.glb",
            AudioFile = "ak47-shot.wav",
            IconFile = "icon.png",
            FireRate = 0.1f,
            AudioVolume = 0.5f,
            NormalPosition = [0.15f, -0.17f, 0.08f],
            AdsPosition = [-0.0037f, -0.115f, 0.08f],
            NormalAngle = [0f, 0f, 0f],
            AdsAngle = [-15f, 0f, 0f],
        }
        ];

        GameObject? gun;
        Gun? activeGun;
        AudioClip? shot;
        AudioSource? source;

        Texture2D? bulletHoleTexture;
        const float bulletHoleDuration = 10f;

        float fireTimer = 0f;

        private const string MoveTweenId = "gun_move";
        private const string RotateTweenId = "gun_rotate";

        public override void OnInitializeMelon()
        {
            foreach (Gun g in guns)
            {
                InventoryManager.Instance.RegisterItem(new ItemDefinition(g.Id, g.DisplayName, LoadSprite(Path.Combine(ModResources, g.IconFile))));
            }

            InventoryManager.Instance.OnItemSelected += Instance_OnItemSelected;

            bulletHoleTexture = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(bulletHoleTexture, File.ReadAllBytes(HolePath));
            bulletHoleTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Version 1.9 POST") return;
            Transform t = Camera.main.transform;

            // load first gun by default, will be driven by selection later
            activeGun = guns[0];

            gun = GunLoader.LoadGun(Path.Combine(ModResources, activeGun.ModelFile));
            gun.transform.parent = t;
            gun.transform.eulerAngles = t.eulerAngles;
            gun.transform.position = t.position;
            gun.transform.localPosition += activeGun.NormalPosition.ToVector3();
            gun.active = false;

            shot = AudioImporter.Load(Path.Combine(ModResources, activeGun.AudioFile));
            source = gun.AddComponent<AudioSource>();
            source.clip = shot;
            source.volume = activeGun.AudioVolume;
        }

        public override void OnUpdate()
        {
            if (gun == null || activeGun == null) return;
            if (InventoryManager.Instance.SelectedItem?.Definition.Id != activeGun.Id) return;

            if (Input.GetMouseButtonDown(1))
            {
                DOTween.Kill(MoveTweenId);
                gun.transform.DOLocalMove(activeGun.AdsPosition.ToVector3(), 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButtonUp(1))
            {
                DOTween.Kill(MoveTweenId);
                gun.transform.DOLocalMove(activeGun.NormalPosition.ToVector3(), 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButton(0) && fireTimer <= 0)
            {
                source!.PlayOneShot(shot);
                fireTimer = activeGun.FireRate;

                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 100f))
                {
                    SpawnBulletHole(hit, bulletHoleTexture!);
                }

                DOTween.Kill(RotateTweenId);
                gun.transform.DOLocalRotate(activeGun.AdsAngle.ToVector3(), 0.05f)
                    .SetEase(Ease.OutQuad)
                    .SetId(RotateTweenId)
                    .OnComplete((TweenCallback)(() =>
                    {
                        gun.transform.DOLocalRotate(activeGun.NormalAngle.ToVector3(), 0.15f)
                            .SetEase(Ease.OutQuad)
                            .SetId(RotateTweenId);
                    }));
            }

            fireTimer -= Time.deltaTime;
        }

        private void Instance_OnItemSelected(InventoryItem? item)
        {
            if (item == null)
            {
                gun!.active = false;
                return;
            }

            Gun? selected = guns.FirstOrDefault(g => g.Id == item.Definition.Id);
            if (selected == null) return;

            activeGun = selected;
            gun!.active = true;
        }

        static void SpawnBulletHole(RaycastHit hit, Texture2D texture)
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
                .SetDelay(bulletHoleDuration)
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