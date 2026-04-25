using Newtonsoft.Json;
using UnityEngine;
using VGltf;

namespace mszguns
{
    internal static class GunLoader
    {
        public static List<Gun> LoadAll(string modResources)
        {
            List<Gun> guns = [];
            foreach (string dir in Directory.GetDirectories(modResources))
            {
                string configPath = Path.Combine(dir, "gun.json");
                if (!File.Exists(configPath)) continue;

                Gun gun = JsonConvert.DeserializeObject<Gun>(File.ReadAllText(configPath))!;

                // infer id from folder name if not set
                if (string.IsNullOrEmpty(gun.Id))
                    gun.Id = Path.GetFileName(dir);

                gun.ModelFile ??= Directory.GetFiles(dir, "*.glb").FirstOrDefault() ?? "";
                gun.AudioFile ??= Directory.GetFiles(dir, "*.wav").FirstOrDefault()
                               ?? Directory.GetFiles(dir, "*.mp3").FirstOrDefault();
                gun.IconFile ??= Directory.GetFiles(dir, "icon.*").FirstOrDefault()
                              ?? Directory.GetFiles(dir, "*.png").FirstOrDefault();
                gun.HoleFile ??= Directory.GetFiles(dir, "hole.*").FirstOrDefault();

                guns.Add(gun);
            }
            return guns;
        }

        public static GameObject LoadGun(string path)
        {
            GltfContainer container;
            using (FileStream fs = new(path, FileMode.Open))
                container = GltfContainer.FromGlb(fs);

            GameObject root = new("Gun");

            if (container.Gltf.Scene != null)
                foreach (int nodeIndex in container.Gltf.Scenes[container.Gltf.Scene.Value].Nodes)
                    GltfHelpers.BuildNode(container, nodeIndex, root.transform);

            root.transform.localScale = Vector3.one * 0.0005f;
            return root;
        }

        public static string GetModelPath(string modResources, Gun gun)
            => Path.IsPathRooted(gun.ModelFile) ? gun.ModelFile : Path.Combine(modResources, gun.Id, gun.ModelFile);

        public static string GetAudioPath(string modResources, Gun gun)
            => gun.AudioFile != null && Path.IsPathRooted(gun.AudioFile) ? gun.AudioFile
               : Fallback(modResources, gun.Id, gun.AudioFile ?? "shot.wav", "shot.wav");

        public static string GetIconPath(string modResources, Gun gun)
            => gun.IconFile != null && Path.IsPathRooted(gun.IconFile) ? gun.IconFile
               : Fallback(modResources, gun.Id, gun.IconFile ?? "icon.png", "icon.png");

        public static string GetHolePath(string modResources, Gun gun)
            => gun.HoleFile != null && Path.IsPathRooted(gun.HoleFile) ? gun.HoleFile
               : Fallback(modResources, gun.Id, gun.HoleFile ?? "hole.png", "hole.png");

        public static string GetDefaultHolePath(string modResources)
            => Path.Combine(modResources, "hole.png");

        static string Fallback(string modResources, string gunId, string file, string fallback)
        {
            string specific = Path.Combine(modResources, gunId, file);
            return File.Exists(specific) ? specific : Path.Combine(modResources, fallback);
        }
    }
}
