using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;


public class UTAU2ADX2LE : MonoBehaviour
{


    public class Zone
    {
        public string name = "";
        public int sampleId = -1;
        public int keyLow = 0;
        public int keyHi = 127;
        //  sampleMode(Loop)
        public int sampleMode = 0;
        public float rootKey = 0;
        public int pan = 0;

        public override string ToString()
        {
            return string.Format("\"{6}\" key:{0:X2}-{1:X2} sampleId:\"{2}\" lp:\"{3}\" key:\"{4}\" pan:\"{5}\"", keyLow, keyHi, sampleId, sampleMode, rootKey, pan, name);
        }

        public string sampleFileName = "";
    }

    #region ADX2LE

    public class Adx2CueSheet
    {
        public string cueSheetName = "";
        public List<Zone> zoneList = new List<Zone>();
    }

    #endregion

    #region UTAU

    public class UTAUNote
    {
        public string waveName = "";
        public string name = "";
        public int loopStart = 0;
        public int loopEnd = 0;

        public override string ToString()
        {
            return string.Format("\"{0}\" \"{1}\" lp:\"({2}-{3})\" freq:\"{4}({5})\"", waveName, name, loopStart, loopEnd, frquency, midiNoteNo);
        }

        public string wavePath = "";
        public double frquency = 0;
        public float midiNoteNo = 0;
    }

    #endregion


    List<UTAUNote> utauNoteList = new List<UTAUNote>();


