# Roadmap

What is finished, what is still open, and what comes later. The README's "State" section
lists what is in; this file lists what is not, or not yet confirmed against the original.

"Confirmed" means a behaviour was compared with the original game running in DOSBox. Much
of what is built was also read out of the original's executable.

## Finished

- The whole game of Ultima Underworld 1 can be started, played and saved: all nine levels,
  the Void and the end sequence
- World, objects, doors, locks, traps and triggers, including all trap actions and the
  special deaths
- Combat, magic, creatures with their goals and attitudes, the group alarm, theft, respawn
- Conversations on the original bytecode, trading, repairing
- Character creation, inventory, automap, sleeping and dreams, hunger and fatigue
- What the automap reveals: the original's own visibility sweep (two edge rays from the exact
  position in the tile, clipped at walls and tile corners), ported from the executable
- Save games that the original can load and vice versa
- Intro, cutscenes, AdLib music and sound effects, the classic interface with its
  animations, the 4:3 display of the original
- A full play-through, compared step by step with the original: from character creation
  through the eight talismans and the Key of Infinity to the end of the game
- The Palette and Remastered render modes
- Effects of the port's own for the Palette mode, every pixel still a palette colour: ambient
  occlusion, grime along the walls with a colour, wall depth with its shadow, darker joints,
  ground shadows, glowing lava and flames, and light sources by the original's light table with
  walls casting shadows - presets Off, Subtle, Full (asked for on Reddit); the grime in the
  Remastered mode too
- The original's keys: F1 to F10 and the Ctrl shortcuts (save, restore, music, sound, detail,
  quit) work as in the original; the port's own keys sit on Shift, the help window on Tab
- The port's own additions: the help window beside the view (stats with the hidden values,
  a live map, notes kept with the save game, spells, mantras and the game's manual), a modern
  readable font, the original's 4:3 frame on wide screens, the world at the original's
  resolution or a multiple of it up to 4x (one pixel row per row of the 320x200 screen at 1x,
  the width following the screen, the interface at full resolution; asked for on Reddit), dealing the attribute points at
  character creation yourself
- The modern control scheme: free mouse look, WASD, an action bar, bags, a character panel and
  a rune panel, a minimap, a conversation screen with trading, and a layout editor to move and
  size every part of it, with an inspector for each part's settings - switched with Shift+F2.
  Interface presets: Modern; Classic Modular, the original's own pieces as parts of their own (the
  compass on its stone disc, the message scroll, the power gem, the flasks, the hollow with the
  prepared runes, the rune tablet, the stats panel, the character page with the original
  inventory, the conversation in the original's look, the original's font); Classic Wide, the
  classic frame stretched over a wide screen at fixed places, the stone grown anew instead of
  stretched, with the dragons and the pieces on it; or a layout of one's own, which can start
  from any preset and is only saved when the editor is left. Every part can
  wear a background in the original's colours and patterns; the original's pictures keep the
  pixel proportion of the 4:3 screen they were drawn for
- The classic scheme always at 4:3, and optionally widened to the screen the same way as Classic
  Wide (the conversation and the map stay 4:3); a slider for the view angle of the 3D view
- The motion of the player, the creatures and everything thrown or shot as the original's
  own code, read out of the executable and run without Unity physics: slopes, ledges, the
  water edge, the deflection off walls, jumping, swimming, levitation and slow fall behave as
  in the original. Under *Motion* in the menu bar the arithmetic can run call by call as
  the original did, or as a precise, frame-rate independent computation of the same rules
- A Linux build (x86_64), tested in a Kubuntu VM
- A gamepad in both control schemes: the sticks to walk and look, the buttons to act, a pointer
  on the stick for every window, a letter grid for typing, glyphs in the help (Xbox,
  PlayStation, Nintendo - chosen, not guessed), every button rebindable, swapped sticks, a
  deadzone per stick; tested with an Xbox controller
