# SushidaAutoTyper Pro 🍣⚡

[![Release](https://img.shields.io/github/v/release/youyouboydragonOfficial/SushidaAutoTyperPro?color=00E5FF)](https://github.com/youyouboydragonOfficial/SushidaAutoTyperPro/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/youyouboydragonOfficial/SushidaAutoTyperPro)
[![Framework](https://img.shields.io/badge/.NET-10.0%20WPF-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

寿司打（Sushida）をはじめとする、各種タイピングゲーム・Unity製タイピングゲーム・ブラウザゲームに対応した **Windows用 超高速自動タイピングアプリケーション** です。

---

## 🔥 主な特徴 (Features)

1. **画面リアルタイムOCR自動連打モード (Screen OCR Auto Mode)**
   - マウスドラッグで寿司打の文字表示枠を簡単に選択（スナイピングオーバーレイ）。
   - 画像前処理（コントラスト自動増幅・二値化・拡大スケール）により、誤認識・文字化けを完全防止。
   - Windows 10/11 組み込みのオフライン `Windows.Media.Ocr` エンジンにより超高速認識。

2. **Unity / DirectInput / Hardware ScanCode 完全対応**
   - Unity WebGL・Unity PCビルド・Chromium Canvas等のゲームエンジンで一般的なキー入力が無効化される問題をクリア。
   - Win32 `SendInput` の **ハードウェアスキャンコード (ScanCode)** モードを標準搭載し、100%のゲーム互換性を実現。

3. **超高速テキスト＆カスタマイズ連打モード**
   - 任意のテキストを入力・貼り付けして最大 10,000+ CPM（打鍵/分）で一瞬送信。
   - キー入力間隔（0ms〜500ms）、人間らしい揺らぎ（ランダムディレイ）調整可能。

4. **寿司打ローマ字変換・最適化エンジン (Romaji Converter)**
   - ひらがな文を自動的にローマ字へ一発変換。
   - `し`(shi/si), `ふ`(fu/hu), `じ`(ji/zi), `ん`(n/nn) などの揺らぎに対応。

5. **グローバルホットキー＆安全設計**
   - **`F8`**: タイピング開始 (Start)
   - **`F9`**: 一時停止 / 再開 (Pause/Resume)
   - **`F10` または `Esc`**: 緊急停止 (Emergency Stop)
   - ブラウザやゲームがアクティブな状態でもグローバルに判定。

---

## 🚀 使い方 (Quick Start)

### インストール不要！ダウンロードしてすぐ使えます
1. [GitHub Releases](https://github.com/youyouboydragonOfficial/SushidaAutoTyperPro/releases) から最新の `SushidaAutoTyperPro-v1.0.0-win-x64.zip` または `SushidaAutoTyper.exe` をダウンロードします。
2. 解凍して `SushidaAutoTyper.exe` を起動します。

### 寿司打での使用手順
1. ブラウザで [寿司打](http://typingx0.net/sushida/) を開きます。
2. アプリ画面の **「🎯 領域を選択」** ボタンをクリックします。
3. 画面が半透明になったら、寿司打でローマ字が表示される位置をマウスドラッグで囲みます。
4. 寿司打のゲームを開始し、**`[F8]`** キーを押すと自動タイピングが始まります！
5. **`[F9]`** で一時停止、**`[F10]`** でいつでも安全停止できます。

---

## ⚙️ システム要件 (System Requirements)

- OS: Windows 10 (19041以上) / Windows 11
- アーキテクチャ: x64
- ランタイム依存なし（単体実行ファイル `.exe` / Self-Contained）

---

## 📜 ライセンス (License)

MIT License © 2026 youyouboydragonOfficial
