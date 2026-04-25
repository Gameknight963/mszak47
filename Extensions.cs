using UnityEngine;

namespace mszguns
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this float[] arr) => new(arr[0], arr[1], arr[2]);

        public static Transform? FindRecursive(this Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name) return child;
                Transform? found = child.FindRecursive(name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
