using UnityEngine;

namespace mszguns
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this float[] arr) => new(arr[0], arr[1], arr[2]);
    }
}
