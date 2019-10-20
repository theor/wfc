/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

class Integration
{
    public static class Utils
    {
        static MethodInfo _clearConsoleMethod;

        static MethodInfo ClearConsoleMethod
        {
            get
            {
                if (_clearConsoleMethod == null)
                {
                    Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
                    Type logEntries = assembly.GetType("UnityEditor.LogEntries");
                    _clearConsoleMethod = logEntries.GetMethod("Clear");
                }

                return _clearConsoleMethod;
            }
        }

        public static void ClearLogConsole()
        {
            ClearConsoleMethod.Invoke(new object(), null);
        }
    }

    [MenuItem("WFC/Run")]
    static void F()
    {
        Utils.ClearLogConsole();
//        Random.InitState(Time.tim);
        bool finished = false;
        OverlappingModel model = null;
//        var name = "Dungeon";
        var name = "Flowers";

        for (int i = 0; i < 1 && !finished; i++)
        {
            model = new OverlappingModel(name, 3, 48, 48, true, true, 2, -4);
//            model = new OverlappingModel(name, 3, 48, 48, true, true, 8, 0);
            finished = model.Run(42, 0);//(int) (DateTime.Now.Millisecond), 0);
        }

        Debug.Log("Finished: " + finished);
        if (finished)
        {
            var t = model.Graphics();

            File.WriteAllBytes($"Assets\\{name}-result.png", t.EncodeToPNG());
        }

        AssetDatabase.Refresh();
//        AssetDatabase.CreateAsset(t, "");
    }
}

class OverlappingModel : Model
{
    int _n;
    byte[][] _patterns;
    List<Color> _colors;
    int _ground;

    public OverlappingModel(string name, int n, int width, int height, bool periodicInput, bool periodicOutput,
        int symmetry, int ground)
        : base(width, height)
    {
        this._n = n;
        periodic = periodicOutput;

        var bitmap =
            AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/{name}.png"); // new Bitmap($"samples/{name}.png");
        int smx = bitmap.width, smy = bitmap.height;
        byte[,] sample = new byte[smx, smy];
        _colors = new List<Color>();

        for (int y = 0; y < smy; y++)
        for (int x = 0; x < smx; x++)
        {
            Color color = bitmap.GetPixel(x, smy - 1 - y);
            

            int i = 0;
            foreach (var col in _colors)
            {
                if (col == color) break;
                i++;
            }

            if (i == _colors.Count) _colors.Add(color);
            sample[x, y] = (byte) i;
        }

        int c = _colors.Count;
        long w = Stuff.Power(c, n * n);

        byte[] Pattern(Func<int, int, byte> f)
        {
            byte[] result = new byte[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                result[x + y * n] = f(x, y);
            return result;
        }

        byte[] PatternFromSample(int x, int y) => Pattern((dx, dy) => sample[(x + dx) % smx, (y + dy) % smy]);
        byte[] Rotate(byte[] p) => Pattern((x, y) => p[n - 1 - y + x * n]);
        byte[] Reflect(byte[] p) => Pattern((x, y) => p[n - 1 - x + y * n]);

        long Index(byte[] p)
        {
            long result = 0, power = 1;
            for (int i = 0; i < p.Length; i++)
            {
                result += p[p.Length - 1 - i] * power;
                power *= c;
            }

            return result;
        }

        byte[] PatternFromIndex(long ind)
        {
            long residue = ind, power = w;
            byte[] result = new byte[n * n];

            for (int i = 0; i < result.Length; i++)
            {
                power /= c;
                int count = 0;

                while (residue >= power)
                {
                    residue -= power;
                    count++;
                }

                result[i] = (byte) count;
            }

            return result;
        }

        Dictionary<long, int> weights = new Dictionary<long, int>();
        List<long> ordering = new List<long>();

        for (int y = 0; y < (periodicInput ? smy : smy - n + 1); y++)
        for (int x = 0; x < (periodicInput ? smx : smx - n + 1); x++)
        {
            if (ordering.Count == 0)
            {
                Debug.Log($"{x} {y}");
            }
            byte[][] ps = new byte[8][];

            ps[0] = PatternFromSample(x, y);
            ps[1] = Reflect(ps[0]);
            ps[2] = Rotate(ps[0]);
            ps[3] = Reflect(ps[2]);
            ps[4] = Rotate(ps[2]);
            ps[5] = Reflect(ps[4]);
            ps[6] = Rotate(ps[4]);
            ps[7] = Reflect(ps[6]);

            for (int k = 0; k < symmetry; k++)
            {
                long ind = Index(ps[k]);
                if (weights.ContainsKey(ind)) weights[ind]++;
                else
                {
                    weights.Add(ind, 1);
                    ordering.Add(ind);
                }
            }
        }

        T = weights.Count;
        this._ground = (ground + T) % T;
        _patterns = new byte[T][];
        base.weights = new double[T];

        int counter = 0;
        foreach (long ww in ordering)
        {
            sb.AppendLine(ww.ToString());
            _patterns[counter] = PatternFromIndex(ww);
            base.weights[counter] = weights[ww];
            if (counter == 0)
            {
                
                Debug.Log($"{ww} {BitConverter.ToString(_patterns[counter])} {weights[ww]}");
            }
            counter++;
        }

        bool Agrees(byte[] p1, byte[] p2, int dx, int dy)
        {
            int xmin = dx < 0 ? 0 : dx, xmax = dx < 0 ? dx + n : n, ymin = dy < 0 ? 0 : dy, ymax = dy < 0 ? dy + n : n;
            for (int y = ymin; y < ymax; y++)
            for (int x = xmin; x < xmax; x++)
                if (p1[x + n * y] != p2[x - dx + n * (y - dy)])
                    return false;
            return true;
        }

        ;

        propagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            propagator[d] = new int[T][];
            for (int t = 0; t < T; t++)
            {
                List<int> list = new List<int>();
                for (int t2 = 0; t2 < T; t2++)
                    if (Agrees(_patterns[t], _patterns[t2], DX[d], DY[d]))
                        list.Add(t2);
                propagator[d][t] = new int[list.Count];
                for (int cc = 0; cc < list.Count; cc++) propagator[d][t][cc] = list[cc];
            }
        }
    }

