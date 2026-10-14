# VR化の検討（日本語化フォーク独自の計画）

状態: Windows向けVR試作ビルドまで作成済み。Unity 6000.6.0f1でC#コンパイル成功、Windows開発ビルド成功（エラー0）。**Quest/Virtual Desktopの実機試験開始済み。タイトルメニュー入力は動作確認、キャラクター作成入力と左右眼の立体視は未解決**。

### 右眼を固定して左眼画像だけを左右キーで動かす診断（10月14日）

- 実機で左右画像が融合しない報告が続いたため、ユーザー指定どおり、**右眼の描画結果は固定**し、**左眼だけ水平にずらす**ための隔離された起動モード `-vr -vr-left-eye-shift` と `LaunchVR_LeftEyeShift.bat` を追加。
- Unity 6 URP 17 / OpenXRのRenderer Featureの最終フルスクリーンパスを利用。`unity_StereoEyeIndex==0` の時に限りサンプリング元UVのXを変更し、右眼（index 1）はUVを変更しない。カメラ自体の位置・向き・眼間距離・投影行列を無理に上書きしない。通常モード・Desktopではこのパスは起動しない。
- 再接続実機ログ `VR_LeftEyeShift_Reconnect_20261014_222242.log` ではVDXRのOpenXRは正常、`stereoRenderingMode=MultiPass`、`[UWVR-EYE-SHIFT] XR final shader pass ACTIVE` を確認したものの、左右キーで `[UWVR-EYE-SHIFT] left=...` の変化が記録されず。WindowsゲームのキーボードフォーカスがVDに移っても動くよう、診断版だけWindowsの `GetAsyncKeyState` で左右/F6の物理キーも読む。
- 同ログで **`XRSettings.eyeTextureWidth=0`** と判明。初期実装は4px/0の代わりに4px/1pxを使う危険な換算だった。シフト量の分母をURP XRレンダーターゲットの正確な幅（`cameraTargetDescriptor.width`）へ変更し、幅0ならオフセット0を返す。MultiPassの場合は `xr.multipassId==0` の時にだけフルスクリーンパスを挿入し、**右目の描画パスそのものを実行しない**。見分け用は左眼の水色目印のみで右眼は完全に変更しない。
- Windowsキーボード左右矢印で左眼を4画素ずつ移動（押し続けると連続）、Shift+矢印で32画素ずつ高速移動、`F6` で0に戻す。左眼だけの目印（水色）と描画下部の補正値、`[UWVR-EYE-SHIFT]` ログを追加。**右眼には目印も補正も加えない**。
- **実機ログ（`Build-vr-prototype/VR_LeftEyeShift_Reconnect_20261014_222242.log`）では+192pxに到達し、ユーザーから「まったく重ならず、少なくともさらに3倍右へ寄せたい」と報告**。実際のXR眼テクスチャ幅2688pxに対して+192px=+7.14％であり、入力が届かない問題ではなかった。これまでの上下限を4倍の **±768px（+28.57％相当）** に拡張した。
- 補正は眼別カラー画像の**事後的な一律シフト**であり、VR立体視の実装そのものではない。左右の描画設定が誤っているというユーザーの当初からの指摘を、単純な距離調整・視差の微調整の問題として扱わない。VDXRの描画方式は **MultiPass**、URP Renderer Featureは右眼パスを挿入せず左眼パスだけを補正する実装。さらに `Camera.GetStereoViewMatrix` の左右眼位置をワールド座標へ戻し、カメラ右方向の相対位置の符号と、左右投影行列のm02を `[UWVR-EYE-GEOMETRY]` として出力する検査を追加。**これらの値は実機で収集できるまで確定しない。**
- `UWVRBuild.BuildExtendedLeftEyeShiftTrial` は旧VRゲームが起動中でも既存ファイルを上書きしないため、`Build-vr-left-eye-shift-extended/` へ別途Windows x64試験版を作成する。この生成先はGit追跡対象外とした。
- `L:\Games\UlutimaUW\unity-vr-left-eye-shift-768-build-20261014.log`：Unity 6000.6.0f1の自動検査 `[UWVR-OFFLINE] PASS`、Windows x64 `Build result: Succeeded; errors: 0`。4px/32pxの移動、±768pxの制限、左右視点行列の逆転を模擬したテストに合格。ただしHMD実機で視差が正しいかは未確認。
- 最新版を `Build-vr-left-eye-shift-extended/UR.exe -vr -vr-left-eye-shift` で起動した `VR_LeftEye768_Quest_20261014_224202.log` は、VDXRの `xrGetSystem` が `XR_ERROR_FORM_FACTOR_UNAVAILABLE` を返し、HMDを取得できずにXR描画開始前に失敗した。**±768pxの実機評価、左右のView/Projectionログは未取得。** 失敗ゲームは終了。HMD/Virtual Desktop再接続後に試験を再開する。
- 直前の`1061982`以降の未コミットVRメニュー表示／入力修正をそのまま保持して作業する。初期メニュー入力の一部は実機成功報告あり、キャラ作成画面の入力は未解決のまま。

## 2026-10-14 実機試験なしで進めた実装

### Quest / Virtual Desktop初回実機で判明したStereoメニュー表示と操作の問題（10月14日）

