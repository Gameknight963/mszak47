namespace mszguns
{
    public class Gun
    {
        public int[] Version { get; set; } = [1, 0, 0];
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ModelFile { get; set; } = "";
        public string? AudioFile { get; set; }
        public string? IconFile { get; set; }
        public string? HoleFile { get; set; }

        public float FireRate { get; set; } = 0.1f;
        public float AudioVolume { get; set; } = 0.5f;
        public float Damage { get; set; } = 10f;
        public float Range { get; set; } = 100f;

        public float AdsSpeed { get; set; } = 0.2f;
        public float AdsFov { get; set; } = 50f;
        public float RecoilKickDuration { get; set; } = 0.05f;
        public float RecoilRecoverDuration { get; set; } = 0.15f;
        public float BulletHoleDuration { get; set; } = 10f;

        public float[] NormalPosition { get; set; } = [0f, 0f, 0f];
        public float[] AdsPosition { get; set; } = [0f, 0f, 0f];
        public float[] NormalAngle { get; set; } = [0f, 0f, 0f];
        public float[] RecoilAngle { get; set; } = [0f, 0f, 0f];

        public ShotEffect Effect { get; set; } = ShotEffect.Normal;
    }
}