    protected override bool OnBoundary(int x, int y) => !periodic && (x + _n > FMX || y + _n > FMY || x < 0 || y < 0);

    public override Texture2D Graphics()
    {
        Texture2D result = new Texture2D(FMX, FMY);
        Color[] bitmapData = new Color[result.height * result.width];

        if (observed != null)
        {
            for (int y = 0; y < FMY; y++)
            {
                int dy = y < FMY - _n + 1 ? 0 : _n - 1;
                for (int x = 0; x < FMX; x++)
                {
                    int dx = x < FMX - _n + 1 ? 0 : _n - 1;
                    Color c = _colors[_patterns[observed[x - dx + (y - dy) * FMX]][dx + dy * _n]];
                    bitmapData[x + (FMY - 1 - y) * FMX] = c; // unchecked((int)0xff000000 | (c.R << 16) | (c.G << 8) | c.B);
                }
            }
        }
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                int contributors = 0;
                float r = 0, g = 0, b = 0;
                int x = i % FMX, y = i / FMX;

                for (int dy = 0; dy < _n; dy++)
                for (int dx = 0; dx < _n; dx++)
                {
                    int sx = x - dx;
                    if (sx < 0) sx += FMX;

                    int sy = y - dy;
                    if (sy < 0) sy += FMY;

                    int s = sx + sy * FMX;
                    if (OnBoundary(sx, sy)) continue;
                    for (int t = 0; t < T; t++)
                        if (wave[s][t])
                        {
                            contributors++;
                            Color color = _colors[_patterns[t][dx + dy * _n]];
//                                Vector4 color = new Vector4(c.r / 255.0f, c.g / 255.0f, c.B / 255.0f, c.A / 255.0f);
                            r += color.r;
                            g += color.g;
                            b += color.b;
                        }
                }

                bitmapData[i] =
                    new Color(r / contributors, g / contributors,
                        b / contributors); // unchecked((int)0xff000000 | ((r / contributors) << 16) | ((g / contributors) << 8) | b / contributors);
            }
        }

        result.SetPixels(0, 0, result.width, result.height, bitmapData);
//        var bits = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
//        System.Runtime.InteropServices.Marshal.Copy(bitmapData, 0, bits.Scan0, bitmapData.Length);
//        result.UnlockBits(bits);

        return result;
    }

    protected override void Clear()
    {
        base.Clear();

        if (_ground != 0)
        {
            for (int x = 0; x < FMX; x++)
            {
                for (int t = 0; t < T; t++)
                    if (t != _ground)
                    {
//                        sb.AppendLine(t.ToString());
                        Ban(x + (FMY - 1) * FMX, t);
                    }
                for (int y = 0; y < FMY - 1; y++) Ban(x + y * FMX, _ground);
            }

            Propagate();
        }
        sb.AppendLine($"after ground {_ground} {entropies[0]} {sumsOfOnes[0]}");
    }
}