- WindowsのOpenXRランタイムはVirtual Desktop OpenXR（VDXR）。統合戦闘試験版（PID7012）は `XR_SESSION_STATE_FOCUSED` まで到達し左右各2688×2880のXR眼テクスチャを確保できていた。実機報告は**左右画像がずれて焦点が合わない、初期メニューでTouch・キーボード・パッド操作が一切できない**。誤った立体視を見続けないようPID7012を安全に終了した。
- UIは `UWVRCanvasBridge` が全Canvasを `ScreenSpaceCamera` に変更していたが、原作 `UWScreenUi.ScreenRectOf` は `GetWorldCorners` の世界座標をそのまま画面ピクセル扱い、`IsMouseOver` もスクリーン空間のカメラ指定なしだった。これをCanvasに紐づくCamera経由の画面座標投影に修正。
- タイトル・オープニング・キャラクター作成・読み込みカバーは、両眼で座標を別々に計算した画面ではなく、追跡中のHMDカメラの前方2.25mに物理幅約2.4mの **WorldSpace Canvasを一つだけ**置く方式へ変更。XRランタイムがその同一平面を左右それぞれの目から描く。DesktopのScreenSpaceOverlayとワールドHUD/鞄/キャラUIの旧レンダリングは維持。`-vr-legacy-menu-canvas` で旧画面モードとのA/B比較も可能。
- `UWVRMenuNavigation` を新設し、VR初期メニュー（4つの項目とセーブ一覧）をTouch左右のスティック/プライマリボタン/トリガー、ゲームパッド十字キー/左スティック/A、キーボード上下/W/S/Enterで直接操作可能にした。マウスWarpや画面内ヒット判定を使わず、選択項目のハイライトと実行を原作メニューに接続。VR初期メニューだけ `UWGamepadPointer` の合成マウスクリックを抑制して2重入力を防止。
- `UWVRHandAimGuides` の右Touch追跡ログは、従来右手レーザーを意図的に隠した近接攻撃モードで必ず「追跡なし」になっていた不備があった。実際の右手Poseによる判定に変更。`[UWVR-MENU-INPUT]` で左右XRデバイス、キーボード、ゲームパッドの認識状況を別途記録。
- 原因と修正は**コード上の整合性**が確認できたもので、Questの両眼表示・入力が直ったかどうかは新ビルドでの再確認が必要。
- 続くHMD実機評価では「2D画像が近すぎる」と指摘された。全画面2Dメニュー（タイトル・オープニング・キャラ作成・ロード）の既定表示距離を **2.25m → 4.0m** に変更。見かけの大きさを維持するため、基準幅2.4m@2.25mから距離に比例した幅4.27m@4.0mへ補正。`-vr-menu-distance=2.0～8.0` で距離を切替可能、`LaunchVR_MenuDistance6.bat` を6mの比較用として追加。HUDの2.25mと鞄の1.8mは変更しない。これは初期の両眼映像自体が立体視になると証明する修正ではない。
- `L:\Games\UlutimaUW\unity-vr-menu-distance4-offline-20261014.log`：全画面2Dパネルの既定4m、指定6m、不正な起動引数の復帰、距離比例の画面幅、HUD/鞄非変更を含むUnity 6000.6.0f1自動検査 `[UWVR-OFFLINE] PASS`。
- `L:\Games\UlutimaUW\unity-vr-menu-distance4-build-20261014.log`：Windows x64開発版 `[UWVR] Build result: Succeeded; errors: 0`。
- ビルド後にVDXRで新たに起動した `Build-vr-prototype/VR_Menu4m_Quest_20261014_213321.log` は `[UWVR] ... 2D menu distance 4 metres` と記録したが、`xrGetSystem: XR_ERROR_FORM_FACTOR_UNAVAILABLE` が発生。**HMDがOpenXRランタイムから取得できず、実機で4mの見え方を検証できなかった**。当該ゲームプロセスは終了させた。Virtual Desktop Streamerプロセスは存在していた。
- 再配布用ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-menu4m-win64.zip`（**102,069,038バイト／373項目／29起動バッチ**）。Unity実行ファイル、OpenXR関連DLL、6m比較バッチ、日本語ガイドをチェック。旧試験ログ、Unityが同梱不可としたバックアップフォルダ、原作ゲームデータを除外。旧ZIPを上書きしていない。
- ZIP SHA-256：`6675D410B3BD7553FBB283DF41296FEEFE38AC351B6FF4031F8CE5C594D5940B`。

### 弓・戦闘振動の物理寸法調整と実機試験ログ（10月14日）

- 直前コミット `7733214` が `fork/japanese-localization` と一致し、作業ツリーがcleanの状態から開始。HMD実機は今回も使用できないため、既定の動作を変えず**比較用の設定と診断手段**を追加する。
- `UWVRCombatCalibration` は起動引数 `-vr-bow-nock-radius`（0.20～0.70m、既定0.46m）、`-vr-bow-min-pull`（0.10～0.32m、既定0.20m）、`-vr-bow-full-pull`（0.35～0.75m、既定0.48m）、`-vr-haptic-scale`（0～1.25、既定1）を取得。どれも不正値・NaN・Infinityは既定に戻す。最大引き距離と最低引き距離は8cm以上離す。Haptic Impulseの最大振幅は0.60までに制限。
- `UWVRTwoHandBowDraw` の弓つがえ許容距離・最低引き距離・引き量表示の上限を実メートルで調整。Unityのステージ倍率1／16でも同じ身体動作なら同じ結果になる。通常のデスクトップ入力・原作の武器最低チャージ・飛翔体衝突・弾数消費は変更しない。
- `-vr-combat-diagnostics` は `-vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow` と併用時のみ詳細ログを出す。右トリガーを握っている間のつがえ待ち・つがえ・追跡喪失・無効ジェスチャー、左右Touch間隔・実引き距離・引き比率を抑制付きで記録。トリガー解除時の原作チャージ・手の追跡・弓判定も記録。未指定時は以前どおり通常のログのみ。
- 比較バッチを4種類追加：`LaunchVR_BowCal_Default.bat`、`LaunchVR_BowCal_EasyReach.bat`、`LaunchVR_BowCal_Compact.bat`、`LaunchVR_BowCal_Silent.bat`。新方式が使いにくければ以前の `LaunchVR_BowMagicHaptics.bat` や `LaunchVR_HandOrigin.bat` で比較できる。
- 最初の自動検査では追加テストのローカル変数初期化漏れをUnityが `CS0165` として検出。テストコードの初期化を修正し、`L:\Games\UlutimaUW\unity-vr-bow-calibration-offline-fixed-20261014.log` に `[UWVR-OFFLINE] PASS` を確認。旧設定を保つテストに加え、66cmのつがえ範囲・12cmの最低引き・振動倍率0.25・不正な数値の既定復帰・上下限・16倍スケールでも同一判定を確認した。
- `L:\Games\UlutimaUW\unity-vr-bow-calibration-build-20261014.log`：Unity 6000.6.0f1のHMD不要検査PASS、Windows x64開発ビルド `[UWVR] Build result: Succeeded; errors: 0`。接続したQuestでの実際の引き心地・振動・見え方は依然未確認。
- 比較用ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-bow-calibration-win64.zip`（**102,072,312バイト、371項目、起動バッチ26種類**）。新旧起動方法、最新の `Assembly-CSharp.dll`、OpenXR関連DLL、日本語試験ガイド・翻訳データを検査済み。原作ゲームデータは含まず、以前のZIPは維持。
- ZIP SHA-256：`CCAC10EC5CA67CB6FD94D98B6BD64C05C23E52717EC0626FE5EB465373F6ABFA`。

### 両手弓・左手魔法のXR振動要求（10月14日）

