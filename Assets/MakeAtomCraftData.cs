using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class MakeAtomCraftData : MonoBehaviour
{
    //  キューに設定するカテゴリ名
    public string defaultGroupCategory = "";
    public int cuePriority = 64;
    //
    public float pos3dDistanceMin = 10.0f;
    public float pos3dDistanceMax = 50.0f;
    public float pos3dDopplerCoefficient = 0.0f;

    public void Make(string outputPath, string workunitName, List<UTAU2ADX2LE.Adx2CueSheet> cueSheetList, List<string> fileList, string srcMaterialsFolder)
    {
        #region WorkUnitNameの名前修正
        workunitName = ChangeName(workunitName);
        #endregion

        MakeWorkUnit(outputPath, workunitName, cueSheetList, fileList);
        CopyMaterialsFolder(outputPath, workunitName, fileList.ToArray(), srcMaterialsFolder);

        Debug.Log("<color=orange>Make Atom Craft Data Finish!</color> " + outputPath);
    }

    void MakeWorkUnit(string outputPath, string workunitName, List<UTAU2ADX2LE.Adx2CueSheet> cueSheetList, List<string> wavList)
    {
        //  情報ファイル作成
        MakeAtmcunit(outputPath, workunitName, cueSheetList);
        MakeMaterialinfo(outputPath, workunitName, wavList);
    }

    void MakeAtmcunit(string outputPath, string workunitName, List<UTAU2ADX2LE.Adx2CueSheet> cueSheetList)
    {
        string filePath = outputPath + "/" + workunitName + "/" + workunitName + ".atmcunit";

        Debug.Log("Make Workunit FilePath: " + filePath);

        if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        StreamWriter sw;
        FileInfo fi;
        fi = new FileInfo(filePath);
        sw = fi.AppendText();

        sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sw.WriteLine("<!-- Orca XML File Format -->");
        sw.WriteLine("<OrcaTrees ObjectTypeExpression=\"Full\" BinaryEncodingType=\"Base64\" FileVersion=\"3\" FileRevision=\"0\">");
        sw.WriteLine("  <OrcaTree OrcaName=\"(no name)\">");
        sw.WriteLine("    <Orca OrcaName=\"" + workunitName + "\" VersionInfo=\"Ver.2.19.02\" FormatVersion=\"Ver.1.00.04\" WorkUnitPath=\"WorkUnits/" + workunitName + "/" + workunitName + ".atmcunit\" UsedMaterialFlag=\"True\" Expand=\"True\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoWorkUnit\">");         
        sw.WriteLine("      <Orca OrcaName=\"References\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoReferenceFolder\">");
        sw.WriteLine("        <Orca OrcaName=\"AISAC\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoReferenceAisacFolder\" />");
        sw.WriteLine("      </Orca>");
        sw.WriteLine("      <Orca OrcaName=\"CueSheetFolder\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSheetFolder\">");
        sw.WriteLine("        <Orca OrcaName=\"" + workunitName + "\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSheetSubFolder\">");

        foreach (var cuesheet in cueSheetList)
        {
            this.CreateCueSheetXML(sw, workunitName, cuesheet);
        }

        sw.WriteLine("        </Orca>");
        sw.WriteLine("      </Orca>");
        sw.WriteLine("    </Orca>");
        sw.WriteLine("  </OrcaTree>");
        sw.WriteLine("</OrcaTrees>");
        sw.WriteLine("<!-- Copyright (c) CRI Middleware Co.,LTD. -->");
        sw.WriteLine("<!-- end of document -->");

        sw.Flush();
        sw.Close();

    }

    class Adx2Track
    {
        public string name;
        public string materialName;
        public bool loopFlag;
        public int pitch;
        public int pan;
    };

    void CreateCueSheetXML(StreamWriter sw, string workunitName, UTAU2ADX2LE.Adx2CueSheet cueSheet)
    {
        Guid guid = Guid.NewGuid();

        string cueSheetName = ChangeName(cueSheet.cueSheetName);

        sw.WriteLine("          <Orca OrcaName=\"" + cueSheetName + "\" Expand=\"True\" OoUniqId=\"" + guid.ToString()
            + "\" CueSheetPaddingSize=\"2048\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSheet\">");

     
        int cueId = 0;
       
        {
            List<string> cueNameList = new List<string>();
            #region track情報収集
            foreach (var zone in cueSheet.zoneList)
            {
                List<Adx2Track> trackList = new List<Adx2Track>();
                //if (zone.keyLow <= noteNo && noteNo <= zone.keyHi)
                {
                    Adx2Track track = new Adx2Track();
                    track.name = zone.name; 
                    track.materialName = Path.GetFileNameWithoutExtension( zone.sampleFileName);
                    //track.loopFlag = (zone.sampleMode == 1) ? true : false;
                    track.pitch = (int)((zone.rootKey) * 100f);
                    track.pan = (zone.pan / 500 * 30);
                    trackList.Add(track);
                }

                if (trackList.Count > 0)
                {
                    //Debug.Log("track Num Name " + trackList.Count);

                    string cueName = zone.name;

                    if(cueNameList.Contains(cueName) == false){

                        this.CreateCueXML(sw, workunitName, cueName, cueId, trackList);

                        cueId++;

                        cueNameList.Add(cueName);
                    } else {

                        DebugWrite.DebugWriteText("Same Name Cue: " + cueName);
                        Debug.Log("<color=red>Same Name Cue: </color> " + cueName);
                        
                    }
                }
            }
            #endregion

        }
        //  --------------------

        sw.WriteLine("          </Orca>");


    }

    void CreateCueXML(StreamWriter sw, string workunitName, string cueName, int cueId, List<Adx2Track> trackList)
    {


        string cueString = "            <Orca OrcaName=\"" + cueName + "\" SynthType=\"SynthPolyphonic\" CueID=\"" + cueId.ToString() + "\" ";
        if (defaultGroupCategory != String.Empty)
        {
            //  CategoryGroup/CategoryName
            //  Category0="/CriAtomCraftV2Root/GlobalSettings/Categories/CategoryGroup_0/Category_0"
            cueString += "Category0=\"/CriAtomCraftV2Root/GlobalSettings/Categories/CategoryGroup_0/";
            cueString += defaultGroupCategory;
            cueString += "\" ";
        }
        cueString += "Pos3dDistanceMin=\"" + pos3dDistanceMin + "\" Pos3dDistanceMax=\"" + pos3dDistanceMax + "\" "; 
        cueString += "Pos3dDopplerCoefficient=\"" + pos3dDopplerCoefficient + "\" ";

        cueString += "CuePriority=\"" + cuePriority + "\" ";

        cueString += "DisplayUnit=\"Frame5994\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSynthCue\">";

        sw.WriteLine(cueString);

        foreach (var track in trackList)
        {

            this.CreateTrackXML(sw, workunitName, track.materialName, track.loopFlag, track.pitch, track.pan);
        }

        sw.WriteLine("            </Orca>");

    }

    void CreateTrackXML(StreamWriter sw, string workunitName, string materialName, bool loopFlag, int pitch, int pan)
    {
        sw.WriteLine("              <Orca OrcaName=\"Track_" + materialName + "\" SynthType=\"Track\" Pitch=\"" + pitch + "\" SwitchRange=\"0.5\" DisplayUnit=\"Frame5994\" ObjectColor=\"30, 200, 100, 180\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSynthTrack\">");

        string acOoCueSynthWaveformStr = "                <Orca OrcaName=\"" + materialName + ".wav\" ";

        if (loopFlag == false)
        {
            //  ループ無効化
            acOoCueSynthWaveformStr += "IgnoreLoop=\"True\" ";
        }

        acOoCueSynthWaveformStr += "Pan3dAngle=\"" + pan.ToString() + "\" ";

        acOoCueSynthWaveformStr += "LinkWaveform=\"/MaterialRootFolder/" + materialName + ".wav\" "
        + " PanType=\"Auto\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoCueSynthWaveform\" />";
        
        sw.WriteLine(acOoCueSynthWaveformStr);

        sw.WriteLine("              </Orca>");
    }

    void MakeMaterialinfo(string outputPath, string workunitName, List<string> wavList)
    {
        string filePath = outputPath + "/" + workunitName + "/" + workunitName + ".materialinfo";

        Debug.Log("Make MaterialInfo FilePath: " + filePath);

        if (Directory.Exists(Path.GetDirectoryName(filePath)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        StreamWriter sw;
        FileInfo fi;
        fi = new FileInfo(filePath);
        sw = fi.AppendText();

        sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sw.WriteLine("<!-- Orca XML File Format -->");
        sw.WriteLine("<OrcaTrees ObjectTypeExpression=\"Full\" BinaryEncodingType=\"Base64\" FileVersion=\"3\" FileRevision=\"0\">");
        sw.WriteLine("  <OrcaTree OrcaName=\"(no name)\">");
        sw.WriteLine("    <Orca OrcaName=\"WorkUnit_" + workunitName + "_MaterialInfo\" VersionInfo=\"Ver.2.19.02\" FormatVersion=\"Ver.1.00.02\" MaterialInfoPath=\"\" MaterialRootPath=\"Materials\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoMaterialInfoFile\">");
        sw.WriteLine("      <Orca OrcaName=\"MaterialRootFolder\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoWaveformFolder\">");

        //  ----- WAV ------

        foreach (string wavName in wavList)
        {
            sw.WriteLine("        <Orca OrcaName=\"" + Path.GetFileName(wavName) + "\" OrcaType=\"CriMw.CriAtomCraft.AcCore.AcOoWaveform\" />");
        }
        //  --------------------

        sw.WriteLine("      </Orca>");
        sw.WriteLine("    </Orca>");
        sw.WriteLine("  </OrcaTree>");
        sw.WriteLine("</OrcaTrees>");
        sw.WriteLine("<!-- Copyright (c) CRI Middleware Co.,LTD. -->");
        sw.WriteLine("<!-- end of document -->");

        sw.Flush();
        sw.Close();
    }

    //  波形コピー
    void CopyMaterialsFolder(string outputPath, string workunitName, string[] fileList, string srcMaterialsPath)
    {
        string filePath_materials = outputPath + "/" + workunitName + "/Materials";

        Debug.Log("Copy MaterialFolder Path: " + srcMaterialsPath + " -> " + filePath_materials);

        if (Directory.Exists(Path.GetDirectoryName(filePath_materials)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath_materials));
        }

        if (Directory.Exists(filePath_materials) == false)
        {
            Directory.CreateDirectory(filePath_materials);
        }

        foreach (string tmpFile in fileList)
        {
            if (Path.GetExtension(tmpFile) == ".wav")
            {

                string filePath = filePath_materials + "/" + Path.GetFileName(tmpFile);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                File.Copy(tmpFile, filePath);
            }
        }
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
