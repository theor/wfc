using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

class Integration
{
    [MenuItem("WFC/All")]
    static void Main()
    {
        Stopwatch sw = Stopwatch.StartNew();

        Random random = new Random();
        XDocument xdoc = XDocument.Load("C:\\Users\\theor\\Downloads\\WaveFunctionCollapse-master\\samples.xml");

        int counter = 1;
        foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
        {
            Model model;
            string name = xelem.Get<string>("name");
            Debug.Log($"< {name}");

            if (xelem.Name == "overlapping") model = new OverlappingModel(name, xelem.Get("N", 2), xelem.Get("width", 48), xelem.Get("height", 48),
                xelem.Get("periodicInput", true), xelem.Get("periodic", false), xelem.Get("symmetry", 8), xelem.Get("ground", 0));
//            else if (xelem.Name == "simpletiled")
//                model = new SimpleTiledModel(name, xelem.Get<string>("subset"),
//                xelem.Get("width", 10), xelem.Get("height", 10), xelem.Get("periodic", false), xelem.Get("black", false));
            else continue;

            for (int i = 0; i < xelem.Get("screenshots", 2); i++)
            {
                for (int k = 0; k < 10; k++)
                {
                    Console.Write("> ");
                    int seed = 42;// random.Next();
                    bool finished = model.Run(seed, xelem.Get("limit", 0));
                    if (finished)
                    {
                        Debug.Log("DONE");

                        var t = model.Graphics();
                        File.WriteAllBytes($"Assets\\Output\\{counter} {name} {i}.png", t.EncodeToPNG());
//                        if (model is SimpleTiledModel && xelem.Get("textOutput", false))
//                            System.IO.File.WriteAllText($"output\\{counter} {name} {i}.txt", (model as SimpleTiledModel).TextOutput());

                        break;
                    }
                    else Debug.Log("CONTRADICTION");
                }
            }

            counter++;
        }

        Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
        AssetDatabase.Refresh();
    }

    [MenuItem("WFC/Run")]
    static void F()
    {
        Utils.ClearLogConsole();
//        Random.InitState(Time.tim);
        bool finished = false;
        OverlappingModel model = null;
        var name = "Dungeon";
//        var name = "Flowers";

        for (int i = 0; i < 1 && !finished; i++)
        {
//            model = new OverlappingModel(name, 3, 48, 48, true, true, 2, -4);
            model = new OverlappingModel(name, 3, 48, 48, true, true, 8, 0);
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

    private static class Utils
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
}