    public string inputUtauPath = "UTAU/重音テト音声ライブラリー";
    string outputpath = "output_wav";
    // Use this for initialization
    void Start()
    {
        DebugWrite.DebugWriteTextReset(Path.GetDirectoryName(Application.dataPath) + "/" + "output_wav/");

        string inputPath = Path.GetDirectoryName(Application.dataPath) + "/" + inputUtauPath;

        #region UTAUフォルダー読む
        ReadUtau(inputPath);
        #endregion

        #region 波形情報を埋める
        for (int i = 0; i < utauNoteList.Count; i++)
        {
            UTAUNote utauNote = utauNoteList [i];
            ReadFrq(ref utauNote);
        }
        #endregion

        List<string> wavefilePathList = new List<string>();
        #region 波形生成

        MakeWave makewave = this.gameObject.AddComponent<MakeWave>();
        for (int utauNoteNo = 0; utauNoteNo < utauNoteList.Count; utauNoteNo++)
        {
            UTAUNote utauNote = utauNoteList [utauNoteNo];

            DebugWrite.DebugWriteText(utauNote.ToString());

            byte[] src_samples = new byte[4];
            uint sampleRate = 44100;
            makewave.ReadWaveFile(utauNote.wavePath, ref src_samples, ref sampleRate);

            if (wavefilePathList.Contains(utauNote.wavePath) == false)
            {
                string waveOutPath = Path.GetDirectoryName(Application.dataPath) + "/" + "output_wav/"
                                     + Path.GetFileName(utauNote.wavePath);
                
                wavefilePathList.Add(waveOutPath);

                if (utauNote.loopEnd > utauNote.loopStart)
                {
                    //  loopの場合 秒からサンプル数に変換
                    uint loopPre = (uint)utauNote.loopStart * (uint)((float)sampleRate / 1000f);
                    uint loopLength = (uint)(utauNote.loopEnd - utauNote.loopStart) * (uint)((float)sampleRate / 1000f);
                    uint loopPost = (uint)utauNote.loopEnd * (uint)((float)sampleRate / 1000f);

                    uint xfadeLength = 11025;
                    if (loopLength > (xfadeLength * 2))
                    {
                        //  xfade分右へ
                        uint loopPre2 = loopPre + xfadeLength;
                        //  xfade分減らす
                        uint loopLength2 = loopLength - xfadeLength;

                        uint loopPost2 = loopPre2 + loopLength2;

                        //xfade
                        byte[] xfade = new byte[loopLength2 * 2];
                        {
                            //  Loop部分
                            System.Array.Copy(src_samples, (loopPre2) * 2, xfade, 0, (loopLength2 - xfadeLength) * 2);

                            //  Loop後半xfade処理
                            uint xfadeStart = loopLength2 - xfadeLength;

                            for (uint i = 0; i < xfadeLength * 2; i += 2)
                            {
                                Int16 src_in = BitConverter.ToInt16(xfade, (int)i);
                                Int16 src_out = BitConverter.ToInt16(src_samples, (int)(i + ((loopPost2 - xfadeLength) * 2)));

                                Int16 value_in = (Int16)(((float)(i) / (xfadeLength * 2)) * (float)src_in * 1.0f);
                                Int16 value_out = (Int16)((1f - (float)i / (xfadeLength * 2)) * (float)src_out * 1.0f);

                                xfade [xfadeStart * 2 + i + 0] = (byte)(value_in + value_out);
                                xfade [xfadeStart * 2 + i + 1] = (byte)((value_in + value_out) >> 8);

                                //xfade [xfadeStart * 2 + i + 0] = (byte)(  value_in);
                                //xfade [xfadeStart * 2 + i + 1] = (byte)(( value_in) >> 8);
                            }
                        }


                        int loopMax = 32;
                        byte[] samples = new byte[(loopPre2 + loopLength2 * loopMax + loopPost2) * 2];

                        System.Array.Copy(src_samples, 0, samples, 0, (loopPre2) * 2);

                        for (int loop = 0; loop < loopMax; loop++)
                        {
                            System.Array.Copy(xfade, 0, samples, (loopPre2) * 2 + (loopLength2) * 2 * loop, (loopLength2) * 2);
                        }


                        makewave.WriteWaveFile(waveOutPath, samples, sampleRate,
                            (uint)(loopPre2), 
                            (uint)(loopPre2 + loopLength2 * loopMax));

                    } else
                    {
                        //  loop区間が短い場合は単純ループ（ノイズになりやすい）

                        int loopMax = 32;
                        byte[] samples = new byte[(loopPre + loopLength * loopMax + loopPost) * 2];

                        System.Array.Copy(src_samples, 0, samples, 0, (loopPre) * 2);
                        for (int loop = 0; loop < loopMax; loop++)
                        {
                            System.Array.Copy(src_samples, loopPre * 2, samples, (loopPre) * 2 + (loopLength) * 2 * loop, (loopLength) * 2);
                        }

                        System.Array.Copy(src_samples, loopPost * 2, samples, (loopPre) * 2 + (loopLength) * 2 * loopMax, loopPost * 2);


                        makewave.WriteWaveFile(waveOutPath, samples, sampleRate,
                            (uint)(loopPre), 
                            (uint)(loopPre + loopLength * loopMax));

                    }
                    
                } else
                {
                    makewave.WriteWaveFile(waveOutPath, src_samples, sampleRate, 0, 0);
                }
            }
        }

        #endregion

        string workUnitName = ChangeName(Path.GetFileName(inputPath));
        #region Zone作成


        List<Adx2CueSheet> cueSheetList = new List<Adx2CueSheet>();
        Adx2CueSheet newCueSheet = new Adx2CueSheet();
        newCueSheet.cueSheetName = workUnitName;

        for (int utauNoteNo = 0; utauNoteNo < utauNoteList.Count; utauNoteNo++)
        {
            UTAUNote utauNote = utauNoteList [utauNoteNo];

            Zone zone = new Zone();

            zone.name = utauNote.name;
            zone.rootKey = 60 - utauNote.midiNoteNo;
            zone.sampleFileName = utauNote.waveName;

            newCueSheet.zoneList.Add(zone);
        }

        cueSheetList.Add(newCueSheet);

        #endregion

        #region ワークユニット作成

        MakeAtomCraftData makeAtomCraftData = this.gameObject.AddComponent<MakeAtomCraftData>();

        string matelialsPath = Path.GetDirectoryName(Application.dataPath) + "/" + outputpath;

        makeAtomCraftData.Make(matelialsPath, workUnitName, cueSheetList, wavefilePathList, matelialsPath);

        #endregion
    }

