# VR化の検討（日本語化フォーク独自の計画）

状態: Windows向けVR試作ビルドまで作成済み。Unity 6000.6.0f1でC#コンパイル成功、Windows開発ビルド成功（エラー0）。**HMDへの左右眼描画、追跡精度、操作の実機検証は未実施**。

### 現在の試験ビルド

- ローカル: `Build-vr-prototype/UR.exe`。配布用Releaseとは別の開発ビルドで、原作ゲームデータは含まない。
- XR Plug-in Management 4.7.0 / OpenXR 1.17.1を解決・導入済み。Windows Standalone向けOpenXR Loaderを有効化。Oculus Touchプロファイルを有効化。
- 起動時のVR初期化は自動ではなく、`UR.exe -vr` と指定した場合にだけ試みる。通常の`UR.exe`はこれまでどおりのデスクトップモード。
- HMDが認識できた場合はプレイヤーカメラに頭部追跡の試作を取り付ける。ただし、メニューやHUDはまだVR用に移植されておらず、HMDで通常プレイできる段階ではない。
- Unityのコマンドラインビルドでは `[UWVR] Build result: Succeeded; errors: 0` を確認。HMDへの立体描画が成功したことを意味しない。

### 実機テストの方法（開発中）

1. WindowsのOpenXRランタイムを使用するVR接続（Quest + Virtual Desktopなど）を準備する。
2. `Build-vr-prototype/LaunchVR.bat`、または `UR.exe -vr` で起動する。ゲーム本体の正規データは従来と同様にユーザー自身で指定する。
3. `VR_Test.log` に `[UWVR] OpenXR subsystems started` が出たか、HMDに左右眼の描画があるか、頭部回転に追随するかを確認する。
4. 画面固定のメニュー・UI、VRコントローラー、酔い対策は未対応。検証中は既存のパッド・キーボード操作を想定する。

## 2026-10-13 初期実装の追加

### Unity読込時間に関する追跡結果

- 当初の「Initial Asset Database Refreshで停止」という判断は、待ち時間を十分に取っておらず早計だった。
- 22:34に開始した再検証では `Logs/upm.log` にOpenXR 1.17.1（27.3 MB）の取得が記録され、約65秒かかっている。Unity本体のログが更新されなくても、Package ManagerのCPU使用・キャッシュ展開が継続した。
- `com.unity.xr.openxr` の展開後も、既存のUniversal Render Pipelineなど数千ファイルからなるパッケージが順次展開されていた。**パッケージ処理中のCPU使用・ファイル更新を見ずにUnityを停止扱いしない。**
- パッケージ解決は最終的に `project:resolve-packages --> 200 (1233495 ms)`（約20分33秒）で完了した。Unityのコンパイル・Windows試作ビルドもその後成功した。

- 起動引数 `-vr` を指定した場合だけ、XR Plug-in ManagementからOpenXRのロード・起動を試みる。通常起動はOpenXRを初期化しない。
- ユーザーのプレイヤーカメラに `UWVRHeadTracking` を取り付け、HMDの頭部回転・移動差分を反映する仕組みを追加。初回の頭部位置を基準として既存の視点高さを保つ暫定方式。
- `Underworld > VR > Configure Windows OpenXR` （バッチ実行: `-executeMethod UWVRSetup.ConfigureWindowsOpenXR`）でWindowsのOpenXR設定アセットを生成・登録する機能を追加。設定はXR自動起動をOFFにし、デスクトップ通常起動への影響を抑える。
- `Underworld > VR > Build Windows Prototype` （バッチ実行: `-executeMethod UWVRBuild.BuildWindowsPrototype`）を追加。公開用ZIPには触れず、開発用の `Build-vr-prototype/UR.exe` を作成する。2026-10-13に**エラー0でビルド成功**。
- UnityのコンパイルとOpenXR設定・Windowsビルドは検証済み。**実際のOpenXR実行、立体視、頭部追跡は実機検証待ち。**