- 直前 `514cd7a` のGitHubへのプッシュと未コミット変更なしを確認してから開始。
- `-vr-combat-haptics` を **`-vr -vr-hand-aim -vr-hand-origin` が有効な場合のみ**許可する追加の試験フラグとして実装。既定は無効。比較用の `LaunchVR_BowMagicHaptics.bat` と `LaunchVR_AllCombatHaptics.bat` を追加。従来の20種類のバッチを残す。
- `UWVRBowHapticTimeline` が左手の弓つがえを1回だけ検出し、有効な弦の引き量が25/50/75/100%へ到達した際に各1回の振動要求を発行する。素早く複数段階を通過しても1フレームで1要求のみ、保持中の連打なし。追跡喪失・キャンセル・攻撃解除・UI切替は段階をリセットする。
- `UWVRCombatHaptics` はUnity XRの `TryGetHapticCapabilities` と `SendHapticImpulse` を利用。**実際に矢の飛翔体が生成された後のみ**右Touch/左Touchに弦のリリース反応を、**実際に左手元から魔法弾が生成された後のみ**左Touchに発射反応を要求する。弾切れ、壁遮蔽、発射失敗、非対応コントローラーの場合は振動しない。インベントリ・会話・フォーカス喪失時も不発とする。
- 振動の振幅は暫定的に最大0.46、各インパルスは0.085秒以内とし、同じTouchへの要求には最低0.105秒間隔を設ける。すべての数値はHMD実機での快適性調整が必要。振動を送っても、その機器で触覚が生じたかを自動的に確認できるわけではない。
- 原作の矢や魔法の発射時刻・飛翔・ダメージ・弾数・魔力、既存の掴み操作の振動は変更しない。装備や視界のVR表示も従来通り。
- Unity 6000.6.0f1でHMDなしの振動要求判定を検査。`L:\Games\UlutimaUW\unity-vr-combat-haptics-offline-20261014.log` に `[UWVR-OFFLINE] PASS`。同じ弓つがえで二重送信しないこと、引き段階の25/50/75/100%境界、急に引き切った1フレームでも複数送らないこと、追跡喪失・NaNデータの拒否、振幅と時間・最低間隔を確認。これは物理Touchが実際に振動した試験ではない。
- `L:\Games\UlutimaUW\unity-vr-combat-haptics-build-20261014.log` に `[UWVR-OFFLINE] PASS` と `[UWVR] Build result: Succeeded; errors: 0` を記録。Unity 6000.6.0f1のWindows x64開発ビルドが正常終了。
- 比較用配布ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-combat-haptics-win64.zip`（**102,068,242バイト／367項目／起動バッチ22種類**）。`LaunchVR_BowMagicHaptics.bat`、`LaunchVR_AllCombatHaptics.bat`、旧版起動バッチ、OpenXR関連DLL、日本語試験ガイド、更新 `Assembly-CSharp.dll` を検査した。原作ゲームデータなし、以前のZIPは保持。
- ZIP SHA-256：`D9B454918E22DCBE6477905CF6070C3271B2D0EFF70A7DB828ED1C2ECD344B2F`。

### 両手での弓引きと魔法の左手3D発射演出（10月14日）

- 直前の `53222a7` を `fork/japanese-localization` の最新コミットと照合し、未コミット変更なしから開始。
- `-vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow` で**弓ID 0x19のみ**両手動作を要求する試験モード。右Touchで弓を保持・照準し、左Touchを最初に右手から約46cm以内へ近づけた後、右手の向きに対して後方20cm以上引く。発射自体は従来の右トリガー解除と原作の最低チャージ判定の両方を通る必要がある。
- `UWVRTwoHandBowDraw` は両手位置の相対差（XR世界倍率を取り除いた実メートル）と右手の前方向だけから弓を引く動作を測定。右手の位置ジャンプ、左手追跡喪失、弓の急な向き変化、単なる左右横移動、引き不足を拒否。1回のトリガー解除につき1回のみ。**矢の消費・弾道・攻撃値・原作の魔法は変更しない**。クロスボウ、スリング、マウス操作は従来のまま。
- `UWVRBowVisual` は右Touchの6軸追跡を使う曲線状の木製弓・張られた弦を `LineRenderer` で仮3D描画。左手が弦を引く間は弦の中央と仮のつがえた矢が左Touchに追従。コライダーなし。VRでモデルが確認できる間だけ元の2D武器スプライトを非表示、追跡が切れた場合は復帰。原作の武器姿勢と一致する完成モデルではない。
- `-vr-hand-magic-fx` は `-vr-hand-origin` と組み合わせた場合のみ有効。`UWVRMagicHandFx` は追跡済み左Touchから魔法弾を狙っている間の小さな3D球、**飛翔体の生成に成功した後だけ**短時間で拡大・減衰する発射リングを描く。ダメージの種類に応じて色を切り替える。ゲームの魔法コスト、着弾、衝突を変更しない。対象指定魔法と原作のアイテム利用は従来処理のまま。
- 比較起動バッチ：`LaunchVR_TwoHandBow.bat`、`LaunchVR_HandMagicFX.bat`、`LaunchVR_BowAndMagic.bat`、`LaunchVR_AllCombat.bat`。既存 `LaunchVR_HandOrigin.bat` 等は保存。HMDでの指の位置・引き距離・両眼表示・操作性・適切な光の強さは**実機未検証**。
- `L:\Games\UlutimaUW\unity-vr-twohand-magic-offline-20261014.log`：Unity 6000.6.0f1で `[UWVR-OFFLINE] PASS`。両手の初期位置・後方36cm引き・左右移動のみ・短い引き・追跡喪失・位置ジャンプ・弓の向き急変・XR倍率16・複数回のリリース不可を確認。魔法のオーブ／両面リングの立体メッシュ形状、カラー、約0.42秒で拡大・消滅する式も検査。
- `L:\Games\UlutimaUW\unity-vr-twohand-magic-build-20261014.log`：上記の非HMD検査PASS、および Windows x64開発版 `[UWVR] Build result: Succeeded; errors: 0`。実機Questでの左右の手・入力と発射の正確さは**依然未検証**。
- 新ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-bow-magic-win64.zip`（**102,065,043バイト／365項目**）。20種類の起動バッチ、OpenXR用DLL、最新 `Assembly-CSharp.dll`、日本語試験説明と翻訳データをチェック。原作ゲームデータは非同梱。旧ZIPは保持。
- ZIP SHA-256：`6873CF0317E0661D459F924C579A716DEE94AB991DEE75F4EC5B9200DAED8BA9`。

