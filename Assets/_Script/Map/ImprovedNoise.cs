using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class ImprovedNoise
{
    private int[] permutations;
    private Vector3 origin;

    public ImprovedNoise(System.Random rand)
    {
        permutations = new int[512];
        origin = new Vector3(
            (float)(rand.NextDouble() * 256),
            (float)(rand.NextDouble() * 256),
            (float)(rand.NextDouble() * 256));

        // 初始化排列表（类似MC的Xoroshiro实现）
        for (int i = 0; i < 256; i++)
            permutations[i] = i;

        // Fisher-Yates洗牌算法
        for (int i = 0; i < 256; i++)
        {
            int j = rand.Next(i, 256);
            int temp = permutations[i];
            permutations[i] = permutations[j];
            permutations[j] = temp;
        }

        System.Array.Copy(permutations, 0, permutations, 256, 256);
    }

    private static double fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static double lerp(double a, double b, double t)
    {
        return a + t * (b - a);
    }

    private static double grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public double Noise(double x, double y, double z)
    {
        // 坐标偏移（模拟MC的wrap功能）
        x += origin.x;
        y += origin.y;
        z += origin.z;

        int X = (int)System.Math.Floor(x) & 255;
        int Y = (int)System.Math.Floor(y) & 255;
        int Z = (int)System.Math.Floor(z) & 255;

        x -= System.Math.Floor(x);
        y -= System.Math.Floor(y);
        z -= System.Math.Floor(z);

        double u = fade(x);
        double v = fade(y);
        double w = fade(z);

        int A = permutations[X] + Y;
        int AA = permutations[A] + Z;
        int AB = permutations[A + 1] + Z;
        int B = permutations[X + 1] + Y;
        int BA = permutations[B] + Z;
        int BB = permutations[B + 1] + Z;

        return lerp(
            lerp(
                lerp(grad(permutations[AA], x, y, z),
                     grad(permutations[BA], x - 1, y, z), u),
                lerp(grad(permutations[AB], x, y - 1, z),
                     grad(permutations[BB], x - 1, y - 1, z), u), v),
            lerp(
                lerp(grad(permutations[AA + 1], x, y, z - 1),
                     grad(permutations[BA + 1], x - 1, y, z - 1), u),
                lerp(grad(permutations[AB + 1], x, y - 1, z - 1),
                     grad(permutations[BB + 1], x - 1, y - 1, z - 1), u), v), w);
    }
}