using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip, int recordSamples)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // 실제 녹음된 길이만큼만 데이터 추출
            float[] samples = new float[recordSamples * clip.channels];
            clip.GetData(samples, 0);

            // WAV 헤더 작성
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + samples.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(samples.Length * 2);

            // 오디오 데이터(PCM 16bit) 작성
            const float rescaleFactor = 32767; 
            for (int i = 0; i < samples.Length; i++)
            {
                writer.Write((short)(samples[i] * rescaleFactor));
            }

            return stream.ToArray();
        }
    }
}