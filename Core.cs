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
        public static string GunPath { get; set; } = Path.Combine(ModResources, "ak47.glb");
        public static string AudioPath { get; set; } = Path.Combine(ModResources, "ak47-shot.wav");
        public static string IconPath { get; set; } = Path.Combine(ModResources, "icon.png");
        public static string HolePath { get; set; } = Path.Combine(ModResources, "hole.png");


        GameObject? gun;
        AudioClip? shot;
        AudioSource? source;
        const string itemId = "ak47";
        readonly Vector3 normalPosition = new(0.15f, -0.17f, 0.08f);
        readonly Vector3 adsPosition = new(-0.0037f, -0.115f, 0.08f);
        readonly Vector3 normalAngle = new(0f, 0f, 0f);
        readonly Vector3 adsAngle = new(-15f, 0f, 0f);

        Texture2D? bulletHoleTexture;
        const float bulletHoleDuration = 10f;

        float fireTimer = 0f;
        const float fireRate = 0.1f;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Version 1.9 POST") return;
            Transform t = Camera.main.transform;

            gun = GunLoader.LoadGun(GunPath);
            gun.transform.parent = t;

            gun.transform.eulerAngles = t.eulerAngles;
            gun.transform.position = t.position;
            gun.transform.localPosition += normalPosition;
            gun.active = false;

            shot = AudioImporter.Load(AudioPath);
            source = gun.AddComponent<AudioSource>();
            source.clip = shot;
            source.volume = 0.5f;
        }

        private const string MoveTweenId = "gun_move";
        private const string RotateTweenId = "gun_rotate";

        public override void OnUpdate()
        {
            if (gun == null) return;
            if (InventoryManager.Instance.SelectedItem?.Definition.Id != itemId) return;

            if (Input.GetMouseButtonDown(1))
            {
                DOTween.Kill(MoveTweenId);
                gun.transform.DOLocalMove(adsPosition, 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButtonUp(1))
            {
                DOTween.Kill(MoveTweenId);
                gun.transform.DOLocalMove(normalPosition, 0.2f)
                    .SetEase(Ease.OutQuad)
                    .SetId(MoveTweenId);
            }

            if (Input.GetMouseButton(0) && fireTimer <= 0)
            {
                source!.PlayOneShot(shot);
                fireTimer = fireRate;

                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 100f))
                {
                    SpawnBulletHole(hit, bulletHoleTexture!);
                }

                DOTween.Kill(RotateTweenId);
                gun.transform.DOLocalRotate(adsAngle, 0.05f)
                    .SetEase(Ease.OutQuad)
                    .SetId(RotateTweenId)
                    .OnComplete((TweenCallback)(() =>
                    {
                        gun.transform.DOLocalRotate(normalAngle, 0.15f)
                            .SetEase(Ease.OutQuad)
                            .SetId(RotateTweenId);
                    }));
            }

            fireTimer -= Time.deltaTime;
        }


        public override void OnInitializeMelon()
        {
            InventoryManager.Instance.RegisterItem(new ItemDefinition(itemId, "AK47", LoadSprite(IconPath)));
            InventoryManager.Instance.OnItemSelected += Instance_OnItemSelected;

            bulletHoleTexture = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(bulletHoleTexture, File.ReadAllBytes(HolePath));
            bulletHoleTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            //InventoryManager.Instance.PlayerInventory.AddItem(itemId);
        }   

        static void SpawnBulletHole(RaycastHit hit, Texture2D texture)
        {
            if (texture is null) throw new NullReferenceException();
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

        private void Instance_OnItemSelected(InventoryItem? item)
        {
            gun!.active = item != null && item.Definition.Id == itemId;
        }

        public Sprite LoadSprite(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(texture, bytes);

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    } 
}