### 左右のTouch発射位置を原作の飛翔体処理へ接続（10月14日）

- 前段の `250cad2` はGitHub `fork/japanese-localization` と一致、未コミット変更なしの状態から開始。
- `-vr-hand-origin` を、`-vr -vr-hand-aim` と組み合わせた場合だけ有効化。通常のデスクトップ、通常VR、従来の `LaunchVR_HandAim`／`LaunchVR_BladeContact` の発射位置・操作は従来のまま。右Touchの前方18cmから矢（弓・クロスボウ・スリング）、左Touchの前方12cmからルーン系の飛翔魔法を発射する試験経路を新設。
- `UWVRHandLaunch` が左右独立の6DoF追跡位置・向き、XR世界倍率に基づくメートル距離、HMD眼位置から手元発射点への壁遮蔽、マップ範囲を検査。追跡が取得できない時だけ従来のプレイヤー位置・HMD視線へ戻す。追跡されていて壁や範囲外・不正姿勢の場合は**発射を失敗**させ、弾・魔力を消費せず、体の位置から壁越しに飛ばさない。
- `UWMobileObjectMotion.Launcher` に任意の原作細座標 `ExplicitFineX/Y/Z` を追加し、XRで計算した実際の手元発射位置を1/8タイル単位ではなく原作の1単位まで保持。`TryLaunch` の新しい開始座標指定時だけ、原作の地形・物体の配置検査を**手元の位置**で実行。大きなプレイヤー当たり判定だけは開始点の検査から除外し、飛翔開始後の本来のプレイヤー衝突・飛翔・重力・命中・落下・消滅は従来のエンジンを維持。
- 弾道方向は従来の `AimFromDirection`、飛翔体生成は `UWProjectileWorld`、弾消費は `UWInventory.TryConsumeOne`、魔力支払いは `UWCharacter.SpendMana` を使う。弓を物理的に引く／魔法の手のひら効果／落下アイテムの手からの投擲は**この段階では変更しない**。
- 新しい比較用起動バッチ：`LaunchVR_HandOrigin.bat`、`LaunchVR_HandOrigin_Scale16.bat`、`LaunchVR_BladeAndHandOrigin.bat`。各試験ログには `[UWVR-MUZZLE]` として発射・遮蔽・追跡不足を記録。
- Unity非HMD検査では、左右の異なる発射点、倍率1と16、原作の細座標とowner、原作の空タイルへの発射成功・壁タイルでの発射失敗、Unity物理での遮蔽／トリガー除外を模擬検証する。**HMD実機で左右手の見え方・実射撃操作が成功したという意味ではない。**
- 初回のオフライン検証はゲーム時計を持たないテスト用模擬ホストが原因で停止。これは実装コードではなく模擬環境の欠落であり、`IUWMobileObjectHost.ClockPhase` などを補完した後、`L:\Games\UlutimaUW\unity-vr-hand-origin-offline-fixed-20261014.log` で `[UWVR-OFFLINE] PASS` を確認した。
- `L:\Games\UlutimaUW\unity-vr-hand-origin-build-20261014.log` では同じ自動検査PASSに加え、Unity 6000.6.0f1 の Windows x64開発ビルドが `[UWVR] Build result: Succeeded; errors: 0`。**HMD接続テストではない**。
- 新しい配布用ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-hand-origin-win64.zip`（**102,056,114バイト／361項目**）。16種類の起動バッチ、OpenXR関連DLL、更新した `Assembly-CSharp.dll`、日本語ガイド・翻訳データを確認。原作ゲームデータなし、既存ZIPの上書きなし。
- ZIP SHA-256：`9ABA834A701571EB3660AABAC6E6CBF197FD534573B2187E72D5E02C0EEEE4AD`。

### 物理的な刃軌道・武器ごとの代表形状（10月14日）

- 開始時点で直前の `800b36b` はGitHub `fork/japanese-localization` と一致し、未コミット変更がないことを確認。
- `-vr-blade-contact` を追加。`-vr -vr-hand-aim -vr-gesture-melee` と併用した時だけ有効。従来の `LaunchVR_GestureMelee.bat` を残し、`LaunchVR_BladeContact.bat` と物理尺度を比較する `LaunchVR_BladeContact_Scale16.bat` を追加。
- `UWVRWeaponShape` は原作 `UWCombat.GetWeaponTypeForWeapon` と同じ系統（剣・斧・棍棒・素手）で分類し、右手の仮メッシュと物理接触長／太さを統一。個別のアイテムIDごとの精密モデルではなく4種類の代表形状。右手の6DoF追跡・手首の捻りも引き続き適用。
- `UWVRBladeSweep` は右トリガーで武器を溜めている間だけ5点の刃軌跡を継続的なSphereCastで検査。最初に触れた実コライダーを使い、プレイヤー自身・トリガー・Ignore Raycastのコライダーを除外。`UWEntityInfo` を通じて本来の `UWDamageable` の対象だけを保持し、壁などに遮られた場合を拒否。
- 連続フレームの手元移動が十分速く、かつジェスチャー判定を通った場合にのみ最新0.35秒以内の接触候補を採用。追跡喪失や飛び移りは候補を破棄し、一攻撃につき候補を1つだけ保持。対象の消滅・移動・遮蔽・原作の射程超過は、**原作アニメーション命中フレームで再検査**する。
- **命中時の既存原作処理 `fApplyMeleeHit` / `UWPlayerAttack` / 武器消耗は変更しない**。ヒットフレームまでダメージを与えず、接触がない場合は従来の視線Rayへフォールバックしない。壁越しの攻撃を防ぐ。物理的な刃停止・敵ノックバック・手の触覚はまだ未実装。
- 重要な制約：ワールドのゲーム座標とVRのメートル縮尺は実機では未調整。尺度1と16の試験バッチで比較可能だが、どちらが実際に快適かは未検証。また、敵や扉での本番命中成功はHMD実機未確認。
- 検証では最初、Unityの `SphereCast` が初期接触で原点座標を返すケースによって、**原作の攻撃射程外でも接触可能と誤判定**する問題を検出。距離・遮蔽判定を実コライダーの `ClosestPoint` に基づく位置へ修正し、同じテストで再確認した。
- `L:\Games\UlutimaUW\unity-vr-blade-contact-offline-fixed-20261014.log`：Unity 6000.6.0f1、`[UWVR-OFFLINE] PASS`。模擬敵＋壁に実際のUnityコライダーを付け、最初に当たる障害物、眼からの遮蔽、トリガー無視、同一ストロークの二重消費、追跡喪失、攻撃射程外の拒否、4種類の武器形状を確認。
- `L:\Games\UlutimaUW\unity-vr-blade-contact-build-20261014.log`：同じ非HMD検査PASS、Windows x64開発ビルド `[UWVR] Build result: Succeeded; errors: 0`。原作戦闘処理を使う統合部分までQuest実機で成功したという意味ではない。
- 新ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-blade-contact-win64.zip`（**102,052,088バイト・358項目**）。13種類の起動バッチ、OpenXR DLL、更新したゲームコード、日本語試験ガイド・翻訳データを確認。原作ゲームデータは含まず、旧ZIPは保持。
- ZIP SHA-256：`F7E5272AD438C5DB9AACDC243313A6A46A0C3D80CFC1BE70720C5861A6C7C71A`。

