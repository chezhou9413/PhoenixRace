using System;
using System.IO;
using System.Text;

//合成原创炮声与低频余响，避免依赖外部采样或复用原版枪声。
public static class PhoenixTurretAudio
{
    private const int SampleRate = 44100;

    //三个确定性变体由 SoundDef 随机选择，文件为游戏直接读取的单声道 PCM。
    public static void Write(string modRoot)
    {
        string directory = modRoot + "/1.7/Sounds/Phoenix/Turret";
        Directory.CreateDirectory(directory + "/Fire");
        for (int i = 0; i < 3; i++)
            Save(directory + "/Fire/Fire_" + (i + 1) + ".wav", Synthesize(9413 + i * 31, false));
        Save(directory + "/Tail.wav", Synthesize(7117, true));
    }

    //短噪声爆裂叠加降调低频冲击、机械尾音和延迟反射。
    private static float[] Synthesize(int seed, bool tail)
    {
        var random = new Random(seed);
        float[] result = new float[(int)(SampleRate * (tail ? 1.1 : .65))];
        double low = 0, phase = 0;
        for (int i = 0; i < result.Length; i++)
        {
            double t = (double)i / SampleRate;
            double noise = random.NextDouble() * 2 - 1;
            low += .035 * (noise - low);
            phase += Math.PI * 2 * (58 + 105 * Math.Exp(-t * 24)) / SampleRate;
            double attack = Math.Min(1, t / .0015);
            double crack = noise * Math.Exp(-t * 85) * .75;
            double body = Math.Sin(phase) * Math.Exp(-t * 17) * .65 + low * Math.Exp(-t * 8) * 2.1;
            double mechanism = Math.Sin(t * Math.PI * 2 * 820) * Math.Exp(-Math.Abs(t - .075) * 85) * .065;
            double value = tail ? (low * 2.8 + Math.Sin(phase) * .18) * Math.Exp(-t * 5.5) * Math.Min(1, t / .025)
                : (crack + body + mechanism) * attack;
            result[i] = (float)value;
        }
        //反射只读取干声，避免形成持续反馈。
        float[] dry = (float[])result.Clone();
        foreach (double delay in new[] { .047, .083, .137 })
        {
            int offset = (int)(delay * SampleRate);
            for (int i = offset; i < result.Length; i++) result[i] += dry[i - offset] * (float)(.2 - delay * .7);
        }
        float peak = .001f;
        foreach (float sample in result) peak = Math.Max(peak, Math.Abs(sample));
        for (int i = 0; i < result.Length; i++)
            result[i] *= (tail ? .56f : .88f) / peak * Math.Min(1, (result.Length - 1 - i) / (SampleRate * .015f));
        return result;
    }

    //明确写入小端 WAV 头和 16 位采样，控制峰值以避免数字削波。
    private static void Save(string path, float[] samples)
    {
        using var writer = new BinaryWriter(File.Create(path), Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
        writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate); writer.Write(SampleRate * 2);
        writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
        foreach (float sample in samples) writer.Write((short)(sample * 32767));
    }
}
