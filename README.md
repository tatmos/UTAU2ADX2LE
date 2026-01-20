# UTAU2ADX2LE

## 概要
UTAU音源フォルダから `oto.ini` と `*_wav.frq` を読み取り、CRI Atom Craft (ADX2LE) 用の WorkUnit / CueSheet と波形素材を生成する Unity スクリプト群です。UTAU のループ情報をもとに波形を再構成し、ADX2LE で使えるループ付き WAV と `.atmcunit` / `.materialinfo` を出力します。オリジナルの開発メモは Qiita にあります。  
http://qiita.com/tatmos/items/491dadbc77ad929396be

## 構成
- `Assets/UTAU2ADX2LE.cs`
  - UTAU 音源フォルダを走査し、`oto.ini` と `*_wav.frq` から音素情報・ループ・周波数を読み取るメイン処理。
  - ループ情報をもとに WAV を生成し、Cue/Zone 情報を組み立てます。
- `Assets/MakeWave.cs`
  - WAV 読み取り/書き込みと、ループ付き WAV (smpl チャンク) を生成する処理。
- `Assets/MakeAtomCraftData.cs`
  - CueSheet / WorkUnit (`.atmcunit`, `.materialinfo`) の XML を生成し、Materials へ WAV をコピーします。
- `Assets/DebugWrite.cs`
  - 変換ログを `output_wav/__debug.txt` に出力します。
- `Assets/test.unity`
  - 動作確認用のシーン。

## 使い方
1. Unity で本プロジェクトを開きます。
2. `Assets/test.unity` を開き、空の GameObject に `UTAU2ADX2LE` をアタッチします。
3. `UTAU2ADX2LE.cs` の `inputUtauPath` を、UTAU 音源フォルダへの相対パスに設定します。  
   例: `UTAU/重音テト音声ライブラリー`（プロジェクト直下の `UTAU` フォルダを想定）
4. UTAU 音源フォルダに `oto.ini` と `*_wav.frq`、対応する `.wav` が揃っていることを確認します。
5. Play を実行すると、プロジェクト直下の `output_wav/` に以下が生成されます。
   - ループ加工済み WAV
   - `WorkUnits/<音源名>/<音源名>.atmcunit`
   - `WorkUnits/<音源名>/<音源名>.materialinfo`
   - `WorkUnits/<音源名>/Materials/`（WAV コピー）

## 主な機能
- UTAU 音源の `oto.ini` と `*_wav.frq` を解析し、音素名・ループ区間・周波数を取得。
- ループ区間が長い場合はクロスフェード処理を行い、短い場合は単純ループで WAV を生成。
- 取得した周波数から MIDI ノート番号を計算し、ADX2LE の Cue/Track ピッチに反映。
- WorkUnit / CueSheet / Materials を一括生成し、ADX2LE へインポートしやすい形で出力。