### 右手武器プロトタイプ・移動軌道からの攻撃分類（10月14日）

- 前回 `f07ba51` までは `fork/japanese-localization` にコミット済みかつ作業ツリーはclean。今回追加の試験フラグは `-vr -vr-hand-aim -vr-gesture-melee` をすべて指定した場合に限り有効。`LaunchVR_GestureMelee.bat` を追加。デスクトップ・従来VR・HandAimの動作を変更しない。
- `UWVRHandWeaponVisual` は右手Touchの追跡済み6軸Poseへ位置・向き・手首のロールを追従させる、全長約0.8mの金属刀身、鍔、柄を持つ**カテゴリ共通の仮3D剣**。素手は拳の簡易ブロック。実アイテムIDごとの精密な形状、実際の握り位置補正はまだない。すべて描画用メッシュのみで衝突コライダー・Rigidbodyは付けない。
- 右手の仮モデルが実際に表示できるフレームだけ、元の2D武器スプライトを非表示。インベントリ表示、武器をしまった場合、追跡喪失時、遠距離武器の場合には3Dモデルを隠し、従来表示へ戻す。HandAimのみの右レーザーはこのモードで武器表示へ切り替えるが、左魔法レーザーは残す。
- `UWVRMeleeMotion` は追跡済みTouchの位置をHMDのローカル座標（XR世界倍率に左右されないメートル）に変換し、トリガーを握る間に短い移動軌道から横斬り、振り下ろし、突きを判定。最低移動距離約14cm、最低速度0.7m/s、26/100秒の判定窓と55/100秒の有効期間を採用。ノイズ、遅い移動、追跡断絶、極端な位置ジャンプは拒否。
- 右トリガーを離すとき、十分に新しい有効動作がある場合に限って原作の `Interaction` へ斬撃タイプと攻撃力上限を反映。武器の溜め・攻撃アニメーションの命中フレーム、原作の武器耐久・防御・ダメージ・射程は**今までと同じ処理**。モーションがなければ原作の攻撃動作からダメージ判定のみ除外。弓・クロスボウ・スリングの発射には適用しない。
- 現時点で**剣の刃先と敵のColliderを接触させてダメージを発生させる物理戦闘ではない**。当たり判定は前回HandAimのプレイヤー視点起点・右Touch方向のRaycast/SphereCast。モーションによる攻撃種別の認識・権限判定と、仮の手元3D武器の段階。
- UnityのHMD不要検査では、模擬90Hz軌道による3種類の判定、二重発火の拒否、細かい手のぶれ、位置ジャンプ、追跡喪失、動作の期限切れ、遅い移動、生成した武器メッシュの頂点数と長さ、XR倍率16の握り位置を検証する。実機での握り位置、振り速度、見え方、実際の敵への命中は**未検証**。
- 最終のUnity 6000.6.0f1バッチ：`L:\Games\UlutimaUW\unity-vr-gesture-melee-final-20261014.log` で `[UWVR-OFFLINE] PASS` と `[UWVR] Build result: Succeeded; errors: 0` を確認。手首ロールを含むTouchのワールド姿勢、トリガー離し時の追跡確認も追加済み。これはHMD不要のコード・数学的検査であり、Questでの実プレイを行ったわけではない。
- 新しい試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-gesture-melee-win64.zip`（102,047,424バイト、356項目）。11種類の起動バッチ（`LaunchVR_GestureMelee.bat` を含む）、OpenXR関連DLL、更新された `Assembly-CSharp.dll` と日本語説明・翻訳データを検査。原作ゲームデータは非同梱で、以前の配布ZIPは保持。
- ZIP SHA-256：`7F46625043EDC5C379EBA74E23D5DF4E80056EC11B44228F22970634F77744DF`。

### 左右Touchの独立照準（10月14日、任意の試験モード）

- 前の状態 `3da9845` はGitHubの `fork/japanese-localization` と一致し、作業ツリーに未コミット変更なしを確認してから開発を開始した。
- `-vr-hand-aim` を追加。**右Touchは近接攻撃・弓やクロスボウの射出方向、左Touchは魔法の飛翔体と対象指定**に割り当てる試作。デスクトップ起動および通常の `-vr` 起動には影響させない。
- `UWVRAim.Combat` / `Magic` はXR追跡スペースで変換済みの左右コントローラーRayを利用し、その**方向のみ**従来の視線Rayへ適用する。射程判定の起点と弓・魔法の発生位置は従来のプレイヤー側に残す。実際の手元・武器先端から攻撃判定や飛翔体を発生させる段階ではない。
- `Interaction` の近接Raycast/SphereCast、弓・クロスボウ・スリングの方向、斬撃種類の上下角度、魔法の飛翔体方向と世界対象指定に左右Touch照準を接続。世界の「調べる」操作、バッグ、ルーンUIのクリック経路は従来のまま。ルーンからの飛翔魔法は手元照準モードではUIカーソルが自由でも詠唱後に射出する（対象指定魔法は従来どおり対象の選択が必要）。
- Touch追跡が取得できないときはHMDの視線Rayへ復帰。`UWVRHandAimGuides` は右手に橙、左手に水色の約1.8m方向確認線を表示。いずれもコライダーを持たない描画用で、実際の命中位置を示す照準器ではない。
- `LaunchVR_HandAim.bat` を比較用に追加し、通常の `LaunchVR.bat` や既存のインベントリ操作起動バッチは保持する。実際の手の追跡、両眼表示、遠距離と近接の照準ずれ、快適性は**HMD実機未検証**。
- Unity 6000.6.0f1のバッチ実行で新しい照準の模擬方向・左右独立Ray・追跡喪失時の復帰・投射方向・表示線の縮尺を検査し、**`[UWVR-OFFLINE] PASS`**。Windows x64開発版も **`[UWVR] Build result: Succeeded; errors: 0`**。検証ログ：`L:\Games\UlutimaUW\unity-vr-hand-aim-build-20261014.log`。
- 試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-hand-aim-win64.zip`（102,042,280バイト、355項目）。`LaunchVR_HandAim.bat` を含む10種類の起動スクリプト、最新 `Assembly-CSharp.dll`、OpenXR DLL、日本語の試験手順を確認済み。原作ゲームデータは含めず旧ZIPは残す。
- ZIP SHA-256：`E8437CB405D4CA94863C99FE202D1853FA7971828BF14BE80EF9B00F3D5EB601`。

