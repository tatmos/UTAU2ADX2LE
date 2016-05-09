using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

#region DebugText

public class DebugWrite
{
    static string outputpath = "output_wav";

    public static void DebugWriteTextReset(string _path)
    {
        outputpath = _path;
        string path = outputpath + "/" + "__debug.txt";

        if (Directory.Exists(Path.GetDirectoryName(path)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
        }


        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void DebugWriteText(string str)
    {
        WriteText(outputpath + "/" + "__debug.txt", str);
    }

    public static void WriteText(string path, string str)
    {
        StreamWriter writer = 
            new StreamWriter(path, true);

        writer.WriteLine(str);
        writer.Close();

        Debug.Log(str);

    }
}
#endregion