- The music's MT-32 version: on General MIDI with a soundfont that comes with the game
  (cut from FluidR3 GM, the instruments matched to the MT-32's by name), and on the Roland
  MT-32 itself through Munt's emulation with the player's own ROMs - compared with a recording
  of the original on a real MT-32

## Open: gameplay parity

- Nothing known at the moment beyond what the play-through compared. Reports of
  differences to the original are welcome.

## Open: controls

- **Gamepad, further.** Vibration, and a Steam Deck layout; the gamepad itself is in place
  (see above). On Linux its pointer clicks through X11 (XTest), not in a native Wayland session.

## Open: platforms

- **macOS.** No build yet, untested.
- **Linux on 64-bit ARM (a Raspberry Pi 5, say).** No build yet, untested. Unity can build for
  ARM64 Linux (with IL2CPP instead of Mono, and the PDFium library for the manual as an ARM64
  build). A rough guess: the CPU is plenty for the game logic, the GPU is the limit - the
  Palette mode at around 720p, the Remastered mode's effects likely too heavy; the world at the
  original's resolution (in the Graphics menu) would help most. Asked on Reddit.
  An outlook further on, also asked there: PortMaster, the port collection for Linux handhelds.
  Most of its devices offer only OpenGL ES 2 without Vulkan, which Unity 6 no longer serves, so
  only the stronger handhelds with Vulkan come into question, and only with the gamepad scheme.

## Open: presentation

- **Music, further.** The sound effects on the MT-32 too, as the original may have played them (its file `UW.MT` holds 64
  timbres of their own - unchecked). Suggested on Reddit: recordings the player supplies, one
  file per track, played in place of the synthesizer when present.
- **Hallucination.** All three pictures the original rolls from are built and confirmed, two
  of them as approximations: the "light table" the original copies from its own engine
  variables is modelled on screenshots (those variables' run-time values are not in the
  file), and the scrambled textures show straight streaks where the original swirls (its
  texture mapper could not be read).
- **Remastered mode.** Metal is not told apart from other materials (the palette has no
  established range for it); more effects are planned. Planned: items and creatures get a
  depth estimated from their outline, so they meet floors and walls more softly and the
  lights shade them.
- **More for the Palette mode.** A shore line along water, a few levels lighter and moving
  with the palette rotation, is an idea that may look odd - not planned yet.
- **The original font traced into outlines** (an idea from Reddit), so it stays
  sharp at any size - either pixel-exact or smoothed, keeping the letter shapes without the
  pixel steps. Generated from the player's own game files at run time, as the font belongs to
  the game.
- **The own-tile rule** of the Remastered mode (an object is never covered by the tile it stands
  in) does not yet cover diagonal walls of that tile. The Palette mode now places items and
  creatures in the original's draw order instead (tile by tile, doors split the objects of their
  tile, a bridge's deck covers what lies under it); the order of items among themselves at a door
  still follows the simpler rule.

## Open: translations

- **Other languages.** Support for translations is planned, before Ultima Underworld II.

## Later

- **VR support (Japanese-localization fork initiative; in progress).** The Windows OpenXR
  prototype compiles and builds with opt-in headset tracking, gaze interaction rays,
  Touch-controller bindings (including UI clicking and pointer toggling), a headset
  reticle and a temporary stereo-camera UI bridge. Offline
  validation passes. Head-relative movement, adjustable tracking scale and physically
  spaced menu/inventory canvases are included in a newly verified Windows 64-bit
  development build (2026-10-14, errors: 0), using Unity Hub-mediated licensing.
  VR-only inventory slot-to-slot thumbstick navigation and direct Touch bag
  toggling are implemented without replacing original drag/drop hit tests;
  their offline checks and a new Windows prototype build pass (2026-10-14,
  errors: 0). The VR right thumbstick can now navigate all eleven original
  paperdoll equipment hit zones as well as bag slots, even across panels;
  a separate configurable stereo inventory distance keeps bags and the
  character panel aligned, with the reticle on their near side. Real-headset
  interaction, tracked-hand gestures and genuinely spatial inventory panels
  still require further work.
  The equipment-and-depth Windows VR prototype passes offline validation and
  a fresh Windows x64 development build (2026-10-14, zero errors); the headset
  view and controller gestures remain untested.
  Experimental opt-in world-anchored inventory (-vr-spatial-inventory): the
  existing bag/character canvases render to a transparent offscreen texture
  on a floating panel; a right-hand Touch 6DoF ray maps back to the legacy
  input coordinates. Geometry/coordinate checks pass without an HMD, but the
  GPU texture projection and headset/controller UX await real-device tests.
  Offline Ray/space tests PASS; the new Windows x64 spatial-inventory prototype
  builds with zero errors (2026-10-14), and its ZIP is validated. The normal
  VR mode remains the fallback until real-headset testing proves the new path.
  Spatial inventory now has individual shallow extruded item-icon tokens in
  real backpack/equipment hit areas, plus a separate 6DoF right-hand visual
  for the authoritative CursorItem carried by the game's drag logic. The
  tokens have no colliders and do not replace inventory rules. Item-specific
  meshes and tested physical hand grabbing remain future milestones.
  The updated item-token Windows x64 prototype passes offline checks and
  builds with zero errors (2026-10-14); ZIP verified and earlier versions kept.
  Explicit -vr-near-grab prototype: 0.68m-reachable spatial inventory, right
  Touch proximity and grip synthesized through the original drag/drop model,
  conservative off-panel release, optional haptic impulses and fourteen
  procedurally generated item-form meshes with original icon overlays.
  Headset UI visibility, actual grip/trigger event timing, haptics, physical
  ergonomics and real-world item transfers are not yet verified.
  The hand-grab input now tracks physical grip edges across bag open/close,
  aborts an active grab through the existing inventory return path if the UI
  disappears or loses focus, and keeps a stable mouse-up pointer across the
  deferred Unity input event. 3D tokens highlight the hovered item, and
  event logs provide source/pickup/release/confirmation diagnostics for HMD
  testing. New headset-free edge-condition checks pass (2026-10-14).
  Unity 6000.6.0f1 Windows x64 prototype rebuild also succeeded with zero
  errors, and the 353-entry grip-input-diagnostics ZIP passed package checks.
  In-game item transfers and haptics still need actual Quest verification.
  A separately opt-in -vr-direct-grab route calls the modern bag's original
  press/release methods without Windows synthetic mouse timing. The original
  HandGrab script is retained for A/B testing. Direct hand grips can restore
  off-panel items to their source and carry the displaced item after swaps.
  Headset-free tests exercise real UWInventoryModel take/swap/place transitions;
  runtime HMD input and visual alignment still require actual Quest testing.
  The direct-inventory Windows x64 development build passes its HMD-free checks
  and succeeds with zero errors (2026-10-14). Its separate 354-entry ZIP has
  been checked for OpenXR, both grab launchers and absent original game files.
  Unity 6000.6.0f1 offline checks PASS and the updated hand-grab prototype
  Windows x64 development build succeeds (2026-10-14, zero errors). A new
  353-entry ZIP with eight launch scripts is validated. HMD behavior still
  needs user-side evaluation before enabling this mode by default.
  Next: real-HMD validation, UI/controller interaction, stereo correctness and comfort.
  Japanese translation additions remain
  temporarily paused. See `Docs/VR_FEASIBILITY_JA.md`.

- Move more rules from the Unity side into the engine-free layer, so the game logic can be
  used without Unity.
- Ultima Underworld II.
- A second front end in Unreal Engine beside the Unity one, to go all out on the effects
  (ray tracing among them). Likely after Ultima Underworld II.
- Maybe: Detect Monster as a heatmap on the help window's map tab (an idea, not planned).
- Maybe: cut content brought back as an option, should more of it turn up. Found so far: Thorlson,
  an old warrior with a complete conversation who asks for an honourable death in battle and can
  join the player as an ally, but stands in no level (an idea, not planned).
