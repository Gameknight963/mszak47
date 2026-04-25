using UnityEngine;
using VGltf;

namespace mszguns
{
    internal static class GltfHelpers
    {
        public static void BuildNode(GltfContainer container, int nodeIndex, Transform parent)
        {
            VGltf.Types.Node node = container.Gltf.Nodes[nodeIndex];
            GameObject go = new(node.Name ?? "Node");
            go.transform.SetParent(parent, false);

            if (node.Mesh != null)
            {
                (UnityEngine.Mesh mesh, int? materialIndex) = BuildMesh(container, node.Mesh.Value);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                Material material = new(Shader.Find("Standard"));
                if (materialIndex != null)
                {
                    Texture2D? tex = LoadTexture(container, materialIndex.Value);
                    if (tex != null)
                        material.mainTexture = tex;
                }
                renderer.material = material;
            }

            if (node.Children != null)
                foreach (int child in node.Children)
                    BuildNode(container, child, go.transform);
        }

        public static (UnityEngine.Mesh, int?) BuildMesh(GltfContainer container, int meshIndex)
        {
            VGltf.Types.Mesh gltfMesh = container.Gltf.Meshes[meshIndex];
            UnityEngine.Mesh mesh = new();

            VGltf.Types.Mesh.PrimitiveType prim = gltfMesh.Primitives[0];

            int posAccessorIndex = prim.Attributes["POSITION"];
            Vector3[] vertices = ReadVec3Array(container, posAccessorIndex);
            mesh.vertices = vertices;

            if (prim.Attributes.ContainsKey("TEXCOORD_0"))
            {
                int uvAccessorIndex = prim.Attributes["TEXCOORD_0"];
                Vector2[] uvs = ReadVec2Array(container, uvAccessorIndex);
                mesh.uv = uvs;
            }

            if (prim.Indices != null)
            {
                int[] indices = ReadIntArray(container, prim.Indices.Value);
                for (int i = 0; i < indices.Length; i += 3)
                {
                    (indices[i + 2], indices[i]) = (indices[i], indices[i + 2]);
                }
                mesh.triangles = indices;
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return (mesh, prim.Material);
        }

        public static Texture2D? LoadTexture(GltfContainer container, int materialIndex)
        {
            VGltf.Types.Material mat = container.Gltf.Materials[materialIndex];

            if (mat.PbrMetallicRoughness?.BaseColorTexture == null) return null;

            int textureIndex = mat.PbrMetallicRoughness.BaseColorTexture.Index;
            VGltf.Types.Texture texture = container.Gltf.Textures[textureIndex];

            if (texture.Source == null) return null;

            VGltf.Types.Image image = container.Gltf.Images[texture.Source.Value];

            if (image.BufferView == null) return null;

            VGltf.Types.BufferView view = container.Gltf.BufferViews[image.BufferView.Value];
            byte[] buffer = container.Buffer.Payload.ToArray();
            byte[] imageData = new byte[view.ByteLength];
            Array.Copy(buffer, view.ByteOffset, imageData, 0, view.ByteLength);

            Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(tex, imageData);
            return tex;
        }

        public static Vector2[] ReadVec2Array(GltfContainer container, int accessorIndex)
        {
            VGltf.Types.Accessor accessor = container.Gltf.Accessors[accessorIndex];
            VGltf.Types.BufferView view = container.Gltf.BufferViews[accessor.BufferView!.Value];
            byte[] buffer = container.Buffer.Payload.ToArray();
            int offset = view.ByteOffset + accessor.ByteOffset;
            Vector2[] result = new Vector2[accessor.Count];
            for (int i = 0; i < accessor.Count; i++)
            {
                float x = BitConverter.ToSingle(buffer, offset + i * 8);
                float y = BitConverter.ToSingle(buffer, offset + i * 8 + 4);
                result[i] = new Vector2(x, 1f - y);
            }
            return result;
        }

        public static Vector3[] ReadVec3Array(GltfContainer container, int accessorIndex)
        {
            VGltf.Types.Accessor accessor = container.Gltf.Accessors[accessorIndex];
            VGltf.Types.BufferView view = container.Gltf.BufferViews[accessor.BufferView!.Value];
            byte[] buffer = container.Buffer.Payload.ToArray();
            int offset = view.ByteOffset + accessor.ByteOffset;
            Vector3[] result = new Vector3[accessor.Count];
            for (int i = 0; i < accessor.Count; i++)
            {
                float x = BitConverter.ToSingle(buffer, offset + i * 12);
                float y = BitConverter.ToSingle(buffer, offset + i * 12 + 4);
                float z = BitConverter.ToSingle(buffer, offset + i * 12 + 8);
                result[i] = new Vector3(-x, y, z);
            }
            return result;
        }

        public static int[] ReadIntArray(GltfContainer container, int accessorIndex)
        {
            VGltf.Types.Accessor accessor = container.Gltf.Accessors[accessorIndex];
            VGltf.Types.BufferView view = container.Gltf.BufferViews[accessor.BufferView!.Value];
            byte[] buffer = container.Buffer.Payload.ToArray();
            int offset = view.ByteOffset + accessor.ByteOffset;
            int[] result = new int[accessor.Count];
            for (int i = 0; i < accessor.Count; i++)
            {
                if (accessor.ComponentType == VGltf.Types.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                    result[i] = BitConverter.ToUInt16(buffer, offset + i * 2);
                else if (accessor.ComponentType == VGltf.Types.Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    result[i] = (int)BitConverter.ToUInt32(buffer, offset + i * 4);
            }
            return result;
        }
    }
}
