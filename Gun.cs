namespace mszguns
{
    public class Gun
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ModelFile { get; set; } = "";
        public string AudioFile { get; set; } = "";
        public string IconFile { get; set; } = "";
        public float FireRate { get; set; } = 0.1f;
        public float AudioVolume { get; set; } = 0.5f;

        public float[] NormalPosition { get; set; } = [0f, 0f, 0f];
        public float[] AdsPosition { get; set; } = [0f, 0f, 0f];
        public float[] NormalAngle { get; set; } = [0f, 0f, 0f];
        public float[] AdsAngle { get; set; } = [0f, 0f, 0f];

        public Grip? RightHand { get; set; }
        public Grip? LeftHand { get; set; }
    }
}