### 直接呼び出しによる近接グリップ操作の別経路（10月14日）

- 既存の `LaunchVR_HandGrab.bat` はWindowsのOS合成マウスを経由するため、Unity Input Systemへの非同期入力とバッグの更新順序に依存する。比較用に旧方式は保持し、別の起動オプション `-vr-direct-grab` を**必ず `-vr -vr-spatial-inventory -vr-near-grab` と同時に指定した場合だけ**有効にした。
- 新しい `LaunchVR_DirectGrab.bat` は元のゲームの `UWModernBags.fLeftPressed` と `fLeftReleased`、保持済みアイテムの `fLeftPressed` を呼び出す公開アダプターを利用する。Windows SendInputによるクリックの押下・解除は発行しない。アイテム・装備のルール自体は引き続き `UWInventoryModel` と既存のバッグ処理が実行する。
- 同じ装備/バッグ枠へ戻した場合や枠外で離した場合、元の `ReturnCursorItem()` で取得前の所持品へ戻す処理を試みる。異なるアイテムとの交換で別アイテムがカーソルに残った場合も、次のグリップで空き/有効枠へ運んで配置する経路を追加。
- 結果はカーソルの実体参照で `Placed / Swapped / StillHeld / Returned / ReturnFailed / NoDrag` に分類する。同じIDでも別オブジェクトなら交換と認識する。トリガーの重複クリックを抑止し、通常のHandGrab／レーザー方式には影響しないよう試験フラグで隔離。
- Unity非HMD検査では、**実際の `UWInventoryModel` のバックパック**へ異なるアイテムを配置し、「取得→交換で相手品がカーソルに残る→元の空き枠へ配置」のモデル状態を検査する。これは原作の移動ロジックを確認するテストで、実際のQuestグリップ入力やステレオ表示・UIへの指の位置は別途実機試験が必要。
- `L:\Games\UlutimaUW\unity-vr-directgrab-build-20261014.log` で `[UWVR-OFFLINE] PASS` と `[UWVR] Build result: Succeeded; errors: 0` を確認。Unity 6000.6.0f1のWindows x64開発ビルド成功。交換後の継続グリップの開始条件、同じIDの別アイテムを区別する結果判定も検査済み。
- 新試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-direct-inventory-win64.zip`（102,039,713バイト、354項目）。原作データなし、OpenXR用DLL、最新版の `Assembly-CSharp.dll`、新旧操作方式を含む起動バッチ9種類、日本語説明書を検査済み。以前のZIPは上書きしていない。
- ZIP SHA-256：`815607F6231BAB1AC41C66E198982E5F26E113BD9E950CAAD7744DF413CD8DEA`。
- 取得・交換・配置の検査は**ゲームのデータモデルを使ったHMD不要の自動検査**であり、Questコントローラーを通じた実操作や左右眼での表示を成功確認したものではない。実機で直接経路に問題があれば、`LaunchVR_HandGrab.bat`（合成マウス経由）および `LaunchVR_SpatialInventory.bat`（従来の遠距離パネル）と比較する。

### 近接グリップの入力競合・状態遷移の補強（10月14日）

- `UWVRNearGrab` は実際の右Touchグリップを**バッグが閉じている間も監視**し、インベントリを表示する前から握っていたグリップを新しい掴みとして誤認識しないよう変更。明示的な押下の立ち上がりのみを採用する。
- グリップ中にインベントリが閉じる、文脈メニューが表示される、PCの操作対象が切り替わる等で制御できなくなった際は、原作の `UWModernBags.ReturnCursorItem()` を使い、取得済みのアイテムを持ち物へ戻すことを試みる。マウスが処理する場所は従来UIの座標へ限定し、近接操作の新しい所持品モデルを作らない。
- 左マウスの合成入力は `UWGamepadPointer` だけが担当し、現在表示中のインベントリがある場合のみグリップによるマウス押下を送る。解除後は2フレーム元のドロップ座標を優先する。手の追跡がなくなれば掴み元の座標へ戻す計算を維持。
- 右Touchが近づいたアイテムのトークンを明るくする視覚フィードバックを追加。既存の共有Materialを壊さない `MaterialPropertyBlock` を使用。
- `VR_HandGrab_Test.log` に `[UWVR-GRAB]` のイベントログを追加。コントローラーが認識されたか、グリップ／振動機能の有無、押下・解除が合成マウスへ届いたか、アイテム取得・配置の確認/未確認を記録。**これは検証可能性の改善であり、HMD実機による動作試験を代替するものではない**。
- Unity 6000.6.0f1のHMDなし自動検査で、物理グリップの新規押下判定、グリップ開始条件、解除座標の優先順位、追跡を失った場合の座標復帰、インターフェース非表示時のポインター不介入などをチェックし、`[UWVR-OFFLINE] PASS` を確認。ログ：`L:\Games\UlutimaUW\unity-vr-gripstate-offline-20261014.log`。実際のMouseイベントやQuest入力は未検証。
- 追加修正を含むUnity Hub経由のWindows 64bit開発ビルドを実行し、`L:\Games\UlutimaUW\unity-vr-gripstate-build-20261014.log` で `[UWVR-OFFLINE] PASS`、`[UWVR] Build result: Succeeded; errors: 0` を確認。
- 新しい試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-grip-input-diagnostics-win64.zip`（102,037,658バイト／353項目）。新しい `Assembly-CSharp.dll`、OpenXR用DLL、8種類の起動バッチ、日本語説明書・翻訳データを検査。原作ゲームデータは含まず、以前のZIPもそのまま保持。
- ZIP SHA-256：`96CF41DC7DD6B205D80D8874B21B6FF38CFAEF3697576E3A3EA60D3FA374F343`。
- 自動検査は**純粋な座標計算・入力状態の分岐とWindows版ビルド**を検証したもので、Questのグリップ押下、OS合成Mouseの順序、実ゲーム上のアイテム受け渡し、振動、左右眼レンダリングの動作を実機で検証したものではない。新しい `[UWVR-GRAB]` ログで今後の実機故障箇所を特定する。