## 最初の実装（2026-10-13）

## OpenXRパッケージ導入段階

- Unity Editorメニュー `Underworld > VR > Report Configuration` を追加。コマンドラインでは `-executeMethod UWVRDiagnostics.ReportConfiguration` でXR管理とOpenXRアセンブリの読込状態をログに記録できる。
- 最初の自動検証はパッケージ解決処理に長時間を要したが、後続の継続観測により約20分33秒後の正常完了とOpenXRアセンブリの読み込み成功を確認した。

- Unity 6000.6用XR Plug-in Management 4.7.0とOpenXR Plugin 1.17.1を`Packages/manifest.json`に追加。
- Windows Standalone向けOpenXRプロバイダーはUnityのXR Management API経由で有効化済み。対象HMDのProject Validationと左右眼描画確認が次の工程。
- 設定アセットは手書きではなく公式のUnity Editor APIで生成した。

- `-vr` オプションだけで動作する `UWVRBootstrap` を追加。XRを設定していない環境では既存の通常画面を変更しない。
- HMDがXRランタイムによって有効と判断された場合、従来の `UWPlayerLook` が追跡回転を上書きしないよう抑制する。
- **OpenXRパッケージ導入とWindowsローダー有効化は済み。左右眼描画とHMD実機検証はまだ未完了。**

## 現在の優先順位

1. 日本語訳の追加作業は一時停止する（既存の翻訳・修正は保持）。
2. Windows PC接続型VR（Quest + Virtual Desktop等）で動く最小構成を検証する。
3. 実機で視点・立体視・入力が正常に動いてからUIと演出を移植する。
4. 日本語訳の残作業はVR検証の後に再開する。

## ソースコード調査による見立て

- 本作はUnityで3Dワールドを描画しており、VR描画の基礎がある。
- `Packages/manifest.json` にOpenXR / XR Plug-in Managementを追加し、Windows向けの設定アセットも生成済み。
- `UWPlayerLook.cs` はプレイヤーの向きとカメラの上下視点をマウス・パッド入力から制御する。HMD追跡による回転と混線させない変更が必要。
- `Interaction.cs` は多数の箇所で `Camera.main.ScreenPointToRay` を使用する。VRでは照準と操作のRayをHMD中央／コントローラーから取得する仕組みを分離する必要がある。
- `UWScreenUi.cs` は `ScreenSpaceOverlay` の画面を作る。VRのメニュー・字幕・会話・HUDはワールド空間UI、またはVR用の別UIとして表示する必要がある。
- `UWViewpoint.cs` にオリジナル座標とUnity空間の換算が集約されている。移動判定と視点のスケール合わせを個別に検証する。

## 段階的な実装・検証

1. 既存の通常画面モードを保持したまま、起動オプションでVRを選択可能にする。
2. Unity OpenXRを導入し、左右眼レンダリングとHMD追跡を確認する。
3. プレイヤー移動とHMD頭部回転を分離し、床高・目線高・水平を合わせる。
4. プレイヤー正面からのRayで注視・取得・会話・戦闘・魔法を検証し、必要に応じてVRコントローラーを対応させる。
5. 説明・会話・地図・インベントリ・各設定画面をVR空間で読みやすい距離に置く。
6. NPCとアイテムの板ポリゴン表示、遮蔽、ライト、透明効果を左右眼で確認する。
7. 移動酔い対策（回転段階切替、移動速度、視野制限、座位・立位）と描画負荷を調整する。
8. Windows向けVR試験ビルドを配布し、Questで実機確認する。

## 完了判定

- 通常画面モードを壊さない。
- HMD内で左右眼が正しく描画され、頭部の左右・上下・傾きに追随する。
- 移動・視点・ゲーム内操作・UIを利用でき、通常プレイと保存／読込が行える。
- 実機確認前には「VR対応済み」と表記しない。
