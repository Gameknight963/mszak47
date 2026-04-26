using NAudio.Wave;
using UnityEngine;

namespace mszguns
{
    public static class AudioImporter
    {
        public static AudioClip Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            using (AudioFileReader reader = new(filePath))
            {
                int channels = reader.WaveFormat.Channels;
                int sampleRate = reader.WaveFormat.SampleRate;

                float[] samples = ReadAllSamples(reader);

                AudioClip clip = AudioClip.Create(
                    Path.GetFileNameWithoutExtension(filePath),
                    samples.Length / channels,
                    channels,
                    sampleRate,
                    false
                );

                clip.SetData(samples, 0);
                return clip;
            }
        }

        private static float[] ReadAllSamples(AudioFileReader reader)
        {
            List<float> sampleList = new((int)(reader.Length / 4));

            float[] buffer = new float[4096];
            int read;

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                    sampleList.Add(buffer[i]);
            }

            return sampleList.ToArray();
        }
    }
}