### 近距離Touchグリップ・触覚出力とカテゴリ別立体形状（10月14日）

- 明示的な `-vr -vr-spatial-inventory -vr-near-grab` のみ、既存の1.8m・幅1.65mの浮遊パネルを**距離0.68m・幅0.85m**の手の届く配置へ変更。`LaunchVR_HandGrab.bat` を追加。通常VRと以前の `LaunchVR_SpatialInventory.bat` の位置や入力は変更しない。
- `UWVRNearGrab` を追加。右手Touchの追跡位置を、XR倍率を含めパネルのローカル座標へ変換。パネルの前13.5cm以内、背面2.5cm以内かつ表示範囲内で、**実在する所持品または装備の枠でグリップを新たに握った場合だけ**ジェスチャーを開始。入力は `UWGamepadPointer` の単一の合成左マウスボタンに接続し、アイテム取得・ドラッグ・合成・交換・設置をゲームの既存処理へ委譲する。
- グリップを離す位置は、元のバッグ・装備判定が認める枠に限定。枠外や追跡を失った場合は、マウス座標を取得元の枠へ戻し、ワールドへ意図せず落とさない。OS合成マウスイベントをUnity入力側が受け取る遅延を考慮し、離した後の座標を2フレーム維持する。カーソルへ残ったものは元のクリック・クリック方式で取り扱う。
- 右グリップは通常武器の構えに使われるため、**近距離インベントリを開いた間だけ**その割当を抑止。それ以外は今までどおり。右トリガー・右Aの既存の入力も残す。ゲームの所持品データ・衝突判定には触れない。
- Unity XRの `TryGetHapticCapabilities` と `SendHapticImpulse` を使い、対応する右Touchのみに短い振動を要求する。枠への接近、取得確認、ゲーム側で所持品がカーソルから離れた配置成功時に限定し、最大頻度も制限する。非対応のデバイスでは無動作。**実機の振動は未試験**。
- `UWVRItemGeometry` に14種類の手続き生成メッシュを追加。武器（近接・飛び道具・弓）、防具、箱、松明、杖、宝石、容器風の瓶、ルーン、鍵、本、巻物、汎用を原作カテゴリと一部ID帯で区別する。`UWVRInventoryTokens` はモデルの共有・使い回しと原作アイコンの重ね合わせを実施する。**カテゴリ共通のプロトタイプ形状であり、個々の剣・鎧の専用完成モデルではない**。
- HMDなしのUnity自動検査では全14メッシュの非空判定、IDの代表値、XR倍率16・回転したパネルの近接UV変換、距離外／裏側／範囲外の拒否、グリップ開始条件、無効な離し位置で元に戻す判定を追加。実際のUnity入力イベント処理、実機の6DoF追跡、コントローラーの触覚出力、実アイテム配置の結果は**HMDで別途検証が必要**。
- Unity Hub認証付きバッチによる最新検証：`L:\Games\UlutimaUW\unity-vr-handgrab-build-20261014.log` に `[UWVR-OFFLINE] PASS` および `[UWVR] Build result: Succeeded; errors: 0` を確認。Windows 64bit開発ビルドで新しい `Assembly-CSharp.dll` を作成済み。HMD実機は未検証。
- 新規ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-hand-grab-meshes-win64.zip`（102,035,897バイト、353項目）。OpenXR用DLL、最新版のゲームコード、`LaunchVR_HandGrab.bat` を含む起動バッチ8種類、日本語説明・翻訳データを検査。原作ゲームデータは含めず、以前の配布ZIPは保持している。
- ZIP SHA-256：`6B3E4D9B5C56557B06254EC456660C2C4636AFB1F4DCADC3C4EB6304249CB9D4`。

### 空間インベントリの個別立体トークン表示（10月14日）

- `UWVRInventoryTokens` を追加。`-vr-spatial-inventory` が有効で、世界固定パネルが表示されているときだけ生成する。**個別の所持品・装備を薄い立体トークンとして表示**し、原作のアイコン画像を手前の面に貼る。武器・防具・容器・巻物などで厚み・縦横比を少し変える初期試作であり、個々の物品形状に合わせた本格的なメッシュはまだない。
- `UWModernBags.AppendVRItemVisuals` は表示中の実アイテム、表示済みアイコン、画面内の枠座標を**読み取り専用**で渡す。`UWModernPanel.AppendVRItemVisuals` は原作の11装備領域を照合し、実際にクリックできる位置から装備アイコンを渡す。アイテムの新規作成・消滅・装備判定の差し替えは行わない。
- Unityの3Dトークンは表示専用でColliderを無効化している。操作は旧 `UWGamepadPointer` の右Touchクリック／Ray座標と `UWModernBags` のドラッグ・合成・配置ルールに引き続き従う。
- `UWScene.Inventory.CursorItem` に所持品があるとき、原作のアイコンから別のプレビューを生成し、右Touchコントローラーの約15cm前に追従させる。**トリガーを押しただけで3Dアイテムを生成・移動するわけではなく、元の所持品モデルの取得状態が唯一の正規状態**。
- 立体札の前面より照準が手前に出るように、Touchのヒット点マーカー位置を補正。RenderTexture／GPU上の立体表示・正しいTouchの位置・取得後の見え方・配置操作はHMD実機での未確認事項として残る。
- UnityエディターをHMDなしで起動した自動検査では、枠座標からのUV変換、部分的に画面外の枠のクリップ、画面外の完全除外、分類による厚みの区別、16倍のXR追跡スケールでも保持中の表示位置が15cmを維持する計算が**PASS**。
- Unity Hub経由の `UWVRBuild.BuildWindowsPrototype` を実行し、`L:\Games\UlutimaUW\unity-vr-itemmodels-build-20261014.log` で `[UWVR-OFFLINE] PASS`、`[UWVR] Build result: Succeeded; errors: 0` を確認。HMD実機試験は未実施。
- ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-item-tokens-win64.zip`（102,028,528バイト、352項目）。更新した `Assembly-CSharp.dll`、OpenXR関連DLL、日本語説明書、通常／空間インベントリの起動バッチを検証。原作ゲームデータは含まれず、旧ZIPは保持。
- ZIPのSHA-256：`9D95A8A846193F0E25ACAE7DBF8FB669FC1C166B3CCD5053BD6315CBCFE99D20`。

