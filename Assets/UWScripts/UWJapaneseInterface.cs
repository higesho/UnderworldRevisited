using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-game user-facing settings/help UI translations. Gameplay strings are resolved through
/// STRINGS.PAK and Localization/ja.tsv; this dictionary only covers the port's own UI.
/// Unknown text, user-entered notes and game-specific key names are never changed.
/// </summary>
public static class UWJapaneseInterface
{
    private static readonly Dictionary<string, string> Translations =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "Game", "ゲーム" },
        { "Motion", "移動" },
        { "Graphics", "画質" },
        { "Display", "表示" },
        { "Controls", "操作" },
        { "Sound", "音声" },
        { "Colours", "色補正" },
        { "Game folder...", "ゲームデータの場所…" },
        { "Game folder", "ゲームデータの場所" },
        { "Quit", "終了" },
        { "Done", "完了" },
        { "Cancel", "取消" },
        { "Off", "切" },
        { "On", "入" },
        { "Original", "原作式" },
        { "Modern", "現代式" },
        { "Smooth", "なめらか" },
        { "Even", "均等" },
        { "Stepped", "段階式" },
        { "Response", "反応速度" },
        { "Picture", "映像" },
        { "View", "画角" },
        { "Head bob", "視点の揺れ" },
        { "Weapon", "武器" },
        { "Original fps", "原作の更新回数" },
        { "Easy movement interval", "移動更新間隔" },
        { "  Strength", "  強さ" },
        { "Low - original palette", "低：原作の色調" },
        { "Medium - Remastered, no effects", "中：高精細・効果なし" },
        { "High - Remastered, all effects", "高：高精細・全効果" },
        { "Very high - own effects...", "最高：効果を個別設定…" },
        { "Palette effects...", "原作色調の効果…" },
        { "  Classic screen widescreen", "  原作画面を横長にする" },
        { "  World at the original's resolution", "  原作の解像度で3Dを描画" },
        { "  Logos and intro at startup", "  起動時にロゴと導入を再生" },
        { "  Keep running in the background", "  他の画面でも動作を続ける" },
        { "  Choose attributes at creation", "  作成時に能力値を指定" },
        { "  4x needs a window 1600 pixels tall", "  4倍表示には高さ1600ピクセル以上が必要" },
        { "Window", "ウィンドウ" },
        { "Borderless fullscreen", "枠なし全画面" },
        { "Mode", "表示方式" },
        { "Resolution", "解像度" },
        { "   (desktop)", "   （デスクトップ）" },
        { "At once", "即時" },
        { "Orig.", "原作" },
        { "Health", "体力" },
        { "Poisoned", "毒状態" },
        { "Back to default", "初期設定に戻す" },
        { "Drives:", "ドライブ：" },
        { "Found:  ", "検出：  " },
        { "Up", "上へ" },
        { "Use this folder", "このフォルダーを使用" },
        { "Open folder", "フォルダーを開く" },
        { "Preparing the game data...", "ゲームデータを準備しています…" },
        { "The game data could not be read from this folder.", "このフォルダーからゲームデータを読み込めませんでした。" },
        { "Keyboard and mouse", "キーボードとマウス" },
        { "Gamepad", "ゲームパッド" },
        { "Button names", "ボタン表記" },
        { "Stick look speed", "スティック視点速度" },
        { "Mouse look speed (modern)", "マウス視点速度（現代式）" },
        { "  Swap the sticks (left-handed)", "  左右スティックを交換（左利き用）" },
        { "  Invert looking up and down", "  視点の上下操作を反転" },
        { "  Button hints in the game (crosshair, action bar)", "  ゲーム内にボタン案内を表示" },
        { "Left stick deadzone", "左スティックの遊び" },
        { "Right stick deadzone", "右スティックの遊び" },
        { "Press a key or button...", "キーまたはボタンを押してください…" },
        { "Press a button...", "ボタンを押してください…" },
        { "Music", "音楽" },
        { "  Music", "  音楽" },
        { "  Sound effects", "  効果音" },
        { "Music played by", "音楽の再生方式" },
        { "AdLib", "AdLib" },
        { "General MIDI", "General MIDI" },
        { "MT-32", "MT-32" },
        { "Strength", "筋力" },
        { "Brightness", "明るさ" },
        { "Contrast", "コントラスト" },
        { "Red (protanopia)", "赤の識別（1型）" },
        { "Green (deuteranopia)", "緑の識別（2型）" },
        { "Blue (tritanopia)", "青の識別（3型）" },
        { "  Hatch the health flask while poisoned", "  毒状態では体力瓶に縞模様を付ける" },
        { "Palette effects", "原作色調の効果" },
        { "Preset", "設定例" },
        { "No colour", "色なし" },
        { "Dirt", "土" },
        { "Moss", "苔" },
        { "Soot", "すす" },
        { "Subtle", "弱め" },
        { "Full", "強め" },
        { "Own", "自分で設定" },
        { "Ambient occlusion", "隅の暗がり" },
        { "Grime along the walls", "壁際の汚れ" },
        { "Depth", "奥行き" },
        { "Ground shadows", "足元の影" },
        { "Hollows", "くぼみ" },
        { "  Light sources", "  光源" },
        { "  Glow - lava and flames keep their full colour, in the dark too", "  発光：暗闇でも溶岩や炎を鮮やかに" },
        { "drag here to move", "ここをドラッグして移動" },
        { " ms", "ミリ秒" },
        { " steps", "段階" },
        { "Stats", "能力" },
        { "Map", "地図" },
        { "Notes", "メモ" },
        { "Spells", "呪文" },
        { "Mantras", "マントラ" },
        { "Manual", "説明書" },
        { "Your own notes, kept with the save game.", "自由に書けるメモです。セーブデータと一緒に保存されます。" },
        { "No character is loaded.", "キャラクターが読み込まれていません。" },
        { "No level is loaded.", "階層が読み込まれていません。" },
        { "You carry no map. Pick it up again to see it here.", "地図を持っていません。地図を入手するとここに表示されます。" },
        { "Attributes", "基本能力" },
        { "Skills", "技能" },
        { "Show the hidden values", "内部の数値を表示" },
        { "Hide the hidden values", "内部の数値を隠す" },
        { "The game keeps these, but shows them only as words or not at all.", "ゲーム内部では数値を記録していますが、通常は言葉で示すか非表示にしています。" },
        { "Worn magic", "装備中の魔法" },
        { "No inventory.", "所持品がありません。" },
        { "Nothing worn is enchanted.", "魔法の付いた装備はありません。" },
        { "Together, without cast spells:", "使用中の呪文を除いた合計：" },
        { "Quests", "探索の手がかり" },
        { "One skill each, raised greatly", "各技能を大きく高める言葉" },
        { "Modern controls", "現代式の操作" },
        { "Classic controls", "原作式の操作" },
        { "The manual cannot be shown.", "説明書を表示できません。" },
        { "Click the page to read it large.", "ページをクリックすると拡大できます。" },
        { "255 is full; the status line changes every 30.", "満タンは255。状態の表示は30刻みで変化します。" },
        { "0 is rested; the status line changes every 23.", "休息時は0。状態の表示は23刻みで変化します。" },
        { "A mushroom adds one, the potion sets three; one wears off every 21 seconds.", "キノコで1増加し、薬で3になります。21秒ごとに1ずつ減少します。" },
        { "A mantra at a shrine spends them. One more every 3000 experience and every level.", "祠でマントラを唱えると消費します。経験値3000ごととレベルアップ時に1増えます。" },
        { "Hide the spells the manual does not name", "説明書にない呪文を隠す" },
        { "Show the spells the manual does not name", "説明書にない呪文も表示" },
        { "Hide the mantras the manual does not name", "説明書にないマントラを隠す" },
        { "Show the mantras the manual does not name", "説明書にないマントラも表示" },
        { "Choose the folder of your GOG installation of Ultima Underworld - the one that contains game.gog.", "GOG版『ウルティマ アンダーワールド』のインストール先を選んでください。game.gog があるフォルダーです。" },
        { "How you move. The game uses the movement code of the original. Here you choose how often it calculates and how the picture follows.", "移動の処理は原作の仕組みに基づきます。移動を計算する頻度と画面の追従方法を設定できます。" },
        { "Original: calculates like the original, including its rounding. Smooth: the same rules without the rounding. Smooth moves the same at every frame rate and may be easier on the stomach.", "原作式では当時と同じ端数処理を行います。なめらか設定では端数処理を省き、画面の更新回数に左右されにくくなります。" },
        { "In the editor the Game view decides the size - the choice is kept and takes effect in the built game.", "Unity編集画面では表示サイズを変更できません。設定は保存され、配布版の起動時に反映されます。" },
        { "Takes effect at once and is kept for the next start.", "変更はすぐに反映され、次回の起動時も引き継がれます。" },
        { "Pulls apart the colours that are hard to tell apart, for the whole game. It is the usual daltonisation: what the eye loses is put back into the channels it can see.", "区別しにくい色を見分けやすくするため、ゲーム全体の色を調整します。" },
        { "The flask is red normally and green while poisoned - the one pair a red-green deficiency cannot tell apart.", "体力瓶は通常は赤、毒状態では緑になります。赤と緑の区別が難しい場合のための設定です。" },
        { "Click an entry to change it, then press the new button on the gamepad. Escape on the keyboard keeps the old one. One stick walks, the other looks (in the classic scheme it turns and tilts) - swap them below.", "変更したい項目を押してから、ゲームパッドの新しいボタンを押してください。Escで変更を取り消します。左右スティックの役割も交換できます。" },
        { "Click an entry to change it, then press the new key or mouse button. Escape keeps the old one. The pointer movement itself is fixed, and the interface panels always use the physical mouse buttons. The gamepad has its own tab.", "項目を選び、新しいキーかマウスボタンを押してください。Escで取り消せます。マウスポインターの移動は変更できません。ゲームパッドは別のタブで設定します。" },
        { "Corners, joints and the floor along the walls get darker.", "壁の隅、石の継ぎ目、壁際の床を暗くします。" },
        { "Joints and recesses get darker.", "石の継ぎ目やくぼみを暗くします。" },
        { "Stones stand out from their joints and shift against them as you move, with their shadow.", "石と継ぎ目に陰影を付け、移動時に奥行きが感じられるようにします。" },
        { "A dark patch under creatures and things, so they stand on the ground.", "生物や物の足元に暗い影を描き、地面との接地を分かりやすくします。" },
        { "Dirt where a wall or a step meets the floor hides the hard edge, in the palette's colour nearest the tone. Grime works in the Remastered mode too.", "壁や段差と床の境界に汚れを重ねます。色は原作の色調から近いものを選び、高精細表示でも有効です。" },
        { "Our own effects for the original's palette - they show in the detail level Low only, which is not chosen at the moment. 0 is off.", "原作色調用の独自効果です。画質「低」でのみ有効ですが、現在は別の画質が選択されています。0で無効です。" },
        { "Our own effects for the original's palette (detail level Low). They only make a surface lighter or darker by the original's shade steps, so every pixel keeps a palette colour - but turned up they quickly take away the look of the original. 0 is off.", "原作色調（画質「低」）の独自効果です。原作の色数を維持して明暗を調整します。強くすると当時の画面から離れます。0で無効です。" },
        { "Put the runes on the shelf from the rune bag, then click the shelf to cast. ", "ルーン袋から棚にルーンを並べ、棚をクリックすると呪文を唱えます。" },
        { "Talk to an ankh shrine and type a mantra. It raises your skills only when you ", "アンクの祠でマントラを入力してください。技能を高めるには、" },
        { "Lights up the surroundings for a while.", "しばらく周囲を明るく照らします。" },
        { "Protects against blows like a full suit of armour, for a while.", "しばらく全身鎧を着たように攻撃を防ぎます。" },
        { "Shoots a magic missile at a target.", "狙った敵へ魔法の矢を放ちます。" },
        { "Conjures a meal.", "食事を生み出します。" },
        { "Muffles your steps for a short time, so creatures notice you less.", "足音を抑え、しばらく敵に気づかれにくくなります。" },
        { "Lets you drift down gently for a short time.", "しばらくゆっくり落下できるようになります。" },
        { "Heals light wounds.", "軽い傷を治します。" },
        { "Tells you of creatures nearby that you cannot see.", "見えない場所にいる周囲の生物を知らせます。" },
        { "May make a foe lose heart and run.", "敵を恐れさせ、逃げ出させることがあります。" },
        { "Lays a ward that tells you when something disturbs it.", "侵入したものを知らせる魔法の警戒装置を設置します。" },
        { "You move faster than your foes, for a while.", "しばらく敵より速く動けるようになります。" },
        { "Makes you harder to see for a short time.", "しばらく姿を見つけられにくくします。" },
        { "Lets you see in the dark, for a while.", "しばらく暗闇でも見えるようになります。" },
        { "Hurls a lightning bolt at a target.", "狙った敵へ稲妻を放ちます。" },
        { "Spikes a door shut.", "扉を開けられないよう固定します。" },
        { "Heals serious wounds.", "深い傷を治します。" },
        { "Lifts you straight up into the air for a short time.", "しばらく真上へ浮き上がります。" },
        { "Poisons a foe.", "敵に毒を与えます。" },
        { "Gives some protection from fire for a short time.", "しばらく炎による被害を軽減します。" },
        { "Disarms a trap you aim at.", "狙った罠を解除します。" },
        { "Hurls a fireball at a target.", "狙った場所へ火球を放ちます。" },
        { "Tells you what magic an object holds.", "品物に込められた魔法を調べます。" },
        { "Makes you proof against missiles, for a while.", "しばらく飛び道具による攻撃を防ぎます。" },
        { "Unlocks a locked door or chest.", "鍵のかかった扉や宝箱を開きます。" },
        { "Cures poison.", "毒を治します。" },
        { "Heals you completely.", "体力を完全に回復します。" },
        { "Takes you to your moonstone at once.", "自分のムーンストーンの場所へ瞬時に移動します。" },
        { "Holds a foe in place.", "敵をその場に縛り付けます。" },
        { "Bright light for a long time.", "長い間、明るい光を放ちます。" },
        { "Lets you take and use a single object from afar, for a while.", "離れた場所の品物を一つ取ったり使ったりできます。" },
        { "Lets you fly for a while, then glide to the ground.", "しばらく飛行できます。効果が切れるとゆっくり降下します。" },
        { "Turns a creature to fight on your side.", "生物を味方につけて戦わせます。" },
        { "Makes you nearly invisible, for a while.", "しばらくほとんど見えなくなります。" },
        { "Makes the foes around you stagger as if drunk.", "周囲の敵を酔ったようにふらつかせます。" },
        { "Reveals hidden objects and secret ways around you.", "周囲の隠された物や秘密の通路を見つけます。" },
        { "Greatly hardens you against damage, for a while.", "しばらく受けるダメージを大幅に減らします。" },
        { "Makes the ground quake and the rocks burst.", "地面を揺らし、岩を砕きます。" },
        { "Lets you see the world from above, for a while.", "しばらく上空から周囲を見渡せます。" },
        { "Rains flaming missiles on the area.", "周囲に燃える飛び道具を降らせます。" },
        { "Stops time for everyone but you, for a while.", "しばらく自分以外の時間を止めます。" },
        { "Improves some of your combat skills.", "戦闘に関する技能をいくつか向上させます。" },
        { "Improves some of your magic skills.", "魔法に関する技能をいくつか向上させます。" },
        { "Improves some of your other skills.", "その他の技能をいくつか向上させます。" },
        { "Tells you where the Cup of Wonder lies.", "驚異の杯の場所を教えてくれます。" },
        { "Gives you the Key of Truth.", "真実の鍵を授けてくれます。" },
        { "Colour strength", "補正の強さ" },
        { "Dexterity", "敏捷" },
        { "Intelligence", "知力" },
        { "Vitality", "体力" },
        { "Mana", "魔力" },
        { "Experience", "経験値" },
        { "Can still carry", "残りの積載量" },
        { "Food", "空腹" },
        { "Fatigue", "疲労" },
        { "Poison", "毒" },
        { "Hallucination", "幻覚" },
        { "Unspent skill points", "未使用の技能点" },
        { "Exact experience", "正確な経験値" },
        { "Avatar", "アバタール" },
        { "Put the runes on the shelf from the rune bag, then click the shelf to cast. A spell costs three mana per circle, and your level halved (rounded up) must reach its circle. Click a spell here to lay its runes into the hollow; grey runes are not in your bag yet.", "ルーン袋から棚にルーンを並べ、棚をクリックすると呪文を唱えます。呪文の消費魔力は環の数の3倍です。唱えるにはレベルの半分（端数切り上げ）が必要です。呪文をクリックするとルーンを棚に並べられます。灰色のルーンはまだ持っていません。" },
        { "Talk to an ankh shrine and type a mantra. It raises your skills only when you have earned a skill point.", "アンクの祠でマントラを唱えてください。技能を伸ばすには未使用の技能点が必要です。" },
        { "Only for Original. Even: the picture moves a little with every screen refresh. Stepped: the picture only moves when the game has calculated, like on an old PC. Smooth is always even.", "原作式でのみ有効です。「均等」では画面更新のたびに少し動き、「段階式」では移動計算時にだけ動きます。なめらか設定では常に均等です。" },
        { "How often per second the Original mode calculates. More is not faster. The original rounds down after every calculation, so with 64 you move a bit slower when swimming or walking backwards, and you reach full speed sooner. With 16 it is the other way round. 32 is like the original in DOSBox at 30000 cycles. Smooth does not use this setting.", "原作式の移動計算を1秒間に何回行うかを決めます。回数を増やしても必ずしも速くなりません。原作の端数処理により、64では後退や水泳が少し遅くなり、加速は速くなります。16はその逆です。32はDOSBoxの30000サイクル相当です。なめらか設定には影響しません。" },
        { "How long it takes to get going and to stop in Smooth. 0.6 s is the original on a slow PC, 0.3 s on the PC that DOSBox at 30000 cycles stands for. Shorter feels more direct. Original uses its own frame rate for this.", "なめらか設定で動き始め・停止するまでの時間です。遅いPCの原作では0.6秒、DOSBoxの30000サイクル相当では0.3秒でした。短いほど操作が機敏になります。" },
        { "The bobbing of the view and the weapon while walking: like the original, smoothed with your own strength, or off if it makes you uneasy.", "歩行時の視点や武器の揺れを設定します。原作どおり、強さを調整したなめらかな動き、または無効から選べます。" },
        { "Sound comes from the emulated AdLib card, as in 1992; the music can also play its MT-32 version on General MIDI. The original only knew on and off, and the same two switches sit in the options panel in the game; the volumes are ours. The two switches belong to the save game, as in the original - a save loaded here brings the state it was saved with, the volumes stay.", "音は1992年当時と同じAdLibの再現音源を使います。音楽はMT-32版をGeneral MIDIで再生することもできます。原作の音楽・効果音の入切はセーブデータに記録されます。音量はこの移植版独自の設定で、読み込み後も維持されます。" },
        { "General MIDI: the game's MT-32 music on a soundfont cut from FluidR3 GM (Frank Wen, MIT), its instruments matched to the MT-32's by name - close, not the original's sound. MT-32: the same music on Munt's emulation of the Roland MT-32, as the game was written for it. Its ROMs belong to Roland and do not come with the game; put your own control and PCM ROM into the folder below.", "General MIDIはMT-32の楽曲を対応する音色で再生しますが、原音の完全な再現ではありません。MT-32はMuntを使い、原作向けの音色を再現します。ROMはゲームには同梱されません。ご自身のコントロールROMとPCM ROMを指定フォルダーに置いてください。" },
        { "Torches, candles, lanterns, fires, lava, glowing stones, mushrooms and orbs light their surroundings by the original's own light table, walls cast shadows, fire flickers. The original lights only from you, so lit places beyond your light show here, up to its farthest drawing distance.", "松明、ろうそく、ランタン、炎、溶岩、発光する石やキノコなどが、原作の照明表に従って周囲を照らします。壁は影を落とし、炎は揺らぎます。原作より離れた明るい場所も描かれます。" },
        { "UW interface font", "画面の文字" },
        { "The mouse either turns the view (pointer locked, a crosshair in the middle) or moves a free pointer over the windows and the world. Only the right button switches between the two.", "マウスは視点を動かす固定操作と、画面内を自由に動くポインター操作を切り替えられます。右ボタンで切り替えます。" },
        { "Free the pointer, or lock it again to look around. The game menu has the other way round as an option: the pointer stays free and the view turns while the button is held.", "ポインターを自由にするか、視点操作へ戻します。ゲームメニューでは、右ボタンを押している間だけ視点を動かす設定も選べます。" },
        { "On a thing in the world: its menu - Talk, Use, Look, Pick up. Held and dragged: take it onto the pointer.", "世界の物を左クリックすると、話す・使う・調べる・拾う等のメニューを開きます。長押ししてドラッグすると持ち上げます。" },
        { "Pointer locked: look up for an overhead bash, down for a thrust, straight ahead for a slash. Pointer free: the upper third of the screen bashes, the lower third thrusts, the middle slashes.", "視点固定中は上向きで振り下ろし、下向きで突き、正面で斬ります。自由ポインターでは画面の上・下・中央がそれぞれの攻撃に対応します。" },
        { "The rune panel. Its handle at the left edge opens it as well, and its pin keeps it out.", "ルーン画面を開きます。左端の取っ手からも開けられ、ピンで固定できます。" },
        { "Click a rune to lay it in the hollow (the fourth pushes out the first), right click to look at it, the strip below the hollow clears it. Click the hollow to cast, or drag it onto the action bar.", "クリックしたルーンを棚に並べます。4つ目を置くと最初の1つが押し出されます。右クリックで調べ、棚の下で消去できます。棚をクリックすると詠唱し、行動バーへも登録できます。" },
        { "Every spell by circle, grey what you cannot cast now. Click one to lay its runes, drag it onto the action bar.", "呪文を環ごとに表示し、使用できない呪文は灰色になります。クリックでルーンを棚に並べ、ドラッグで行動バーへ登録します。" },
        { "A spell that needs a target flies at once at the crosshair with the pointer locked. With the pointer free the target cursor waits for a left click; Escape lets the spell go.", "対象を指定する呪文は、視点固定中なら照準へ直ちに発動します。自由ポインターでは左クリックで対象を選び、Escで取り消せます。" },
        { "Take a thing and put it down - drag it, or click and click. On a container it goes inside; outside the windows it is dropped (low on the screen) or thrown (higher).", "品物はドラッグかクリックで移動できます。容器へ置くと中に入り、画面外へ置くと足元に落とすか、上方なら投げます。" },
        { "A thing's menu: Use, Equip, Look, Pick up, Split. A container opens or closes at once (Shift for its menu). What is used on something else - a key, the oil flask - then waits for a left click on its target.", "品物のメニューから使う・装備・調べる・拾う・分割を選べます。容器は直接開閉し、Shiftでメニューを表示します。鍵などは対象の物を左クリックして使います。" },
        { "The action bar. Drop a thing or drag a spell onto a slot; the key or a click uses it, a weapon goes into your hand. Drag a slot off the bar to empty it.", "行動バーに品物や呪文をドラッグして登録します。対応キーやクリックで使え、武器なら手に持ちます。枠から外へドラッグすると解除できます。" },
        { "Drag your things from the bags onto \"Your offer\". A left click on a trade slot marks it for the deal (a gold ring), a right click looks at it; drag your offer back out when you change your mind.", "袋から品物を自分の提示欄へドラッグします。取引枠の左クリックで提示を確定し、右クリックで調べられます。考え直した時は品物をドラッグして戻せます。" },
        { "The big map, with the map in your pack. The minimap top right: + and - or the mouse wheel zoom it, a click opens the big map.", "所持している地図を大きく表示します。右上の小地図は＋・－やホイールで拡大縮小でき、クリックすると大地図を開きます。" },
        { "In the view: walk towards the pointer, held. On the frame: the icons, the runes, the compass, and taking and putting down things.", "画面内では左ボタンを押し続けるとポインターの方向へ歩きます。枠の上では道具、ルーン、方位計や品物の移動を操作します。" },
        { "Do what the lit icon says - look by default; talk, get, use after its icon. With the weapon drawn: held to charge, let go to strike; the third of the view the pointer is in chooses the strike.", "選択されたアイコンの操作を行います。初期状態では調べる操作で、話す・拾う・使う等を選べます。武器を構えている時は長押しで溜め、離すと攻撃し、画面内の位置で攻撃方向が変わります。" },
        { "The character panel: the paperdoll, your attributes and skills. Its pin keeps it out.", "キャラクター画面には装備、基本能力、技能を表示します。ピンで開いたままにできます。" },
        { "The controls of the original: the pointer stays free, the view lies in the frame.", "原作どおりの操作です。ポインターは自由に動かせ、中央の枠に世界が表示されます。" },
        { "With the pointer free the pad is the mouse. Under a window that holds the game - the menu, a conversation, the map - the pointer is free by itself.", "ポインターを自由にするとゲームパッドをマウスとして使えます。メニュー・会話・地図を開いている間は自動的に自由になります。" },
        { "The pointer", "ポインター" },
        { "Moving", "移動操作" },
        { "Acting", "調べる・使う" },
        { "Combat", "戦闘" },
        { "Magic", "魔法" },
        { "The bags and the character", "荷物とキャラクター" },
        { "Conversation", "会話" },
        { "Map, menu and the rest", "地図・メニュー・その他" },
        { "The mouse", "マウス操作" },
        { "Keys", "キー操作" },
        { "The strike", "攻撃方向" },
        { "Bow, sling", "弓・スリング" },
        { "The head", "頭" },
        { "Targets", "対象の指定" },
        { "Runes tab", "ルーン一覧" },
        { "Spells tab", "呪文一覧" },
        { "Active spells", "使用中の呪文" },
        { "Trading", "取引" },
        { "Locked", "視点固定" },
        { "Free", "自由操作" },
        { "Mouse", "マウス" },
        { "E (tap)", "E（短押し）" },
        { "E (hold)", "E（長押し）" },
        { "The game menu.", "ゲームメニューを開きます。" },
        { "In a menu: the next entry.", "メニュー内の次の項目を選びます。" },
        { "Gamepad: the free pointer", "ゲームパッド：自由ポインター" },
        { "Left stick", "左スティック" },
        { "Right stick", "右スティック" },
        { "Changeable in the Controls dialog of the menu bar, its Gamepad tab - the button names too.", "上部メニューの「操作」→「ゲームパッド」でボタンを変更できます。ボタン表記も設定できます。" },
        { "Changeable under Controls in the menu bar at the top edge.", "キーの割り当ては上部メニューの「操作」で変更できます。" },
        { "The pad's buttons are the mouse's buttons, where the pointer is. Changeable in the Controls dialog of the menu bar, its Gamepad tab.", "ゲームパッドのボタンはポインター位置でマウスボタンとして動きます。上部メニューの「操作」から割り当てを変更できます。" },
        { "Walk and step sideways.", "前後に歩き、左右に横移動します。" },
        { "Walk forward and back, step sideways.", "前後に歩き、左右に横移動します。" },
        { "Walk; pushed well sideways: step sideways.", "歩行します。大きく横に倒すと横移動します。" },
        { "Look around.", "周囲を見回します。" },
        { "Look at it.", "対象を調べます。" },
        { "Turn and look up and down (pointer locked).", "視点固定時に左右を向き、上下を見ます。" },
        { "Turn; up and down tilt the view a step.", "左右を向き、上下で視点を一段階傾けます。" },
        { "Jump.", "ジャンプします。" },
        { "While flying or levitating: rise and sink.", "飛行・浮遊中に上昇と下降を行います。" },
        { "The usual thing for what you aim at: pick it up, talk, open a door, pull a lever, use it.", "狙ったものに応じて、拾う、話す、扉を開ける、レバーを引くなどの操作をします。" },
        { "Use it directly, even what could be picked up - a sack is emptied instead of taken.", "拾えるものでも、その場で直接使います。袋なら持たずに中身を開きます。" },
        { "Draw or put away the weapon in your hand.", "手に持った武器を構えるかしまいます。" },
        { "Charge the weapon; let go to strike. The power gem shows the charge.", "押し続けて攻撃を溜め、離すと打撃します。パワージェムに溜め具合が表示されます。" },
        { "Draw the weapon; held: charge, let go to strike.", "武器を構えます。押し続けて溜め、離すと攻撃します。" },
        { "Put the weapon away.", "武器をしまいます。" },
        { "Charge as above; the red target circle shows when the charge is enough. The shot flies to the free pointer, else to the crosshair.", "弓なども溜めてから放ちます。赤い照準が溜め完了を示し、自由ポインターの位置または照準に向けて発射します。" },
        { "Top right. Left click: what it does; right click: end it.", "右上に表示されます。左クリックで効果を確認し、右クリックで解除します。" },
        { "Open or close the bags. The backpack always shows at the bottom right.", "荷物画面を開閉します。背負い袋は常に右下に表示されます。" },
        { "The character panel; held: every bag.", "短押しでキャラクター画面、長押しですべての袋を開きます。" },
        { "Split a stack.", "重なった品物を分けます。" },
        { "Food and drink put down on the paperdoll's head are eaten.", "装備画面の頭の位置に食べ物や飲み物を置くと口にします。" },
        { "Choose an answer - or click it. The mouse wheel scrolls the conversation.", "番号キーやクリックで返答を選びます。ホイールで会話をスクロールできます。" },
        { "This help, in the character panel.", "キャラクター画面のヘルプを開きます。" },
        { "Save, restore.", "セーブとロードを行います。" },
        { "Switch to the classic controls.", "原作式の操作に切り替えます。" },
        { "Switch to the modern controls.", "現代式の操作に切り替えます。" },
        { "Cast the runes in the hollow; held: the rune panel.", "棚のルーンで呪文を唱えます。長押しでルーン画面を開きます。" },
        { "Pick a slot of the action bar.", "行動バーの枠を選びます。" },
        { "Use the slot.", "選択中の枠を使います。" },
        { "Free the pointer (onto the backpack) or lock it again.", "ポインターを自由にするか、視点固定に戻します。" },
        { "Move the pointer.", "ポインターを動かします。" },
        { "The left mouse button.", "マウスの左ボタンです。" },
        { "The left mouse button; held: drag.", "マウスの左ボタンです。長押しでドラッグします。" },
        { "The right mouse button.", "マウスの右ボタンです。" },
        { "Over a window: scroll. In a menu: the next entry.", "画面上ではスクロールし、メニュー内では次の項目を選びます。" },
        { "With the pointer on the stick, over the frame: scroll.", "スティックでポインターを動かしている時、枠の上ではスクロールします。" },
        { "Sink and rise while flying.", "飛行中に下降・上昇します。" },
        { "Close what is open.", "開いている画面を閉じます。" },
        { "Use what you aim at; held: use it directly.", "狙ったものを使います。長押しでその場で直接使います。" },
        { "The map; held: this help.", "地図を開きます。長押しでこのヘルプを開きます。" },
        { "The stick moves the pointer instead, or turns again.", "スティックによる自由ポインターと視点操作を切り替えます。" },
        { "   <i>(in the hollow)</i>", "   <i>（棚に配置済み）</i>" },
        { "Walk forward", "前進" },
        { "Walk backward", "後退" },
        { "Left (modern: strafe)", "左へ（現代式：横移動）" },
        { "Right (modern: strafe)", "右へ（現代式：横移動）" },
        { "Turn left (classic)", "左を向く（原作式）" },
        { "Turn right (classic)", "右を向く（原作式）" },
        { "Strafe left (classic)", "左へ横移動（原作式）" },
        { "Strafe right (classic)", "右へ横移動（原作式）" },
        { "Turn left", "左を向く" },
        { "Turn right", "右を向く" },
        { "Strafe left", "左へ横移動" },
        { "Strafe right", "右へ横移動" },
        { "Jump", "ジャンプ" },
        { "Jump (second key)", "ジャンプ（予備キー）" },
        { "Sink while flying", "飛行中に下降" },
        { "Rise while flying", "飛行中に上昇" },
        { "Look down (classic)", "下を向く（原作式）" },
        { "Look straight (classic)", "正面を向く（原作式）" },
        { "Look up (classic)", "上を向く（原作式）" },
        { "Look down", "下を向く" },
        { "Look straight", "正面を向く" },
        { "Look up", "上を向く" },
        { "Character panel (modern)", "キャラクター画面（現代式）" },
        { "Bags (modern)", "荷物（現代式）" },
        { "Look (modern)", "調べる（現代式）" },
        { "Runes and spells (modern)", "ルーンと呪文（現代式）" },
        { "Switch control scheme (with Shift)", "操作方式の切替（Shift併用）" },
        { "Walk, drag, attack", "歩く・ドラッグ・攻撃" },
        { "Look and use", "調べる・使う" },
        { "Open the map", "地図を開く" },
        { "Switch the font", "フォントを切り替える" },
        { "Open the help (not with Alt)", "ヘルプを開く（Alt併用は除く）" },
        { "Options", "設定" },
        { "Talk mode", "会話モード" },
        { "Get mode", "拾うモード" },
        { "Look mode", "調べるモード" },
        { "Fight (draw the weapon)", "戦闘（武器を構える）" },
        { "Use mode", "使うモード" },
        { "Turn the panel", "操作パネルを切り替える" },
        { "Cast the runes on the shelf", "棚のルーンで呪文を唱える" },
        { "Track (look for creatures nearby)", "追跡（近くの生物を探す）" },
        { "Make camp", "野営する" },
        { "Use (held: use directly)", "使う（長押しで直接使う）" },
        { "Look", "調べる" },
        { "Close what is open", "開いている画面を閉じる" },
        { "Character panel (held: every bag)", "キャラクター画面（長押しですべての袋）" },
        { "Draw the weapon and strike", "武器を構えて攻撃" },
        { "Draw or put away the weapon", "武器を構える・しまう" },
        { "Cast the runes (held: rune panel)", "呪文詠唱（長押しでルーン画面）" },
        { "Action bar: previous slot", "行動バー：前の枠" },
        { "Action bar: next slot", "行動バー：次の枠" },
        { "Action bar: use the slot", "行動バー：選択中の枠を使う" },
        { "Open the map (tapped)", "地図を開く（短押し）" },
        { "Menu", "メニュー" },
        { "Help (held)", "ヘルプ（長押し）" },
        { "Pointer on the stick, on or off", "スティックのポインター切替" },
        { "Pointer: click (held: drag)", "ポインター：クリック（長押しでドラッグ）" },
        { "Pointer: click, second button", "ポインター：クリック（予備ボタン）" },
        { "Pointer: right button", "ポインター：右ボタン" },
        { "Pointer: right button, second button", "ポインター：右ボタン（予備）" },
        { "no pad", "ゲームパッドなし" },
        { "Next level", "次のレベル" },
        { "Next level at", "次のレベルに必要な経験値" },
        { "Load", "荷物の重量" },
        { "Protection", "防御力" },
        { "Resistance", "耐性" },
        { "Light", "明かり" },
        { "Curse", "呪い" },
        { "Maze navigation", "迷宮の案内" },
        { "Dragon skin boots", "竜皮のブーツ" },
        { "none, this is the highest", "これ以上のレベルはありません" },
        { "shows the way in Tybal's maze", "タイボールの迷宮で進む道を示します" },
        { "no harm from lava", "溶岩の被害を受けません" },
        { " 4x", "4倍" },
        { "One more every 3000 experience and every level.", "経験値3000ごととレベルアップ時に1増えます。" },
    };

    // IMGUI in Unity 6 uses TextCore rather than uGUI's legacy font renderer.
    // Assigning Font.CreateDynamicFontFromOSFont directly to an IMGUI GUIStyle
    // crashes its layout engine on this editor version (it does not have embedded
    // font data). Instead use the project's imported Liberation Sans Font, whose
    // importer has installed Japanese OS fonts in fontNames as glyph fallbacks.
    // This keeps a valid source font for IMGUI and never bundles a Japanese font.
    public static bool IsAvailable =>
        UWUiFonts.JapaneseFontFor("日本語") != null && UWInterfaceFont.Font != null;

    public static Font FontOr(Font fallback)
    {
        return IsAvailable ? UWInterfaceFont.Font : fallback;
    }

    public static string Text(string original)
    {
        if (!IsAvailable || original == null) return original;
        if (Translations.TryGetValue(original, out string japanese)) return japanese;
        const string desktopSuffix = "   (desktop)";
        if (original.EndsWith(desktopSuffix, StringComparison.Ordinal))
            return original.Substring(0, original.Length - desktopSuffix.Length) +
                "   （デスクトップ）";
        const string mt32NoRoms = "MT-32: no MT-32 or CM-32L ROMs in that folder";
        if (original == mt32NoRoms)
            return "MT-32：指定フォルダーにMT-32／CM-32LのROMがありません";
        const string mt32EmulationMissing = "MT-32: the MT-32 emulation is missing on this system (";
        if (original.StartsWith(mt32EmulationMissing, StringComparison.Ordinal) &&
            original.EndsWith(")", StringComparison.Ordinal))
            return "MT-32：エミュレーターが見つかりません（" +
                original.Substring(mt32EmulationMissing.Length, original.Length - mt32EmulationMissing.Length - 1)
                + "）";
        const string manualFooter = "      Arrow keys or the wheel turn the pages, Esc or a click on the page closes";
        if (original.EndsWith(manualFooter, StringComparison.Ordinal))
        {
            string pages = original.Substring(0, original.Length - manualFooter.Length);
            return Text(pages) + "　　矢印キー／ホイール：ページ移動。Escまたはページをクリック：閉じる。";
        }
        if (original.StartsWith("Pages ", StringComparison.Ordinal))
        {
            string[] parts = original.Split(' ');
            if (parts.Length == 6 && parts[0] == "Pages" && parts[2] == "and" && parts[4] == "of")
                return "全" + parts[5] + "ページ、" + parts[1] + "・" + parts[3] + "ページ目";
        }
        if (original.StartsWith("Circle ", StringComparison.Ordinal) &&
            original.EndsWith(" mana", StringComparison.Ordinal))
        {
            int split = original.IndexOf("  -  ", StringComparison.Ordinal);
            if (split > 0)
                return "第" + original.Substring(7, split - 7) + "環 － 消費魔力" +
                    original.Substring(split + 5, original.Length - split - 10);
        }
        if (original.StartsWith("Page ", StringComparison.Ordinal) &&
            original.Contains(" of "))
        {
            int split = original.IndexOf(" of ", StringComparison.Ordinal);
            return original.Substring(split + 4) + "ページ中 " +
                original.Substring(5, split - 5) + "ページ目";
        }
        if (original.StartsWith("Level ", StringComparison.Ordinal))
            return "レベル " + original.Substring(6);
        if (original.EndsWith(" stones", StringComparison.Ordinal))
            return original.Substring(0, original.Length - 7) + " ストーン";
        if (original.StartsWith("Found:  ", StringComparison.Ordinal))
            return "検出：  " + original.Substring(8);
        if (original.StartsWith("MT-32 ROMs: ", StringComparison.Ordinal))
            return "MT-32 ROM： " + original.Substring(12);
        const string conflictKeyPrefix = "This key is also used for \"";
        const string conflictButtonPrefix = "This button is also used for \"";
        if (original.EndsWith("\".", StringComparison.Ordinal) &&
            (original.StartsWith(conflictKeyPrefix, StringComparison.Ordinal) ||
             original.StartsWith(conflictButtonPrefix, StringComparison.Ordinal)))
        {
            string prefix = original.StartsWith(conflictKeyPrefix, StringComparison.Ordinal)
                ? conflictKeyPrefix : conflictButtonPrefix;
            string other = original.Substring(prefix.Length, original.Length - prefix.Length - 2);
            return (prefix == conflictKeyPrefix ? "このキーは「" : "このボタンは「") +
                Text(other) + "」にも割り当てられています。";
        }
        const string aimedPrefix = "The crosshair aims: E, ";
        const string aimedSuffix = " and the left button act on what it is on.";
        if (original.StartsWith(aimedPrefix, StringComparison.Ordinal) &&
            original.EndsWith(aimedSuffix, StringComparison.Ordinal))
        {
            string key = original.Substring(aimedPrefix.Length,
                original.Length - aimedPrefix.Length - aimedSuffix.Length);
            return "照準を合わせた対象には、Eキー、" + key + "キー、左ボタンで操作できます。";
        }
        const string freePrefix = "The windows take the clicks; E and ";
        const string freeSuffix = " act on what is under the pointer, in the world as in the bags, the character panel and the action bar (holding E does nothing over a window).";
        if (original.StartsWith(freePrefix, StringComparison.Ordinal) &&
            original.EndsWith(freeSuffix, StringComparison.Ordinal))
        {
            string key = original.Substring(freePrefix.Length,
                original.Length - freePrefix.Length - freeSuffix.Length);
            return "画面内のクリックは各ウィンドウが受け取ります。Eキーと" + key +
                "キーはポインターの下にある世界の物や荷物、装備画面、行動バーを操作します。ウィンドウ上でのE長押しは無効です。";
        }
        const string campPrefix = "The bedroll and the boots beside the paperdoll's feet - or ";
        if (original.StartsWith(campPrefix, StringComparison.Ordinal) &&
            original.EndsWith(".", StringComparison.Ordinal))
        {
            string keys = original.Substring(campPrefix.Length, original.Length - campPrefix.Length - 1);
            return "装備画面の足元にある寝袋と靴、または" +
                keys.Replace(" and ", "と") + "キーで野営・追跡ができます。";
        }
        const string escapePrefix = "Closes what is open - a waiting spell, the bags, the panels - then the game menu (";
        const string escapeSuffix = " as well).";
        if (original.StartsWith(escapePrefix, StringComparison.Ordinal) &&
            original.EndsWith(escapeSuffix, StringComparison.Ordinal))
        {
            string key = original.Substring(escapePrefix.Length,
                original.Length - escapePrefix.Length - escapeSuffix.Length);
            return "待機中の呪文、荷物や各種画面を順に閉じ、最後にゲームメニューを開きます。" +
                key + "キーでも開けます。";
        }
        return original;
    }

    public static void Label(Rect rect, string original, GUIStyle style)
    {
        GUI.Label(rect, Text(original), style);
    }

    public static void Label(Rect rect, GUIContent content, GUIStyle style)
    {
        GUI.Label(rect, content, style);
    }

    public static bool Button(Rect rect, string original, GUIStyle style)
    {
        return GUI.Button(rect, Text(original), style);
    }

    public static bool Toggle(Rect rect, bool enabled, string original, GUIStyle style)
    {
        return GUI.Toggle(rect, enabled, Text(original), style);
    }

    public static void LayoutLabel(string original, GUIStyle style, params GUILayoutOption[] options)
    {
        GUILayout.Label(Text(original), style, options);
    }

    public static void LayoutLabel(Texture image, GUIStyle style, params GUILayoutOption[] options)
    {
        GUILayout.Label(image, style, options);
    }

    public static bool LayoutButton(string original, GUIStyle style, params GUILayoutOption[] options)
    {
        return GUILayout.Button(Text(original), style, options);
    }
}
