# VR化の検討（日本語化フォーク独自の計画）

状態: Windows向けVR試作ビルドまで作成済み。Unity 6000.6.0f1でC#コンパイル成功、Windows開発ビルド成功（エラー0）。**HMDへの左右眼描画、追跡精度、操作の実機検証は未実施**。

## 2026-10-14 実機試験なしで進めた実装

### VRバッグ操作：枠ごとの移動とTouch単独操作（10月14日）

- モダン操作のバッグUIはピクセル座標でアイテムの掴む・離す・合成・右クリックを判定している。VR専用の別Canvasへ作り替えると既存の判定を破壊するため、**元のバッグ画面のスロット座標にカーソルを移動する方式**を採用した。
- `UWVRInventoryNavigation` は与えられた画面上のスロット中心から、右スティックの上下左右に対して次の中心を決定する純粋な計算処理。上下左右・初回フォーカス・端での停止・中立入力の自動検査を追加。
- `UWModernBags.TryGetNextVRSlot` は表示中のバックパックとコンテナの枠を列挙する。ウィンドウが非表示の場合や分割ダイアログ・文脈メニュー中は対象にしない。
- `UWGamepadPointer` は**VR時かつバッグを開き、ポインターが自由なときだけ**右スティックを1枠単位で進める。最初の繰り返しは0.38秒、以降0.14秒で繰り返す。クリックを保持している間は従来の連続カーソル移動に戻し、アイテムのドラッグを維持する。
- 左Touchスティック押し込みをバッグ開閉（`ModernBags`）へ追加。右スティック押し込みは従来どおりポインター解放・固定を切り替える。キーボードのBやデスクトップゲームパッドの操作割当は変更しない。
- 元のインベントリ画面・キャラクターパネル・右クリックメニューを3D空間に作り直したわけではない。**HMDでの可視性、実際のTouchボタン認識、ドラッグ操作は未検証**。
- `unity-vr-inventory-build-20261014.log`：Unity 6000.6.0f1のHub認証付きバッチで **`[UWVR-OFFLINE] PASS`** と **`[UWVR] Build result: Succeeded; errors: 0`** を確認。
- 最新のWindows試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-inventory-touch-win64.zip`（102,017,248バイト、350項目）。更新後の `Assembly-CSharp.dll`、OpenXR用DLL、日本語データ、5種類の起動バッチ、日本語の説明書を確認。原作ゲームデータは含まない。
- ZIP SHA-256：`0AD103452FBEBCD980D7C1A6D8A92F6534E7A094218D6CF6D9D0A6CB7FA9DB11`。

### Unity Hub認証経由でのビルド復旧（10月14日）

- Microsoft Store版Unity Hub 3.22.3では、Hub本体のプロジェクト登録先と、通常環境から呼ぶUnity CLIの登録先が異なっていた。Hub本体側に既存プロジェクト `L:\Games\UlutimaUW\UnderworldRevisited` を登録し、Unity 6000.6.0f1を通常起動できることを確認した。
- 以前の直接 `Unity.exe -batchmode` 実行はライセンスエラー198で終了したが、**Unity HubのCLIによる `projects open` にバッチ実行引数を渡した場合は認証が通り、ビルドが成功**した。既に起動中のEditorが同じプロジェクトを開かないよう、GUIのない残存プロセスを終了してから実行した。
- `UWVRDiagnostics.RunOfflineChecks`: **PASS**（OpenXR設定、Quest Touch入力、頭の向き基準の移動、頭部追跡空間と倍率、UIの物理距離、カーソルの入力透過性）。
- `UWVRBuild.BuildWindowsPrototype`: Unity 6000.6.0f1のWindows 64bit開発ビルドが **Succeeded / errors: 0**。Unityログ: `L:\Games\UlutimaUW\unity-vr-hub-batch-20261014.log`。
- 配布ZIP: `Release/UnderworldRevisited-vr-prototype-20261014-worldscale-win64.zip`（102,015,823バイト）。`UR_Data/Managed/Assembly-CSharp.dll` は2026-10-14の更新版。OpenXR用DLL、既存の日本語訳、5種類の起動バッチ、日本語の試験説明書を同梱。原作ゲームデータは含めていない。以前の試験版ZIPは保持。
- ZIPのSHA-256: `7BD8089AA3B59327869FE39968AB42DECFE18671B6F7A20FCE673CF1B8948699`。
- **HMDでの左右眼描画、移動の自然さ、Quest Touchの実際の認識、UIの読みやすさは未試験**。日本語訳の追加作業も一時停止のまま。

### 最新ソースの検証とUnityライセンスの障害（10月14日）

- `UWVRHeadTracking` に頭部追跡空間を導入した版は、Unity 6000.6.0f1の非HMD自動検査で **PASS** を確認（`unity-vr-worldspace-offline2.log`）。カメラの元位置保持、倍率16での位置差分、終了後の階層復帰、HMD方向の歩行を検査した。
- その後に追加した **UI距離の倍率補正** と `-vr-ui-distance=` の検査は、当初ライセンス問題で実行できなかったが、上記のHub経由で再実行し、**PASSを確認**した。
- Unity 6000.6.0f1のバッチ起動で **終了コード198**、`No valid Unity Editor license found. Please activate your license.` および `com.unity.editor.headless was not found` を記録。`-nographics` を外しても同じエラー。ゲームのコードエラーではない。
- 代替としてUnity同梱の.NET SDK / Roslynと既存のUnityコンパイル応答ファイルを使い、現在のソースから **Assembly-CSharp.dll と Assembly-CSharp-Editor.dll をそれぞれエラー0でコンパイル**。既存コードに関する警告は残る。これはC#コンパイルの確認であり、Unityのアセット・シェーダー・Windows Playerビルドの代替ではない。
- ライセンス問題が発生した時点では新しいWindows実行ファイルとZIPは未作成だった。その後、Hub経由のビルドで**更新版の作成と検証を完了**した（上記参照）。以前の配布物（`UnderworldRevisited-vr-prototype-20261014-touch-ui-win64.zip`）は上書きしていない。
- HMD実機試験はユーザーの希望により引き続き保留。日本語訳の追加も停止中。

### VR世界尺度・視点位置・移動の実装（10月14日）

- ゲームの1タイルは64ゲーム単位、プレイヤーの CharacterController 高さは2ゲーム単位、カメラのプレハブ上のローカル高さは約0.907。XRの移動値・両眼オフセットをそのままゲームの大きさとして扱うと、身体と世界の見かけの大きさが整合しない可能性がある。
- `UWVRWorldSpace` でゲーム単位／メートルの倍率を起動引数 `-vr-units-per-meter=N`（0.25～64）から設定。**既定値は1で、実機で正しい尺度と認めた値ではない。** `LaunchVR_Scale16.bat` で倍率16と比較可能。
- `UWVRHeadTracking` は元のカメラ位置の間に追跡用の親オブジェクトを挿入し、その親を指定倍率で均等に拡大。HMDの頭の並進と両眼レンダリングの視点間隔が同じ親変換を受ける。VR終了時はカメラ階層・座標・縮尺を元に戻す。水泳時の視点高さ変化は親側の位置に適用。
- メニューカメラの追跡コンポーネントを親設定後に追加するよう修正し、シーンルートに余計な追跡用親オブジェクトが作られる不具合を防止。
- VRのモダン操作では、左スティックの方向をHMDの水平な向きに合わせてプレイヤーの移動入力へ変換。見下ろし・見上げによる上下方向の移動は起こさない。`-vr-body-relative` で体の向きを基準に切替可。
- `F9` キーで頭部位置の基準をリセット（向きの基準・プレイヤー本体の位置は変更しない）。
- VR用画面固定UIは、頭部追跡の倍率を大きくすると目に近づきすぎる問題があったため、`UWVRCanvasBridge`でカメラの追跡空間倍率に応じて距離を補正。既定は2.25メートル、`-vr-ui-distance=N`（1～5メートル）で調整可能。インベントリにも共通で適用する暫定措置。`LaunchVR_UIFar.bat` は比較用の3.5メートル。
- Unityの非HMD検査に、視点の親オブジェクト追加時に元の位置が変わらないこと、倍率をかけた頭の移動、終了後の元の階層への復帰、移動方向の回転、倍率引数の解析を追加。検査用のカメラで実際にTransformを操作して確認。
- 16ゲーム単位／メートルでのUI距離の補正（2.25メートル→36ゲーム単位）と元の倍率への復帰も自動検査に追加。
- HMD実機でのIPD、画面の大きさ、自然な移動速度、衝突判定との整合性は未検証であり、倍率確定・補正は引き続き必要。

### VRメニュー操作の続き（10月14日）

- 既存の `UWGamepadPointer` はメニューで実際のマウスを動かす方式だが、Quest Touchからのクリック入力が `PadClick` / `PadContext` に接続されていなかった。右Aボタンと右トリガーをクリック、左グリップを右クリック、右スティック押し込みをポインター切替に追加。
- `UWGamepadPointer.Takes` により、メニュー操作中のボタン入力が同時に武器攻撃や世界への使用にならないよう、既存の排他機構を活用する。
- WindowsのシステムカーソルはHMD内のOpenXR描画には映らないため、`UWVRPointerReticle` を追加。画面固定ではなくステレオカメラの Canvas 上にレティクルを描き、ポインター解放時は実際のマウス座標、ロック時は視線中央に配置する。いずれの表示も UI への Raycast を遮らない。
- メニュー用一時カメラの AudioListener が既存の Listener と重複しないように修正。
- `UWGamepad.AnyPressed` にXRコントローラーのボタンも追加。オープニング・長文のページ送りなど、専用の入力アクションを使っていない箇所にも対応する。
- `UWVRDiagnostics.RunOfflineChecks` にTouchのメニュー入力バインド、VRカーソルの生成・最前面・クリック透過性を加えた。Unity 6000.6.0f1で**C#コンパイル成功、非HMD検査PASS**。
- 最終検証ログ `L:\Games\UlutimaUW\unity-vr-ui-20261014-build.log`：`[UWVR-OFFLINE] PASS` と `[UWVR] Build result: Succeeded; errors: 0` を確認。
- **注意**: この成果はあくまでUnityのコードと自動検査。Win32マウス操作とXR入力の実機上の連携、カーソル位置の見え方、ボタン反応は未検証。

- `UWVRAim`: VR時はHMD正面から世界への視線Rayを出す。`Interaction` の調査・使用・拾う・魔法・投射物でRay計算を共有する。非VR時は以前と同じ画面座標Rayを使う。
- `UWVRCanvasBridge`: 元の `ScreenSpaceOverlay` CanvasをVR時だけ `ScreenSpaceCamera` に移し、画面の前方3ワールド単位に投影する。メニュー・HUD・所持品等は既存の配置と並び順を維持する暫定表示。動的に作られるCanvasにも追従し、VR終了後は元の設定に戻す。
- `UWVRBootstrap`: プレイヤーカメラのないメニューで一時的なVRカメラを生成する。プレイヤー出現時には切り替える。左右眼の解像度と描画方式をログに出力する。
- `UWVRHeadTracking` と `UWPlayerTerrain`: 水泳中の視点高さをVR追跡側に引き継ぐ。通常の歩行時の頭の上下揺れ・泳ぎでの強制傾きをHMD時は止め、頭の姿勢を上書きしない。
- `UWVRComfortTurn`: モダン操作時は右スティックで30度ずつ体を旋回。UI操作中は発動しない。
- `UWControls` と `UWGamepad`: 左右Quest Touchコントローラーのスティック・ボタンを既存の移動・使用・戦闘・魔法などへ接続。OpenXRのXRControllerとして扱う。物理コントローラー入力の実機確認は未実施。
- `UWVRDiagnostics.RunOfflineChecks`: XRの有効化、非VR時のXR自動起動禁止、Touch入力定義、非VR時の視線Ray互換をエディターの自動検査で確認する。
- `UWVRBuild`: Windowsの試験版ビルド前に上記チェックを実行し、VR/通常モードの起動バッチと日本語説明書を自動同梱する。
- Unity 6000.6.0f1で `UWVRDiagnostics.RunOfflineChecks` を実行し、XRローダー・デスクトップRay互換・Touch入力・VR Canvas変換の検査が **PASS**。続く `UWVRBuild.BuildWindowsPrototype` のWindows開発ビルドも **Succeeded / errors 0** で完了。

**ここまでの「実装済み」はコードと非VR環境での検査を意味し、Quest実機で動作したことは意味しない。** 実機でのステレオ出力・操作・UIの正確さは保留。

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