### 空間固定インベントリと右手TouchのRay操作（10月14日）

- `-vr-spatial-inventory` を明示したVR起動に限り、`UWVRSpatialInventory` が既存のバッグ・キャラクター画面のCanvasを専用カメラと透明RenderTextureに向け、プレイヤーの前方に**世界座標で固定された平面パネル**として表示する試験経路を追加。通常VRモードとデスクトップモードの表示は変更しない。
- パネルは幅約1.65メートル、距離は既存の `-vr-inventory-distance=N`（既定1.8メートル）。現時点ではバッグと装備画面をまとめた1枚の平面で、アイテム個別の3Dモデルや実際に手で持つ仕組みではない。F8キーで視界正面に再配置でき、開き直し時にも再配置する。
- `UWVRHeadTracking.TryGetControllerRay` は右手Touchの6DoF位置・回転を、頭の初期位置とVR追跡空間の倍率を踏まえ、ゲーム世界のRayへ変換。頭と手で異なるワールド尺度にならないようにした。
- Rayとパネルの交点をUVへ変換し、既存の画面ピクセル座標へ戻して `UWGamepadPointer` の合成マウス入力に渡す。クリック・ドラッグ・装備判定を再実装しない。Touchの位置・姿勢が得られないときは右スティックの旧操作にフォールバック。パネル上の照準線と小さな点を用意した。
- `UWVRCanvasBridge` は空間モードに限りバッグとキャラクターパネルのCanvasを専用カメラに向ける。他のHUD/メニューCanvasは以前のステレオカメラ変換を保持。`UWVRPointerReticle` はTouch Rayが当たっている間、二重に表示される画面固定カーソルを隠す。
- `LaunchVR_SpatialInventory.bat` を追加。通常VR起動時は以前の固定表示に戻るため、実機で問題が出ても旧試験版の操作経路を比較できる。
- Unity 6000.6.0f1でHMDなしの自動検査は **PASS**。回転・非均一倍率のあるパネルでのRay位置、範囲外・裏面の除外、16倍の追跡空間における手の座標を確認。ただしGPU RenderTextureとHMD左右眼の表示、XRランタイムからのコントローラー位置、実際のクリック・ドラッグ操作は**未検証**。
- `unity-vr-spatial-build-20261014.log` に `[UWVR-OFFLINE] PASS`、`[UWVR] Build result: Succeeded; errors: 0` を確認。Unity Hubの認証付きバッチでWindows x64開発ビルドが成功した。
- 配布ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-spatial-touch-win64.zip`（102,024,254バイト、352項目）。OpenXR用DLL、新しいゲーム用 `Assembly-CSharp.dll`、日本語説明書、7種類の起動バッチ（新規 `LaunchVR_SpatialInventory.bat` を含む）を検証した。原作ゲームデータは同梱しない。前のZIPも保持。
- ZIP SHA-256：`0DDC6C4C7462DAA852EBE9E76B3073CA916DC6D58C59B5536E0FF4B167925BAC`。
- この完成判定は**コードの自動検査・Windows実行ファイル生成・ZIPの構成検査**までであり、透明RenderTextureの見え方、Rayの左右眼での位置、Quest Touchの実入力、実際のアイテム移動はHMD実機で別途確認すること。

### 装備11領域の選択・VRインベントリ平面の奥行き分離（10月14日）

- `UWClickRules.TryGetInventorySpotArea` は原作の装備当たり判定表を読み取り専用で公開。原作の優先順位やクリック動作自体は変更していない。
- `UWVRInventoryNavigation.TryGetEquipmentCentre` は原作の11装備領域（兜・胴・手袋・脚・靴・左右肩・左右手・左右指輪）に対し、横幅を拡張した紙人形ページの位置へ変換する。領域が重なる場所は原作の判定で最初にヒットする装備だけを候補にし、キャラクター画面のスクロール範囲から隠れた点を除外。
- `UWModernPanel` は実際に表示中の装備欄の選択可能位置を列挙。`UWModernBags` はバッグと装備欄の位置を結合し、右スティックで両方の間を移動可能にした。バッグが閉じていてもキャラクター画面が表示中なら装備欄だけの選択が可能。ポインター非表示、文脈メニュー中、アイテム操作中は従来動作を優先。
- `UWVRCanvasBridge` はモダンHUD・メニューの暫定表示距離（既定2.25メートル）とは別に、バッグとキャラクターパネルの距離を既定1.8メートルへ分離。両パネルは同じ距離にして、アイテム移動の元の画面内座標を維持する。`UWVRPointerReticle` は操作対象の画面の少し手前へ追従。手の動きに同期する本格的な3Dインベントリは未実装。
- `-vr-inventory-distance=N`（1～5メートル）を追加し、比較用に `LaunchVR_InventoryFar.bat`（2.5メートル）を用意。
- Unity 6000.6.0f1のHMDなし自動検査で、**11装備領域すべてのクリック位置一致**、クリップされた部位の除外、バッグ操作とVR平面の距離分離を確認して **PASS**。実機での視認性・ドラッグ・Quest Touchの反応は未検証。
- Unity Hub経由のバッチで `UWVRBuild.BuildWindowsPrototype` を再実行し、`[UWVR-OFFLINE] PASS`、`[UWVR] Build result: Succeeded; errors: 0` を確認。ログ：`L:\Games\UlutimaUW\unity-vr-equipment-build-20261014.log`。
- Windows試験ZIP：`Release/UnderworldRevisited-vr-prototype-20261014-equipment-depth-win64.zip`（102,019,265バイト、351項目）。更新した `Assembly-CSharp.dll`、OpenXR用DLL、日本語データ、VR比較起動バッチを含めて検証済み。原作ゲームデータは同梱しない。以前のZIPを上書きしていない。
- ZIP SHA-256：`C8D01EE6F4B02CCFD13B750ED49C91C6B5063BE205FD3FF2317B247BC7DC3107`。

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
- VR用画面固定UIは、頭部追跡の倍率を大きくすると目に近づきすぎる問題があったため、`UWVRCanvasBridge`でカメラの追跡空間倍率に応じて距離を補正。当初はインベントリを含め既定2.25メートルで共通だったが、上記の実装後はバッグとキャラクターパネルを独立して調整可能。`LaunchVR_UIFar.bat` はHUD・メニュー向けの3.5メートル比較用。
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