    void ReadUtau(string path)
    {
        DebugWrite.DebugWriteText("read " + path);

        if (System.IO.Directory.Exists(path) == false)
        {
            Debug.LogError("Not Read File Path : " + path);
            return;
        }

        {
            string[] files = System.IO.Directory.GetFiles(path, "oto.ini", System.IO.SearchOption.AllDirectories);
            if (files != null)
            {
                foreach (var file in files)
                {
                    ReadotoIni(file);
                }
            }
        }

        //  ディレクトリを得る
        string[] subFolders = System.IO.Directory.GetDirectories(path, "*", System.IO.SearchOption.AllDirectories);
        if (subFolders == null)
        {
            Debug.LogError("Not Read File : " + path);
            return;
        }

        foreach (var subFolder in subFolders)
        {
            string[] files = System.IO.Directory.GetFiles(subFolder, "oto.ini", System.IO.SearchOption.AllDirectories);
            if (files != null)
            {
                foreach (var file in files)
                {
                    ReadotoIni(file);
                }
            }
        }

    }

    void ReadotoIni(string path)
    {

        StreamReader file = new StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"));
        string line;

        while ((line = file.ReadLine()) != null)
        {
            UTAUNote utauNote = new UTAUNote();

            string[] strArry = line.Split('=');
            utauNote.waveName = strArry [0];

            utauNote.wavePath = Path.GetDirectoryName(path) + "/" + utauNote.waveName;

            string[] strArry2 = strArry [1].Split(',');

            utauNote.name = strArry2 [0];

            if (utauNote.name == "")
            {
                //  空の時はwav名
                utauNote.name = Path.GetFileNameWithoutExtension(utauNote.waveName);
            }


            if (strArry2 [1] != "" && utauNote.name != "")
            {
                utauNote.loopStart = int.Parse(strArry2 [1]) + int.Parse(strArry2 [4]);
                utauNote.loopEnd = int.Parse(strArry2 [1]) + int.Parse(strArry2 [5]);
         
                //DebugWriteText(utauNote.ToString());
                utauNoteList.Add(utauNote);
            } else
            {
                DebugWrite.DebugWriteText("unkown line " + line);
                Debug.Log("<color=red>unkown line </color> " + line);
            }
        }
            
        file.Close();

        Debug.Log("Read End");
    }

    void ReadFrq(ref UTAUNote utauNote)
    {
        if (System.IO.File.Exists(utauNote.wavePath) == false)
        {

            DebugWrite.DebugWriteText("Not Read File : " + utauNote.wavePath);
            Debug.Log("<color=red>Not Read File </color> " + utauNote.wavePath);

            return;
        }

        string frqPath = Path.GetDirectoryName(utauNote.wavePath) + "/" + Path.GetFileNameWithoutExtension(utauNote.wavePath) + "_wav.frq";

        if (System.IO.File.Exists(frqPath) == false)
        {
            Debug.LogError("Not Read File : " + frqPath);
            return;
        }


        FileStream fs = new FileStream(frqPath, FileMode.Open, FileAccess.Read);
        //int fileSize = (int)fs.Length; // ファイルのサイズ

        byte[] buf = new byte[8];
        {
            fs.Read(buf, 0, 4);
            fs.Read(buf, 0, 4);
            fs.Read(buf, 0, 4);
            fs.Read(buf, 0, 8);
            //DebugWriteText(string.Format("ReadFrq:{0:X2}{1:X2}{2:X2}{3:X2}", buf [0], buf [1], buf [2], buf [3]));
            utauNote.frquency = BitConverter.ToDouble(buf, 0);
            utauNote.midiNoteNo = Ftom((float)utauNote.frquency);

            //DebugWriteText(utauNote.ToString());
        }

        fs.Dispose();

        //Debug.Log("Read End");

    }

    public float Ftom(float freq)
    {
        return (69f + (1f / .057762265f) * Mathf.Log(freq / 440f));
    }

    string ChangeName(string name)
    {
        name = name.Replace('(', '_');
        name = name.Replace(')', '_');
        name = name.Replace('&', '_');
        name = name.Replace(':', '_');
        name = name.Replace('/', '_');
        name = name.Replace('.', '_');
        name = name.Replace(' ', '_');
        return name;
    }

}
