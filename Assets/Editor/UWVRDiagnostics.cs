using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UWDataImport.UWData;

/// <summary>Batchmode XR diagnostic that does not rely on an active headset.</summary>
public static class UWVRDiagnostics
{
    [MenuItem("Underworld/VR/Report Configuration")]
    public static void ReportConfiguration()
    {
        bool lOManagementInstalled = Type.GetType(
            "UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management") != null;
        bool lOOpenXRInstalled = Type.GetType(
            "UnityEngine.XR.OpenXR.OpenXRLoader, Unity.XR.OpenXR") != null;

        Debug.Log("[UWVR] XR Plug-in Management assembly available: " + lOManagementInstalled);
        Debug.Log("[UWVR] OpenXR loader assembly available: " + lOOpenXRInstalled);
        Debug.Log("[UWVR] Active build target: " + EditorUserBuildSettings.activeBuildTarget);
        if (!lOManagementInstalled || !lOOpenXRInstalled)
            Debug.LogWarning("[UWVR] XR packages are not yet resolved. Check Package Manager and manifest.json.");
    }

    /// <summary>
    /// Checks VR project wiring and verifies the non-VR ray fallback without
    /// loading a headset, entering Play mode or modifying game scene assets.
    /// For batchmode: -executeMethod UWVRDiagnostics.RunOfflineChecks
    /// </summary>
    [MenuItem("Underworld/VR/Run Offline Checks")]
    public static void RunOfflineChecks()
    {
        XRGeneralSettings lOXR = XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        fRequire(lOXR != null && lOXR.Manager != null, "Standalone XR manager asset missing");
        fRequire(!lOXR.InitManagerOnStart, "XR must stay off in non-VR launches");

        bool lbLoader = false;
        foreach (XRLoader lOLoader in lOXR.Manager.activeLoaders)
            if (lOLoader is OpenXRLoader)
                lbLoader = true;
        fRequire(lbLoader, "Windows Standalone OpenXR loader not assigned");

        OpenXRSettings lOOpenXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        fRequire(lOOpenXR != null, "OpenXR build settings missing");

        bool lbQuestController = false;
        foreach (var lOFeature in lOOpenXR.GetFeatures<OculusTouchControllerProfile>())
            if (lOFeature is OculusTouchControllerProfile lOTouch && lOTouch.enabled)
                lbQuestController = true;
        fRequire(lbQuestController, "Oculus Touch interaction profile not enabled");

        UWControls lOControls = new UWControls();
        try
        {
            fRequire(fHasBinding(lOControls.Player.Move, "<XRController>{LeftHand}/thumbstick"),
                "Quest left hand movement binding missing");
            fRequire(fHasBinding(lOControls.Player.LookStick, "<XRController>{RightHand}/thumbstick"),
                "Quest right hand turning binding missing");
            fRequire(fHasBinding(lOControls.Player.ModernAttack, "<XRController>{RightHand}/triggerPressed"),
                "Quest trigger attack binding missing");
            fRequire(fHasBinding(lOControls.Player.ModernUse, "<XRController>{RightHand}/primaryButton"),
                "Quest action button binding missing");
            fRequire(fHasBinding(lOControls.Player.PadClick, "<XRController>{RightHand}/primaryButton"),
                "Quest menu primary click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadClick, "<XRController>{RightHand}/triggerPressed"),
                "Quest menu trigger click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadContext, "<XRController>{LeftHand}/gripPressed"),
                "Quest menu context click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadPointer, "<XRController>{RightHand}/thumbstickClicked"),
                "Quest pointer toggle binding missing");
            fRequire(fHasBinding(lOControls.Player.ModernBags, "<XRController>{LeftHand}/thumbstickClicked"),
                "Quest backpack toggle binding missing");
        }
        finally
        {
            lOControls.Dispose();
        }

        fRequire(!UWVRBootstrap.IsHeadsetActive, "Offline checks require no active XR session");
        fRequire(!UWVRAim.IsHandAimActive,
            "VR hand-aim must never activate in a desktop or offline session");
        fRequire(!UWVRMeleeMotion.IsEnabled && !UWVRHandWeaponVisual.IsModelVisible,
            "A desktop session must not enable gesture strikes or hide the normal weapon sprite");
        fRequire(!UWVRBladeSweep.IsEnabled,
            "VR sword collision must never run outside an explicitly opted-in XR session");
        fRequire(!UWVRHandLaunch.IsEnabled,
            "VR hand-origin missiles must never run in a desktop/offline session");
        fRequire(!UWVRTwoHandBowDraw.IsEnabled && !UWVRMagicHandFx.IsEnabled,
            "VR two-hand bow and spell visuals must never run during desktop/offline play");
        fRequire(!UWVRCombatHaptics.IsEnabled,
            "Combat haptics must remain disabled outside explicitly opted-in real XR sessions");
        fRequire(!UWVRBootstrap.CombatDiagnosticsRequested,
            "Verbose VR combat telemetry must remain off in desktop/offline sessions");
        fTestVRLeftEyeShift();
        fTestVRMenuStereoAndNavigation();
        fTestVRCombatCalibration();
        fTestVRCombatHaptics();
        fTestVRTwoHandBowAndMagic();
        fTestVRHandMuzzle();
        fTestVRBladeSweep();

        // Run the opt-in sword input through synthetic 90 Hz Touch hand paths.
        // All thresholds are physical metres/second and must not vary when
        // the original's arbitrary dungeon units-per-metre is changed.
        fTestVRStroke(UWVRMeleeMotion.Strike.Slash, new Vector3(0.25f, 0f, 0f));
        fTestVRStroke(UWVRMeleeMotion.Strike.Hack, new Vector3(0f, -0.25f, 0f));
        fTestVRStroke(UWVRMeleeMotion.Strike.Stab, new Vector3(0f, 0f, 0.25f));
        UWVRMeleeMotion lOStationary = new UWVRMeleeMotion();
        for (int li = 0; li < 80; li++)
            lOStationary.Sample(true, new Vector3(li % 2 == 0 ? 0.004f : -0.004f, 0f, 0f),
                Vector3.forward, 0.011f);
        fRequire(!lOStationary.TryFinish(true, out _, out _),
            "Small, repeated right-hand controller jitter must not count as a strike");
        UWVRMeleeMotion lOTeleport = new UWVRMeleeMotion();
        lOTeleport.Sample(true, Vector3.zero, Vector3.forward, 0.02f);
        lOTeleport.Sample(true, Vector3.right * 0.8f, Vector3.forward, 0.02f);
        fRequire(!lOTeleport.TryFinish(true, out _, out _),
            "Tracking jump must not produce an instant damaging swing");
        UWVRMeleeMotion lODropped = new UWVRMeleeMotion();
        lODropped.Sample(true, Vector3.zero, Vector3.forward, 0.02f);
        for (int li = 1; li <= 10; li++)
            lODropped.Sample(true, Vector3.right * (0.025f * li), Vector3.forward, 0.02f);
        lODropped.Sample(false, Vector3.zero, Vector3.forward, 0.02f);
        fRequire(!lODropped.TryFinish(true, out _, out _),
            "Tracking loss after a swing must cancel its pending hit");
        UWVRMeleeMotion lOStale = new UWVRMeleeMotion();
        lOStale.Sample(true, Vector3.zero, Vector3.forward, 0.02f);
        for (int li = 1; li <= 10; li++)
            lOStale.Sample(true, Vector3.right * (0.025f * li), Vector3.forward, 0.02f);
        for (int li = 0; li < 35; li++)
            lOStale.Sample(true, Vector3.right * 0.25f, Vector3.forward, 0.02f);
        fRequire(!lOStale.TryFinish(true, out _, out _),
            "An old sword movement must expire instead of scoring a later attack");
        UWVRMeleeMotion lOSlow = new UWVRMeleeMotion();
        for (int li = 0; li < 100; li++)
            lOSlow.Sample(true, Vector3.right * (li * 0.003f), Vector3.forward, 0.02f);
        fRequire(!lOSlow.TryFinish(true, out _, out _),
            "Slow controller drift must not become a melee strike");

        Mesh lOBladeMesh = UWVRHandWeaponVisual.CreateBladeMesh();
        Mesh lOAxeMesh = UWVRHandWeaponVisual.CreateAxeMesh();
        Mesh lOMaceMesh = UWVRHandWeaponVisual.CreateMaceMesh();
        Mesh lOGripMesh = UWVRHandWeaponVisual.CreateCubeMesh();
        try
        {
            fRequire(lOBladeMesh.vertexCount >= 13
                && lOBladeMesh.triangles.Length >= 48
                && lOBladeMesh.bounds.size.z > 0.65f
                && lOBladeMesh.bounds.size.x > 0.06f
                && lOGripMesh.vertexCount >= 8 && lOGripMesh.triangles.Length >= 36,
                "VR tracked weapon placeholder meshes must have complete three-dimensional geometry");
            fRequire(lOAxeMesh.vertexCount >= 24 && lOMaceMesh.vertexCount >= 32
                && lOAxeMesh.bounds.size.x > lOBladeMesh.bounds.size.x
                && lOMaceMesh.bounds.size.y > lOBladeMesh.bounds.size.y,
                "Axe and mace must be distinct from the sword silhouette and have true 3D volume");
            fRequire(UWVRWeaponShape.Classify(WeaponTypes.RightHandSword) == UWVRWeaponShape.Kind.Sword
                && UWVRWeaponShape.Classify(WeaponTypes.LeftHandAxe) == UWVRWeaponShape.Kind.Axe
                && UWVRWeaponShape.Classify(WeaponTypes.RightHandMace) == UWVRWeaponShape.Kind.Mace
                && UWVRWeaponShape.Classify(WeaponTypes.LeftHandFist) == UWVRWeaponShape.Kind.Fist,
                "Original weapon family must select matching VR mesh and blade contact length");
            UWVRWeaponShape.Profile lOSwordProfile = UWVRWeaponShape.Get(UWVRWeaponShape.Kind.Sword);
            fRequire((UWVRWeaponShape.ContactPoint(new Ray(Vector3.zero, Vector3.forward),
                    lOSwordProfile, 1f, 16f) - Vector3.forward * 11.68f).sqrMagnitude < 0.00001f,
                "Physical blade endpoint must match the rendered model when world scale changes");
            fRequire((UWVRHandWeaponVisual.GripOrigin(new Ray(Vector3.zero, Vector3.forward), 16f)
                    - Vector3.back * 0.96f).sqrMagnitude < 0.00001f,
                "Right Touch grip attachment must respect scaled physical metres");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOBladeMesh);
            UnityEngine.Object.DestroyImmediate(lOAxeMesh);
            UnityEngine.Object.DestroyImmediate(lOMaceMesh);
            UnityEngine.Object.DestroyImmediate(lOGripMesh);
        }

        // Simulated Touch poses: right and left can point independently from
        // the HMD gaze. Keep the original eye origin to preserve existing
        // combat reach and world-projectile spawn rules in this first phase.
        Ray lOGazeCombat = new Ray(new Vector3(3f, 4f, 5f), Vector3.forward);
        Ray lORightHandRay = new Ray(new Vector3(7f, 8f, 9f), Vector3.right);
        Ray lOLeftHandRay = new Ray(new Vector3(-2f, 1f, 4f), Vector3.left);
        Ray lOCombatAim = UWVRAim.WithHandDirection(lOGazeCombat, lORightHandRay, true);
        Ray lOMagicAim = UWVRAim.WithHandDirection(lOGazeCombat, lOLeftHandRay, true);
        fRequire((lOCombatAim.origin - lOGazeCombat.origin).sqrMagnitude < 0.00001f
            && (lOMagicAim.origin - lOGazeCombat.origin).sqrMagnitude < 0.00001f
            && (lOCombatAim.direction - Vector3.right).sqrMagnitude < 0.00001f
            && (lOMagicAim.direction - Vector3.left).sqrMagnitude < 0.00001f,
            "Hand-aim must distinguish two controllers while preserving the game attack origin");
        fRequire((UWVRAim.WithHandDirection(lOGazeCombat, lORightHandRay, false).direction
                - lOGazeCombat.direction).sqrMagnitude < 0.00001f
            && (UWVRAim.WithHandDirection(lOGazeCombat, new Ray(Vector3.zero,
                    new Vector3(float.NaN, 1f, 1f)), true).direction
                - lOGazeCombat.direction).sqrMagnitude < 0.00001f,
            "A missing or invalid Touch pose must fall back to head/gaze aim");
        fRequire(Mathf.Abs(UWVRAim.PitchUpDegrees(Vector3.forward)) < 0.001f
            && UWVRAim.PitchUpDegrees(new Vector3(0f, 1f, 2f)) > 12f
            && UWVRAim.PitchUpDegrees(new Vector3(0f, -1f, 2f)) < -12f,
            "Melee attack selection should use the right Touch pitch in hand-aim mode");
        UWProjectileWorld.AimFromDirection(lOCombatAim.direction, 15, out int liRightHeading, out int _);
        UWProjectileWorld.AimFromDirection(lOMagicAim.direction, 15, out int liLeftHeading, out int _);
        fRequire(liRightHeading == 64 && liLeftHeading == 192,
            "Hand-aim projectile and spell headings must follow different Touch hands");
        fRequire((UWVRHandAimGuides.GuideEnd(lORightHandRay, 16f)
                - (lORightHandRay.origin + Vector3.right * 28.8f)).sqrMagnitude < 0.00001f,
            "Touch aim guide must preserve its physical length when world units per metre change");

        fRequire(UWVRWorldSpace.ParseUnitsPerMeter(null) == 1f,
            "VR default scale changed unexpectedly");
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseUnitsPerMeter(
            new[] { "UR.exe", "-vr-units-per-meter=16" }) - 16f) < 0.001f,
            "VR per-metre calibration argument was not parsed");
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseUiDistanceMeters(
            new[] { "UR.exe", "-vr-ui-distance=3.5" }) - 3.5f) < 0.001f,
            "VR interface distance argument was not parsed");
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseInventoryDistanceMeters(
            new[] { "UR.exe", "-vr-inventory-distance=2.5" }) - 2.5f) < 0.001f,
            "VR inventory distance argument was not parsed");
        fRequire(Mathf.Abs(UWVRCanvasBridge.DistanceForCanvasName(
                "Modern bags", 2.25f, 1.8f) - 1.8f) < 0.001f
            && Mathf.Abs(UWVRCanvasBridge.DistanceForCanvasName(
                "Modern character panel", 2.25f, 1.8f) - 1.8f) < 0.001f
            && Mathf.Abs(UWVRCanvasBridge.DistanceForCanvasName(
                "Modern HUD", 2.25f, 1.8f) - 2.25f) < 0.001f,
            "VR inventory planes must match while the HUD stays at its own distance");
        fRequire(!UWVRSpatialInventory.IsEnabled,
            "Headset-free editor test must not create spatial inventory renderers");
        fRequire(!UWVRNearGrab.IsEnabled && !UWVRNearGrab.ReservesRightGrip,
            "Hand proximity and XR grip must never run in desktop mode");
        fRequire(UWVRSpatialInventory.IsInventoryCanvas("Modern bags")
            && UWVRSpatialInventory.IsInventoryCanvas("Modern character panel")
            && !UWVRSpatialInventory.IsInventoryCanvas("Modern HUD"),
            "Spatial inventory must redirect only bags and character panels");

        // The 3D tokens project each original item rectangle on exactly the
        // same 2D UI surface. They are visual-only: no new inventory or
        // collider/physics logic may be needed for taking and dropping them.
        fRequire(UWVRInventoryTokens.TryGetSlotUV(new Rect(160f, 45f, 64f, 36f),
                320f, 180f, out Rect lOTokenUV)
            && (lOTokenUV.center - new Vector2(0.6f, 0.35f)).sqrMagnitude < 0.00001f
            && Mathf.Abs(lOTokenUV.width - 0.2f) < 0.00001f,
            "3D item token does not align with the original clickable item slot");
        fRequire(UWVRInventoryTokens.TryGetSlotUV(new Rect(-30f, 100f, 60f, 60f),
                320f, 180f, out lOTokenUV)
            && Mathf.Abs(lOTokenUV.xMin) < 0.00001f
            && Mathf.Abs(lOTokenUV.width - (30f / 320f)) < 0.00001f,
            "3D token must clip slots partly outside the game display");
        fRequire(!UWVRInventoryTokens.TryGetSlotUV(new Rect(400f, 40f, 10f, 10f),
                320f, 180f, out _)
            && !UWVRInventoryTokens.TryGetSlotUV(new Rect(10f, 10f, 0f, 10f),
                320f, 180f, out _),
            "Invisible or empty inventory slots must not spawn token models");
        fRequire(UWVRInventoryTokens.CategoryShape(UWObject.ObjectCategoryEnum.Containers).z
                > UWVRInventoryTokens.CategoryShape(UWObject.ObjectCategoryEnum.BooksAndScrolls).z,
            "VR item tokens must give containers more depth than books");

        // Every supported item group must generate a real triangle mesh, not
        // a cube with a changed name. These all remain display-only: models
        // are never inserted into the actual UWInventoryModel or world.
        foreach (UWVRItemGeometry.Form leForm in Enum.GetValues(typeof(UWVRItemGeometry.Form)))
        {
            Mesh lOShapeMesh = UWVRItemGeometry.CreateMesh(leForm);
            try
            {
                fRequire(lOShapeMesh != null && lOShapeMesh.vertexCount >= 8
                    && lOShapeMesh.triangles.Length >= 12
                    && lOShapeMesh.bounds.size.x > 0f
                    && lOShapeMesh.bounds.size.y > 0f
                    && lOShapeMesh.bounds.size.z > 0f,
                    "3D inventory item form has an empty mesh: " + leForm);
            }
            finally
            {
                if (lOShapeMesh != null)
                    UnityEngine.Object.DestroyImmediate(lOShapeMesh);
            }
        }

        fRequire(UWVRItemGeometry.Classify(new UWObject(0)) == UWVRItemGeometry.Form.Blade
            && UWVRItemGeometry.Classify(new UWObject(18)) == UWVRItemGeometry.Form.Missile
            && UWVRItemGeometry.Classify(new UWObject(24)) == UWVRItemGeometry.Form.Bow
            && UWVRItemGeometry.Classify(new UWObject(128)) == UWVRItemGeometry.Form.Chest
            && UWVRItemGeometry.Classify(new UWObject(257)) == UWVRItemGeometry.Form.Key
            && UWVRItemGeometry.Classify(new UWObject(315)) == UWVRItemGeometry.Form.Scroll,
            "Object IDs were assigned to incorrect inventory mesh families");

        // Hand proximity must use physical depth (after XR scale conversion)
        // and ignore positions outside the panel or behind its plane.
        Matrix4x4 lONearPanel = Matrix4x4.TRS(new Vector3(2f, 3f, 4f),
            Quaternion.Euler(0f, 25f, 0f), new Vector3(0.85f * 16f, 0.48f * 16f, 1f));
        fRequire(UWVRNearGrab.TryProjectHand(
                lONearPanel.MultiplyPoint3x4(new Vector3(0.2f, -0.1f, -0.08f * 16f)),
                lONearPanel.inverse, 16f, out Vector2 lONearUV)
            && (lONearUV - new Vector2(0.7f, 0.4f)).sqrMagnitude < 0.00001f,
            "Near-hand contact does not compensate for rotated/scaled XR panel");
        fRequire(!UWVRNearGrab.TryProjectHand(
                lONearPanel.MultiplyPoint3x4(new Vector3(0.2f, -0.1f, -0.22f * 16f)),
                lONearPanel.inverse, 16f, out _)
            && !UWVRNearGrab.TryProjectHand(
                lONearPanel.MultiplyPoint3x4(new Vector3(0.2f, -0.1f, 0.08f * 16f)),
                lONearPanel.inverse, 16f, out _)
            && !UWVRNearGrab.TryProjectHand(
                lONearPanel.MultiplyPoint3x4(new Vector3(0.7f, -0.1f, -0.08f * 16f)),
                lONearPanel.inverse, 16f, out _),
            "Near-hand interaction must ignore distant, rear and off-panel positions");

        fRequire(UWVRNearGrab.CanBeginGrip(true, true, true, true)
            && !UWVRNearGrab.CanBeginGrip(false, true, true, true)
            && !UWVRNearGrab.CanBeginGrip(true, false, true, true)
            && !UWVRNearGrab.CanBeginGrip(true, true, false, true)
            && !UWVRNearGrab.CanBeginGrip(true, true, true, false),
            "Hand grab must require a new grip press, reach, occupied slot, and free cursor");

        // The hardware grip must be monitored even with the interface
        // closed; otherwise opening a bag while already gripping produces
        // an unintended phantom grab on its first active frame.
        fRequire(UWVRNearGrab.IsNewPhysicalGrip(true, false, true)
            && !UWVRNearGrab.IsNewPhysicalGrip(true, true, true)
            && !UWVRNearGrab.IsNewPhysicalGrip(true, false, false)
            && !UWVRNearGrab.IsNewPhysicalGrip(false, false, true),
            "VR grab must only begin on a real Touch grip rising edge");
        fRequire(UWVRNearGrab.CanBeginDirectGrip(true, true, true, true, false)
            && UWVRNearGrab.CanBeginDirectGrip(true, true, true, false, true)
            && !UWVRNearGrab.CanBeginDirectGrip(true, true, true, false, false)
            && !UWVRNearGrab.CanBeginDirectGrip(true, true, false, true, true)
            && !UWVRNearGrab.CanBeginDirectGrip(false, true, true, true, true),
            "Direct grip must accept an already-held swapped item on an empty slot, but no blank grip");

        Vector2 lOOrigin = new Vector2(25f, 30f);
        Vector2 lOTarget = new Vector2(100f, 90f);
        fRequire((UWVRNearGrab.ReleasePoint(lOOrigin, lOTarget, false) - lOOrigin).sqrMagnitude < 0.00001f
            && (UWVRNearGrab.ReleasePoint(lOOrigin, lOTarget, true) - lOTarget).sqrMagnitude < 0.00001f,
            "Invalid off-panel grip release must return to its original slot");

        Vector2 lOHandMoved = new Vector2(64f, 50f);
        fRequire(UWVRNearGrab.TryChoosePointer(true, true, true, true,
                lOTarget, lOHandMoved, lOOrigin, out Vector2 lOPointer)
            && lOPointer == lOTarget,
            "Mouse-up coordinate must take priority over hand movement");
        fRequire(UWVRNearGrab.TryChoosePointer(false, true, false, true,
                lOTarget, lOHandMoved, lOOrigin, out lOPointer)
            && lOPointer == lOOrigin,
            "Loss of hand tracking during drag must keep the original source slot");
        fRequire(UWVRNearGrab.TryChoosePointer(false, true, true, true,
                lOTarget, lOHandMoved, lOOrigin, out lOPointer)
            && lOPointer == lOHandMoved,
            "Near-hand drag must follow the tracked hand while in reach");
        fRequire(!UWVRNearGrab.TryChoosePointer(false, false, false, true,
                lOTarget, lOHandMoved, lOOrigin, out _)
            && !UWVRNearGrab.TryChoosePointer(false, false, true, false,
                lOTarget, lOHandMoved, lOOrigin, out _),
            "Near-hand control may not steal the pointer when hidden or untracked");

        // Exercise the actual inventory model behind both input routes. A
        // direct grip must use the original take/drop/swap implementation,
        // not copy, remove or fabricate item objects in a VR-only container.
        UWInventoryModel lODirectInventory = new UWInventoryModel(() => null, () => null);
        UWObject lOFirstItem = new UWObject(0);
        UWObject lOSecondItem = new UWObject(315);
        lODirectInventory.Backpack[0] = lOFirstItem;
        lODirectInventory.Backpack[1] = lOSecondItem;
        lODirectInventory.BeginDragFromBackpack(0);
        fRequire(lODirectInventory.Backpack[0] == null
            && lODirectInventory.CursorItem == lOFirstItem,
            "Actual inventory model failed to take item into the authoritative cursor");
        fRequire(lODirectInventory.DropCursorItemInBackpack(1)
            && lODirectInventory.Backpack[1] == lOFirstItem
            && lODirectInventory.CursorItem == lOSecondItem
            && UWModernBags.ClassifyVRDirectRelease(lOFirstItem, lODirectInventory.CursorItem)
                == UWModernBags.VRDirectGrabResult.Swapped,
            "Real inventory swap must keep displaced object on cursor and report Swapped");
        fRequire(lODirectInventory.DropCursorItemInBackpack(0)
            && lODirectInventory.Backpack[0] == lOSecondItem
            && lODirectInventory.CursorItem == null
            && UWModernBags.ClassifyVRDirectRelease(lOSecondItem, lODirectInventory.CursorItem)
                == UWModernBags.VRDirectGrabResult.Placed,
            "Real inventory model failed to put swapped item into vacated slot");
        fRequire(UWModernBags.ClassifyVRDirectRelease(lOFirstItem, lOFirstItem)
                == UWModernBags.VRDirectGrabResult.StillHeld
            && UWModernBags.ClassifyVRDirectRelease(lOFirstItem, new UWObject(0))
                == UWModernBags.VRDirectGrabResult.Swapped
            && UWModernBags.ClassifyVRDirectRelease(null, null)
                == UWModernBags.VRDirectGrabResult.NoDrag,
            "A rejected drop, empty grip, or new item with a repeated ID must be classified correctly");

        // A physical inventory quad is anchored in world space. Test the
        // controller-ray to source-screen mapping without any XR runtime or
        // renderer. Rotated/nonuniformly scaled panels must map precisely.
        Matrix4x4 lOPanelMatrix = Matrix4x4.TRS(new Vector3(2f, 1.5f, 3f),
            Quaternion.Euler(0f, 30f, 0f), new Vector3(1.65f, 0.93f, 1f));
        Ray lOCentreRay = new Ray(lOPanelMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, -1f)),
            lOPanelMatrix.MultiplyVector(Vector3.forward).normalized);
        fRequire(UWVRSpatialInventory.TryIntersectPanel(lOCentreRay,
                lOPanelMatrix.inverse, out Vector2 lOUV)
            && (lOUV - new Vector2(0.5f, 0.5f)).sqrMagnitude < 0.00001f,
            "Spatial inventory ray does not hit the physical panel centre");

        Ray lOOffsetRay = new Ray(lOPanelMatrix.MultiplyPoint3x4(new Vector3(0.25f, -0.3f, -1f)),
            lOPanelMatrix.MultiplyVector(Vector3.forward).normalized);
        fRequire(UWVRSpatialInventory.TryIntersectPanel(lOOffsetRay,
                lOPanelMatrix.inverse, out lOUV)
            && (lOUV - new Vector2(0.75f, 0.2f)).sqrMagnitude < 0.00001f,
            "Spatial inventory controller ray does not map to the source screen UV");

        Ray lOMissRay = new Ray(lOPanelMatrix.MultiplyPoint3x4(new Vector3(0.75f, 0f, -1f)),
            lOPanelMatrix.MultiplyVector(Vector3.forward).normalized);
        fRequire(!UWVRSpatialInventory.TryIntersectPanel(lOMissRay,
                lOPanelMatrix.inverse, out _),
            "A controller pointing outside the physical panel must not click the UI");

        Ray lOBackRay = new Ray(lOPanelMatrix.MultiplyPoint3x4(new Vector3(0f, 0f, 1f)),
            lOPanelMatrix.MultiplyVector(Vector3.back).normalized);
        fRequire(!UWVRSpatialInventory.TryIntersectPanel(lOBackRay,
                lOPanelMatrix.inverse, out _),
            "Touch ray must not click the physical panel from its back face");

        GameObject lOTestTracking = new GameObject("VR offline Touch ray tracking space");
        try
        {
            lOTestTracking.transform.position = new Vector3(3f, 4f, 5f);
            lOTestTracking.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            lOTestTracking.transform.localScale = Vector3.one * 16f;
            Ray lOTrackedRay = UWVRHeadTracking.RayFromTrackedPose(lOTestTracking.transform,
                new Vector3(0.25f, 0.5f, 1f), Quaternion.identity,
                new Vector3(0.25f, 0.25f, 0.5f));
            Vector3 lOExpected = lOTestTracking.transform.TransformPoint(new Vector3(0f, 0.25f, 0.5f));
            fRequire((lOTrackedRay.origin - lOExpected).sqrMagnitude < 0.00001f
                && (lOTrackedRay.direction - Vector3.right).sqrMagnitude < 0.00001f,
                "XR Touch controller ray ignores tracking origin, yaw or world scale");
            Quaternion lOWristTwist = Quaternion.AngleAxis(90f, Vector3.forward);
            Quaternion lOExpectedTwist = lOTestTracking.transform.rotation * lOWristTwist;
            Quaternion lOActualTwist = UWVRHeadTracking.WorldControllerRotation(
                lOTestTracking.transform, lOWristTwist);
            fRequire(Quaternion.Angle(lOExpectedTwist, lOActualTwist) < 0.001f
                && (lOActualTwist * Vector3.up - lOExpectedTwist * Vector3.up).sqrMagnitude < 0.00001f,
                "Full right-hand 6DoF pose must retain wrist roll after world rotation and scale");
            fRequire((UWVRInventoryTokens.HeldPosition(lOTrackedRay, 16f)
                - (lOTrackedRay.origin + Vector3.right * 2.4f)).sqrMagnitude < 0.00001f,
                "VR held item preview does not preserve a physical hand offset with XR scale");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOTestTracking);
        }

        // Confirm all eleven equipment points land in exactly the same
        // first-hit inventory zone as an actual click, including overlapped
        // hand/ring and leg/glove areas on the widened paperdoll page.
        RectInt lOPaperdoll = UWModernHudArt.PaperdollPage;
        float lfEquipScale = 3f;
        Rect lOEquipmentPage = new Rect(90f, 110f,
            lOPaperdoll.width * lfEquipScale, lOPaperdoll.height * lfEquipScale);
        UWClickRules.InventorySpot[] lOEquipSpots =
        {
            UWClickRules.InventorySpot.Helmet,
            UWClickRules.InventorySpot.Chest,
            UWClickRules.InventorySpot.Gloves,
            UWClickRules.InventorySpot.Legs,
            UWClickRules.InventorySpot.Boots,
            UWClickRules.InventorySpot.ShoulderLeft,
            UWClickRules.InventorySpot.ShoulderRight,
            UWClickRules.InventorySpot.HandLeft,
            UWClickRules.InventorySpot.HandRight,
            UWClickRules.InventorySpot.RingLeft,
            UWClickRules.InventorySpot.RingRight
        };

        foreach (UWClickRules.InventorySpot leSpot in lOEquipSpots)
        {
            fRequire(UWVRInventoryNavigation.TryGetEquipmentCentre(leSpot,
                    lOEquipmentPage, lOEquipmentPage, lfEquipScale, out Vector2 lOEquipPoint),
                "VR equipment navigation has no selectable location for " + leSpot);

            int liColumn = Mathf.FloorToInt((lOEquipPoint.x - lOEquipmentPage.xMin) / lfEquipScale);
            int liRow = Mathf.FloorToInt((lOEquipmentPage.yMax - lOEquipPoint.y) / lfEquipScale);
            int liX = UWModernHudArt.PageColumnToMain(liColumn);
            int liY = 199 - lOPaperdoll.y - liRow;
            fRequire(UWClickRules.InventorySpotAt(liX, liY) == leSpot,
                "VR equipment point would click a different slot: " + leSpot);
        }

        Rect lOClippedView = new Rect(lOEquipmentPage.xMin, lOEquipmentPage.yMin,
            lOEquipmentPage.width, lOEquipmentPage.height * 0.25f);
        fRequire(!UWVRInventoryNavigation.TryGetEquipmentCentre(
                UWClickRules.InventorySpot.Helmet, lOEquipmentPage,
                lOClippedView, lfEquipScale, out Vector2 _),
            "VR navigation must not select equipment clipped by the panel scroll area");

        // Looking 90 degrees right: forward on the stick must become right
        // relative to the unturned body. Head pitch is explicitly ignored.
        Vector2 lORelative = UWVRWorldSpace.HeadRelativeStick(Vector2.up,
            Quaternion.identity, Quaternion.Euler(0f, 90f, 0f) * Vector3.forward);
        fRequire((lORelative - Vector2.right).sqrMagnitude < 0.0001f,
            "Head-relative locomotion maps yaw incorrectly");

        lORelative = UWVRWorldSpace.HeadRelativeStick(Vector2.up,
            Quaternion.Euler(0f, 90f, 0f), Quaternion.Euler(25f, 90f, 0f) * Vector3.forward);
        fRequire((lORelative - Vector2.up).sqrMagnitude < 0.0001f,
            "Head-relative locomotion does not ignore head pitch or body yaw");

        // XR inventory navigation uses the *actual 2D slot centres*, without
        // changing the game's click, drag and equipment hit-test coordinates.
        List<Vector2> lOVRSlots = new List<Vector2>
        {
            new Vector2(20f, 20f), new Vector2(60f, 20f),
            new Vector2(20f, 60f), new Vector2(60f, 60f)
        };

        fRequire(UWVRInventoryNavigation.TryChooseNext(lOVRSlots,
                new Vector2(20f, 20f), Vector2.right, true, out Vector2 lOChosen)
            && (lOChosen - new Vector2(60f, 20f)).sqrMagnitude < 0.0001f,
            "VR inventory right-stick horizontal step is incorrect");

        fRequire(UWVRInventoryNavigation.TryChooseNext(lOVRSlots,
                new Vector2(20f, 20f), Vector2.up, true, out lOChosen)
            && (lOChosen - new Vector2(20f, 60f)).sqrMagnitude < 0.0001f,
            "VR inventory right-stick vertical step is incorrect");

        fRequire(UWVRInventoryNavigation.TryChooseNext(lOVRSlots,
                new Vector2(5f, 5f), Vector2.right, false, out lOChosen)
            && (lOChosen - new Vector2(20f, 20f)).sqrMagnitude < 0.0001f,
            "VR inventory initial focus must select the nearest slot");

        fRequire(!UWVRInventoryNavigation.TryChooseNext(lOVRSlots,
                new Vector2(60f, 20f), Vector2.right, true, out lOChosen),
            "VR inventory navigation should stop at the last column");

        fRequire(!UWVRInventoryNavigation.TryChooseNext(lOVRSlots,
                new Vector2(20f, 20f), Vector2.zero, true, out lOChosen),
            "VR inventory navigation must ignore neutral stick input");

        GameObject lOCameraObject = new GameObject("VR offline ray check", typeof(Camera));
        GameObject lOCanvasObject = new GameObject("VR offline canvas check", typeof(RectTransform), typeof(Canvas));
        GameObject lOReticleHost = new GameObject("VR offline reticle check");
        GameObject lOBody = new GameObject("VR offline avatar body");
        GameObject lOEye = new GameObject("VR offline avatar eye", typeof(Camera));
        try
        {
            lOBody.transform.position = new Vector3(16f, 3f, 8f);
            lOEye.transform.SetParent(lOBody.transform, false);
            lOEye.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Vector3 lOOriginalEyePosition = lOEye.transform.position;

            UWVRHeadTracking lOTrackedEye = lOEye.AddComponent<UWVRHeadTracking>();
            MethodInfo lOCreateTrackingSpace = typeof(UWVRHeadTracking).GetMethod(
                "EnsureTrackingSpace", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo lORestoreTrackingSpace = typeof(UWVRHeadTracking).GetMethod(
                "RestoreTrackingSpace", BindingFlags.NonPublic | BindingFlags.Instance);
            fRequire(lOCreateTrackingSpace != null && lORestoreTrackingSpace != null,
                "VR tracking-space lifecycle methods unavailable");

            lOCreateTrackingSpace.Invoke(lOTrackedEye, new object[] { 16f });
            fRequire((lOEye.transform.position - lOOriginalEyePosition).sqrMagnitude < 0.0001f,
                "VR rig attachment moved the original eye anchor");
            fRequire(Mathf.Abs(lOEye.transform.lossyScale.x - 16f) < 0.0001f,
                "VR rig did not scale stereo camera transform");
            float lfScaledUi = UWVRCanvasBridge.CalculatePlaneDistance(
                lOEye.GetComponent<Camera>(), 2.25f);
            fRequire(Mathf.Abs(lfScaledUi - 36f) < 0.01f,
                "VR panels do not preserve a physical distance across tracking scale changes");
            fRequire(Mathf.Abs(UWVRCanvasBridge.CalculatePlaneDistance(
                lOEye.GetComponent<Camera>(), 1.8f) - 28.8f) < 0.01f,
                "VR equipment/bag plane distance is inconsistent with the XR tracking scale");

            lOEye.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            fRequire(Mathf.Abs(lOEye.transform.position.x - lOOriginalEyePosition.x - 4f) < 0.0001f,
                "VR rig does not scale the physical HMD movement consistently");

            lORestoreTrackingSpace.Invoke(lOTrackedEye, null);
            fRequire(lOEye.transform.parent == lOBody.transform
                && (lOEye.transform.position - lOOriginalEyePosition).sqrMagnitude < 0.0001f,
                "VR rig does not restore desktop camera hierarchy and position");
            fRequire(Mathf.Abs(UWVRCanvasBridge.CalculatePlaneDistance(
                lOEye.GetComponent<Camera>(), 2.25f) - 2.25f) < 0.01f,
                "VR panel distance does not return to original units with the rig");

            Camera lOCamera = lOCameraObject.GetComponent<Camera>();
            lOCamera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            lOCamera.transform.position = new Vector3(1f, 2f, 3f);
            lOCamera.transform.rotation = Quaternion.Euler(-20f, 35f, 0f);

            Vector2 lOPoint = new Vector2(173f, 449f);
            Ray lOOriginal = lOCamera.ScreenPointToRay(lOPoint);
            Ray lOVRCompatible = UWVRAim.ScreenPoint(lOCamera, lOPoint);
            fRequire(fRayMatches(lOOriginal, lOVRCompatible),
                "Desktop ScreenPointToRay compatibility has changed");

            lOOriginal = lOCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            lOVRCompatible = UWVRAim.Forward(lOCamera);
            fRequire(fRayMatches(lOOriginal, lOVRCompatible),
                "Desktop viewport center ray compatibility has changed");

            Canvas lOCanvas = lOCanvasObject.GetComponent<Canvas>();
            lOCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lOCanvas.sortingOrder = 42;
            UWVRCanvasBridge.ConfigureCanvas(lOCanvas, lOCamera);
            fRequire(lOCanvas.renderMode == RenderMode.ScreenSpaceCamera,
                "VR canvas must use a stereo-compatible camera rendering mode");
            fRequire(lOCanvas.worldCamera == lOCamera && lOCanvas.sortingOrder == 42,
                "VR canvas must retain its original sorting and camera link");
            fRequire(lOCanvas.planeDistance > lOCamera.nearClipPlane
                && lOCanvas.planeDistance < lOCamera.farClipPlane,
                "VR canvas distance outside the active camera clipping range");

            UWVRPointerReticle lOReticle = lOReticleHost.AddComponent<UWVRPointerReticle>();
            MethodInfo lOCreate = typeof(UWVRPointerReticle).GetMethod("CreateCanvas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            fRequire(lOCreate != null, "VR cursor creator missing");
            lOCreate.Invoke(lOReticle, null);

            Canvas lOReticleCanvas = lOReticleHost.GetComponentInChildren<Canvas>(true);
            fRequire(lOReticleCanvas != null && lOReticleCanvas.sortingOrder == 32000,
                "VR cursor canvas not created at the UI foreground");
            Image[] lOMarks = lOReticleHost.GetComponentsInChildren<Image>(true);
            fRequire(lOMarks.Length == 3, "VR cursor geometry missing");
            foreach (Image lOMark in lOMarks)
                fRequire(!lOMark.raycastTarget, "VR cursor must not block input to menus");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOBody);
            UnityEngine.Object.DestroyImmediate(lOReticleHost);
            UnityEngine.Object.DestroyImmediate(lOCanvasObject);
            UnityEngine.Object.DestroyImmediate(lOCameraObject);
        }

        Debug.Log("[UWVR-OFFLINE] PASS: OpenXR, opt-in left-eye-only XR shader + horizontal UV offset and reset bounds, stereo-safe menu sheet, direct XR/keyboard/pad menu, bow and haptics, original projectile physics, sword sweep and inventory.");
        Debug.Log("[UWVR-OFFLINE] HMD stereo output, head tracking and UI visibility still require an actual headset.");
    }

    private static void fTestVRLeftEyeShift()
    {
        fRequire(!UWVRLeftEyeShift.IsEnabled,
            "Left-eye shift must be OFF during desktop and unattended Unity tests");
        fRequire(UWVRLeftEyeShift.ClampPixels(-1000) == -768
            && UWVRLeftEyeShift.ClampPixels(1000) == 768
            && UWVRLeftEyeShift.ClampPixels(576) == 576
            && UWVRLeftEyeShift.ClampPixels(768) == 768
            && UWVRLeftEyeShift.ClampPixels(0) == 0
            && UWVRLeftEyeShift.StepPixels == 4
            && UWVRLeftEyeShift.FastStepPixels == 32,
            "Left-eye shift must allow at least three times the former 192px limit, retain 4px precision and 32px fast steps");
        fRequire(Mathf.Abs(UWVRLeftEyeShift.NormalizedOffset(4, 2688)
                - 4f / 2688f) < 0.000001f
            && Mathf.Abs(UWVRLeftEyeShift.NormalizedOffset(-8, 2688)
                + 8f / 2688f) < 0.000001f
            && UWVRLeftEyeShift.NormalizedOffset(4, 0) == 0f
            && UWVRLeftEyeShift.NormalizedOffset(-8, -1) == 0f
            && UWVRLeftEyeShift.NormalizedOffset(0, 2688) == 0f,
            "A left-image pixel shift must convert to valid eye-render-target UV, never divide by zero or change the right eye");
        fRequire(Mathf.Abs(UWVRLeftEyeShift.NormalizedOffset(576, 2688)
                - 576f / 2688f) < 0.000001f
            && Mathf.Abs(UWVRLeftEyeShift.NormalizedOffset(768, 2688)
                - 768f / 2688f) < 0.000001f
            && Mathf.Abs(UWVRLeftEyeShift.NormalizedOffset(900, 2688)
                - 768f / 2688f) < 0.000001f,
            "New extended left-eye range must cover >=576 pixels and clamp after 768 pixels");
        Matrix4x4 lOLeftEyeView = Matrix4x4.TRS(
            Vector3.left * 0.032f, Quaternion.identity, Vector3.one).inverse;
        Matrix4x4 lORightEyeView = Matrix4x4.TRS(
            Vector3.right * 0.032f, Quaternion.identity, Vector3.one).inverse;
        fRequire(Mathf.Abs(UWVRLeftEyeShift.SignedEyeBaseline(
                lOLeftEyeView, lORightEyeView, Vector3.right) - 0.064f) < 0.000001f
            && Mathf.Abs(UWVRLeftEyeShift.SignedEyeBaseline(
                lORightEyeView, lOLeftEyeView, Vector3.right) + 0.064f) < 0.000001f,
            "XR stereo view-matrix audit must flag a deliberately reversed left/right eye baseline");

        UnityEngine.Rendering.Universal.UniversalRendererData lORenderer =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(
                UWVRLeftEyeShiftSetup.RendererAssetPath);
        fRequire(lORenderer != null
            && lORenderer.rendererFeatures.Exists(pOFeature =>
                pOFeature is UWVRLeftEyeShiftRendererFeature),
            "URP renderer must contain the opt-in XR eye-shift feature");
        Material lOMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            UWVRLeftEyeShiftSetup.MaterialAssetPath);
        fRequire(lOMaterial != null && lOMaterial.shader != null
            && lOMaterial.shader.name == UWVRLeftEyeShiftSetup.ShaderName
            && lOMaterial.shader.isSupported,
            "URP eye-shift blit shader and persistent material must be build-ready");
    }

    private static void fTestVRMenuStereoAndNavigation()
    {
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseMenuDistanceMeters(null) - 4f) < 0.00001f
            && Mathf.Abs(UWVRWorldSpace.ParseMenuDistanceMeters(
                new[] { "-VR-MENU-DISTANCE=6.0" }) - 6f) < 0.00001f
            && Mathf.Abs(UWVRWorldSpace.ParseMenuDistanceMeters(
                new[] { "-vr-menu-distance=NaN" }) - 4f) < 0.00001f
            && Mathf.Abs(UWVRWorldSpace.ParseMenuDistanceMeters(
                new[] { "-vr-menu-distance=9" }) - 4f) < 0.00001f
            && Mathf.Abs(UWVRWorldSpace.ParseUiDistanceMeters(null) - 2.25f) < 0.00001f
            && Mathf.Abs(UWVRWorldSpace.ParseInventoryDistanceMeters(null) - 1.8f) < 0.00001f,
            "Full-screen 2D menu distance must default to 4m and remain independent of HUD/bag distance");
        fRequire(Mathf.Abs(UWVRCanvasBridge.PhysicalMenuWidthAtDistance(2.25f) - 2.4f) < 0.00001f
            && Mathf.Abs(UWVRCanvasBridge.PhysicalMenuWidthAtDistance(4f) - 4.2666667f) < 0.0001f
            && Mathf.Abs(UWVRCanvasBridge.PhysicalMenuWidthAtDistance(6f) - 6.4f) < 0.0001f,
            "More distant menus must increase plane width proportionally to preserve apparent size");
        fRequire(UWVRCanvasBridge.IsStereoMenuCanvas("UW Main Menu")
            && UWVRCanvasBridge.IsStereoMenuCanvas("UW Intro Canvas")
            && UWVRCanvasBridge.IsStereoMenuCanvas("UW Character Creation")
            && UWVRCanvasBridge.IsStereoMenuCanvas("UW Loading Cover")
            && !UWVRCanvasBridge.IsStereoMenuCanvas("Modern HUD")
            && !UWVRCanvasBridge.IsStereoMenuCanvas("Modern bags"),
            "VR stereo-safe full-screen conversion must exclude gameplay and inventory canvases");

        fRequire(UWVRMenuNavigation.WrapSelection(0, -1, 4) == 3
            && UWVRMenuNavigation.WrapSelection(3, 1, 4) == 0
            && UWVRMenuNavigation.WrapSelection(1, 1, 4) == 2
            && UWVRMenuNavigation.WrapSelection(2, 2, 4) == 0
            && UWVRMenuNavigation.WrapSelection(0, 1, 0) == -1,
            "Main menu and savegame selection must wrap correctly with no mouse pointer");
        fRequire(UWVRMenuNavigation.StickDirection(0.85f) == -1
            && UWVRMenuNavigation.StickDirection(-0.85f) == 1
            && UWVRMenuNavigation.StickDirection(0.3f) == 0,
            "Quest thumbsticks must map up/down with a noise-rejecting dead zone");

        GameObject lORoot = new GameObject("VR menu test desktop parent");
        GameObject lOCameraObject = new GameObject("VR menu test eye", typeof(Camera));
        GameObject lOCanvasObject = new GameObject("UW Main Menu",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        GameObject lOButtonObject = new GameObject("Menu test button",
            typeof(RectTransform), typeof(RawImage));
        try
        {
            Camera lOCamera = lOCameraObject.GetComponent<Camera>();
            lOCamera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            lOCamera.transform.position = new Vector3(3f, 2f, 5f);
            lOCamera.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            lOCamera.nearClipPlane = 0.05f;
            lOCamera.farClipPlane = 30f;
            lOCamera.fieldOfView = 90f;

            RectTransform lORect = lOCanvasObject.GetComponent<RectTransform>();
            lORect.SetParent(lORoot.transform, false);
            lORect.localPosition = new Vector3(1f, 3f, 2f);
            Canvas lOCanvas = lOCanvasObject.GetComponent<Canvas>();
            lOCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lOCanvas.sortingOrder = 95;

            RectTransform lOButtonRect = lOButtonObject.GetComponent<RectTransform>();
            lOButtonRect.SetParent(lORect, false);
            lOButtonRect.anchorMin = lOButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
            lOButtonRect.sizeDelta = new Vector2(80f, 30f);
            lOButtonRect.anchoredPosition = Vector2.zero;

            UWVRCanvasBridge.ConfigureStereoMenuCanvas(lOCanvas, lOCamera, 2.25f);
            fRequire(lOCanvas.renderMode == RenderMode.WorldSpace
                && lOCanvas.worldCamera == lOCamera
                && lORect.parent == lOCamera.transform
                && (lORect.localPosition - Vector3.forward * 2.25f).sqrMagnitude < 0.00001f,
                "Full-screen VR menu must become exactly one physical sheet attached to head center");
            fRequire(lORect.sizeDelta == new Vector2(320f, 200f)
                && Mathf.Abs(lORect.rect.width * lORect.lossyScale.x
                    - UWVRCanvasBridge.MenuPhysicalWidthMetres) < 0.00001f,
                "Stereo menu must have a fixed comfortable physical width and logical screen size");

            // Both physical eyes are 32mm left/right of the center camera,
            // viewing ONE unchanged world-space point. Never create per-eye
            // UI objects or add another artificial IPD shift in our scripts.
            Vector3 lOPanelPoint = lORect.TransformPoint(Vector3.zero);
            Vector3 lOLeftEye = lOCamera.transform.TransformPoint(Vector3.left * 0.032f);
            Vector3 lORightEye = lOCamera.transform.TransformPoint(Vector3.right * 0.032f);
            Vector3 lOPanelNormal = lORect.forward;
            fRequire(Mathf.Abs(Vector3.Dot(lOPanelPoint - lOLeftEye, lOPanelNormal)
                    - Vector3.Dot(lOPanelPoint - lORightEye, lOPanelNormal)) < 0.00001f,
                "Menu plane depth must match both XR eyes, not be independently centered per-eye");

            Canvas.ForceUpdateCanvases();
            Rect lOScreenRect = UWScreenUi.ScreenRectOf(lOButtonObject.GetComponent<RawImage>());
            fRequire(lOScreenRect.width > 4f && lOScreenRect.height > 2f,
                "Mouse/gamepad menu hit rectangles must be projected to screen pixels from VR world space");

            // Simulate transition from the title camera to the world camera.
            UWVRCanvasBridge.ConfigureStereoMenuCanvas(lOCanvas, lOCamera, 1.8f);
            fRequire(Mathf.Abs(lORect.localPosition.z - 1.8f) < 0.00001f,
                "The same physical title sheet must be able to follow an XR camera switch");

            UWVRCanvasBridge.ConfigureStereoMenuCanvas(lOCanvas, lOCamera,
                UWVRWorldSpace.DefaultMenuDistanceMeters);
            fRequire(Mathf.Abs(lORect.localPosition.z - 4f) < 0.00001f
                && Mathf.Abs(lORect.rect.width * lORect.lossyScale.x - 4.2666667f) < 0.0001f,
                "Quest menu must move from 2.25m to the actual 4m default with same visual angle");
            UWVRCanvasBridge.ConfigureStereoMenuCanvas(lOCanvas, lOCamera, 6f);
            fRequire(Mathf.Abs(lORect.localPosition.z - 6f) < 0.00001f
                && Mathf.Abs(lORect.rect.width * lORect.lossyScale.x - 6.4f) < 0.0001f,
                "6m comparison option must place 2D art farther away without shrinking it");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOButtonObject);
            UnityEngine.Object.DestroyImmediate(lOCanvasObject);
            UnityEngine.Object.DestroyImmediate(lOCameraObject);
            UnityEngine.Object.DestroyImmediate(lORoot);
        }
    }

    private static void fTestVRCombatHaptics()
    {
        UWVRBowHapticTimeline lOTimeline = new UWVRBowHapticTimeline();
        fRequire(lOTimeline.Sample(false, 0f) == 0
            && lOTimeline.Sample(true, 0f) == 1
            && lOTimeline.Sample(true, 0f) == 0,
            "Bow nocking must request one cue only, not an impulse on every frame");
        fRequire(lOTimeline.Sample(true, 0.249f) == 0
            && lOTimeline.Sample(true, 0.25f) == 2
            && lOTimeline.Sample(true, 0.50f) == 3
            && lOTimeline.Sample(true, 0.75f) == 4
            && lOTimeline.Sample(true, 1.0f) == 5,
            "Bowstring haptics must advance at distinct 25/50/75/100 percent stages");
        for (int li = 0; li < 24; li++)
            fRequire(lOTimeline.Sample(true, 1f) == 0,
                "Holding a drawn bow must not cause continuous vibration");

        lOTimeline.Reset();
        fRequire(lOTimeline.Sample(true, 0f) == 1
            && lOTimeline.Sample(true, 1f) == 5
            && lOTimeline.Sample(true, 1f) == 0,
            "One-frame fast drawing must not queue four overlapping impulses");
        fRequire(lOTimeline.Sample(false, 1f) == 0
            && !lOTimeline.HasNock
            && lOTimeline.Sample(true, 0f) == 1,
            "Losing either tracked hand must cancel and reset bow vibration milestones");
        fRequire(lOTimeline.Sample(true, float.NaN) == 0
            && !lOTimeline.HasNock && lOTimeline.Sample(true, 0f) == 1,
            "Invalid controller draw data must not retain an armed haptic sequence");
        fRequire(UWVRBowHapticTimeline.DrawMilestone(-1f) == 0
            && UWVRBowHapticTimeline.DrawMilestone(0.25f) == 1
            && UWVRBowHapticTimeline.DrawMilestone(0.50f) == 2
            && UWVRBowHapticTimeline.DrawMilestone(0.75f) == 3
            && UWVRBowHapticTimeline.DrawMilestone(0.99f) == 4
            && UWVRBowHapticTimeline.DrawMilestone(float.NaN) == 0,
            "Bowstring haptic milestone boundaries must be deterministic");
        for (int li = 1; li <= 5; li++)
        {
            float lfAmplitude = UWVRBowHapticTimeline.DrawAmplitude(li);
            float lfDuration = UWVRBowHapticTimeline.DrawDuration(li);
            fRequire(lfAmplitude >= 0.12f && lfAmplitude <= 0.36f
                && lfDuration >= 0.018f && lfDuration <= 0.0461f,
                "Bow impulses must stay short and below the provisional strength ceiling");
        }
        fRequire(UWVRBowHapticTimeline.CanPulse(0f, float.NegativeInfinity)
            && !UWVRBowHapticTimeline.CanPulse(0.05f, 0f)
            && UWVRBowHapticTimeline.CanPulse(0.11f, 0f)
            && !UWVRBowHapticTimeline.CanPulse(float.NaN, 0f),
            "Per-hand impulse cool-down must throttle duplicate calls without blocking first use");

        // These calls must remain inert in an offline/desktop session without
        // attempting to use or emulate real XR hardware or force game input.
        UWVRCombatHaptics.BowSample(true, 1f);
        UWVRCombatHaptics.BowProjectileLaunched();
        UWVRCombatHaptics.SpellProjectileLaunched();
        fRequire(!UWVRCombatHaptics.IsEnabled,
            "Haptic tests must never activate OpenXR or report physical vibration");
    }

    private static void fTestVRCombatCalibration()
    {
        UWVRCombatCalibration.Settings lODefault = UWVRCombatCalibration.Parse(null, false);
        fRequire(Mathf.Abs(lODefault.NockRadiusMetres - UWVRTwoHandBowDraw.NockRadiusMeters) < 0.00001f
            && Mathf.Abs(lODefault.MinPullMetres - UWVRTwoHandBowDraw.MinPullMeters) < 0.00001f
            && Mathf.Abs(lODefault.FullPullMetres - UWVRTwoHandBowDraw.FullPullMeters) < 0.00001f
            && Mathf.Abs(lODefault.HapticScale - 1f) < 0.00001f,
            "Combat calibration defaults must exactly preserve the previously shipped VR bow behavior");

        UWVRCombatCalibration.Settings lOCustom = UWVRCombatCalibration.Parse(new[]
        {
            "-VR-BOW-NOCK-RADIUS=0.66", "-vr-bow-min-pull=0.12",
            "-vr-bow-full-pull=0.38", "-vr-haptic-scale=0.25"
        }, false);
        fRequire(Mathf.Abs(lOCustom.NockRadiusMetres - 0.66f) < 0.00001f
            && Mathf.Abs(lOCustom.MinPullMetres - 0.12f) < 0.00001f
            && Mathf.Abs(lOCustom.FullPullMetres - 0.38f) < 0.00001f
            && Mathf.Abs(lOCustom.HapticScale - 0.25f) < 0.00001f,
            "Explicit calibration options must parse invariant decimals and case-insensitive option names");

        UWVRCombatCalibration.Settings lOInvalid = UWVRCombatCalibration.Parse(new[]
        {
            "-vr-bow-nock-radius=NaN", "-vr-bow-min-pull=0",
            "-vr-bow-full-pull=Infinity", "-vr-haptic-scale=100"
        }, false);
        fRequire(Mathf.Abs(lOInvalid.NockRadiusMetres - lODefault.NockRadiusMetres) < 0.00001f
            && Mathf.Abs(lOInvalid.MinPullMetres - lODefault.MinPullMetres) < 0.00001f
            && Mathf.Abs(lOInvalid.FullPullMetres - lODefault.FullPullMetres) < 0.00001f
            && Mathf.Abs(lOInvalid.HapticScale - lODefault.HapticScale) < 0.00001f,
            "Malformed, NaN, infinite and out-of-bounds options must never replace safe defaults");

        UWVRCombatCalibration.Settings lONarrow = UWVRCombatCalibration.Parse(new[]
        {
            "-vr-bow-min-pull=0.32", "-vr-bow-full-pull=0.35"
        }, false);
        fRequire(Mathf.Abs(lONarrow.MinPullMetres - 0.32f) < 0.00001f
            && Mathf.Abs(lONarrow.FullPullMetres - lODefault.FullPullMetres) < 0.00001f,
            "Full bow draw must remain at least 8cm greater than the minimum shot threshold");

        fRequire(UWVRCombatCalibration.ScaleAmplitude(0.46f, 0f) == 0f
            && Mathf.Abs(UWVRCombatCalibration.ScaleAmplitude(0.46f, 0.25f) - 0.115f) < 0.00001f
            && Mathf.Abs(UWVRCombatCalibration.ScaleAmplitude(0.46f, 1f) - 0.46f) < 0.00001f
            && UWVRCombatCalibration.ScaleAmplitude(0.46f, 1.25f) < 0.60f
            && UWVRCombatCalibration.ScaleAmplitude(0.90f, 1.25f) <= 0.60f
            && UWVRCombatCalibration.ScaleAmplitude(0.46f, float.NaN) == 0f,
            "Haptic multiplier zero/quarter/default/upper limit must remain capped even with bad data");

        Vector3 lORight = new Vector3(1f, 3f, 1f);
        Vector3 lOLeft = lORight + Vector3.right * 0.55f;
        UWVRTwoHandBowDraw lODefaultBow = new UWVRTwoHandBowDraw();
        lODefaultBow.Sample(true, lORight, lOLeft, Vector3.forward, 1f, 0.011f);
        fRequire(!lODefaultBow.IsNocked,
            "A 55cm-separated hand must not nock the original default 46cm bow");

        UWVRTwoHandBowDraw lOCustomBow = new UWVRTwoHandBowDraw(lOCustom);
        lOCustomBow.Sample(true, lORight, lOLeft, Vector3.forward, 1f, 0.011f);
        fRequire(lOCustomBow.IsNocked,
            "Configurable nock radius must allow real 55cm hand separation when set to 66cm");
        for (int li = 1; li <= 5; li++)
            lOCustomBow.Sample(true, lORight, lOLeft + Vector3.back * (0.03f * li),
                Vector3.forward, 1f, 0.011f);
        float lfDrawFraction = 0f;
        fRequire(lOCustomBow.IsNocked
            && lOCustomBow.TryRelease(true, out lfDrawFraction)
            && lfDrawFraction > 0.11f && lfDrawFraction < 0.13f,
            "A calibrated 12cm minimum pull must accept 15cm without bypassing the configured full draw");

        UWVRTwoHandBowDraw lODefaultTooShort = new UWVRTwoHandBowDraw();
        Vector3 lODefaultLeft = lORight + Vector3.right * 0.24f;
        lODefaultTooShort.Sample(true, lORight, lODefaultLeft, Vector3.forward, 1f, 0.011f);
        for (int li = 1; li <= 5; li++)
            lODefaultTooShort.Sample(true, lORight,
                lODefaultLeft + Vector3.back * (0.03f * li),
                Vector3.forward, 1f, 0.011f);
        fRequire(!lODefaultTooShort.TryRelease(true, out _),
            "The unchanged 20cm minimum pull must reject the same 15cm partial draw");

        UWVRTwoHandBowDraw lOScaledCustom = new UWVRTwoHandBowDraw(lOCustom);
        lOScaledCustom.Sample(true, lORight * 16f, lOLeft * 16f,
            Vector3.forward, 16f, 0.011f);
        for (int li = 1; li <= 5; li++)
            lOScaledCustom.Sample(true, lORight * 16f,
                (lOLeft + Vector3.back * (0.03f * li)) * 16f,
                Vector3.forward, 16f, 0.011f);
        fRequire(lOScaledCustom.TryRelease(true, out float lfScaledFraction)
            && Mathf.Abs(lfScaledFraction - lfDrawFraction) < 0.001f,
            "Calibrated bow distances must stay in physical metres under the 16x VR world scale");
    }

    private static void fTestVRTwoHandBowAndMagic()
    {
        Vector3 lORight = new Vector3(1f, 3f, 1f);
        Vector3 lOLeftNock = lORight + new Vector3(0.24f, 0f, -0.02f);
        Vector3 lOForward = Vector3.forward;

        UWVRTwoHandBowDraw lODrawn = new UWVRTwoHandBowDraw();
        lODrawn.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        fRequire(lODrawn.IsNocked && lODrawn.DrawMeters < 0.001f,
            "A left Touch near the right bow must nock without accidentally shooting");
        for (int li = 1; li <= 12; li++)
            lODrawn.Sample(true, lORight, lOLeftNock + Vector3.back * (0.03f * li),
                lOForward, 1f, 0.011f);
        fRequire(lODrawn.IsNocked && lODrawn.DrawMeters > 0.30f
                && lODrawn.DrawFraction > 0.40f,
            "A tracked left Touch moving 36cm backward must draw the bowstring");
        fRequire(lODrawn.TryRelease(true, out float lfDrawFraction)
                && lfDrawFraction > 0.40f && !lODrawn.TryRelease(true, out _),
            "One bowstring pull must fire on release at most once");

        // The same physical movement must work at a 16x dungeon scale.
        UWVRTwoHandBowDraw lOScaled = new UWVRTwoHandBowDraw();
        lOScaled.Sample(true, lORight * 16f, lOLeftNock * 16f,
            lOForward, 16f, 0.011f);
        for (int li = 1; li <= 12; li++)
            lOScaled.Sample(true, lORight * 16f,
                (lOLeftNock + Vector3.back * (0.03f * li)) * 16f,
                lOForward, 16f, 0.011f);
        fRequire(lOScaled.TryRelease(true, out float lfScaledDraw)
                && Mathf.Abs(lfScaledDraw - lfDrawFraction) < 0.001f,
            "Bow draw must measure real metres independently of VR world scale");

        UWVRTwoHandBowDraw lOTooShort = new UWVRTwoHandBowDraw();
        lOTooShort.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        lOTooShort.Sample(true, lORight, lOLeftNock + Vector3.back * 0.12f,
            lOForward, 1f, 0.03f);
        fRequire(!lOTooShort.TryRelease(true, out _),
            "A partial 12cm bow draw cannot consume an arrow");

        UWVRTwoHandBowDraw lOLateral = new UWVRTwoHandBowDraw();
        lOLateral.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        for (int li = 1; li <= 11; li++)
            lOLateral.Sample(true, lORight, lOLeftNock + Vector3.right * (0.018f * li),
                lOForward, 1f, 0.011f);
        fRequire(!lOLateral.TryRelease(true, out _),
            "Moving the bowstring sideways without drawing it back cannot fire");

        UWVRTwoHandBowDraw lOTooFar = new UWVRTwoHandBowDraw();
        lOTooFar.Sample(true, lORight, lORight + Vector3.right * 0.85f,
            lOForward, 1f, 0.011f);
        fRequire(!lOTooFar.TryRelease(true, out _),
            "Hands too far apart at the start must not be considered nocked");

        UWVRTwoHandBowDraw lODropped = new UWVRTwoHandBowDraw();
        lODropped.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        for (int li = 1; li <= 10; li++)
            lODropped.Sample(true, lORight,
                lOLeftNock + Vector3.back * (0.03f * li),
                lOForward, 1f, 0.011f);
        lODropped.Sample(false, Vector3.zero, Vector3.zero,
            Vector3.forward, 1f, 0.011f);
        lODropped.Sample(true, lORight,
            lOLeftNock + Vector3.back * 0.30f, lOForward, 1f, 0.011f);
        fRequire(!lODropped.TryRelease(true, out _),
            "Tracking loss while pulling a bow must invalidate the held trigger");

        UWVRTwoHandBowDraw lOTeleport = new UWVRTwoHandBowDraw();
        lOTeleport.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        lOTeleport.Sample(true, lORight, lOLeftNock + Vector3.back * 0.85f,
            lOForward, 1f, 0.011f);
        fRequire(!lOTeleport.TryRelease(true, out _),
            "An XR pose teleport must not grant an instant full-draw arrow");

        UWVRTwoHandBowDraw lORotated = new UWVRTwoHandBowDraw();
        lORotated.Sample(true, lORight, lOLeftNock, lOForward, 1f, 0.011f);
        lORotated.Sample(true, lORight, lOLeftNock + Vector3.back * 0.30f,
            Vector3.left, 1f, 0.011f);
        fRequire(!lORotated.TryRelease(true, out _),
            "Rotating the bow by 90 degrees must not fake a string draw");

        Ray lORay = new Ray(lORight, lOForward);
        Vector3 lOTop = UWVRBowVisual.StavePoint(lORay, Vector3.up, 1f, 16f);
        Vector3 lOBottom = UWVRBowVisual.StavePoint(lORay, Vector3.up, -1f, 16f);
        Vector3 lOMiddle = UWVRBowVisual.StavePoint(lORay, Vector3.up, 0f, 16f);
        fRequire(Mathf.Abs(Vector3.Distance(lOTop, lOBottom) - 14.08f) < 0.0001f
                && lOMiddle.z > lOTop.z
                && UWVRBowVisual.StringNock(lORay, lOLeftNock, true, 16f) == lOLeftNock,
            "3D bow stave must curve and scale with tracked metres, while string follows left hand");

        Mesh lOOrbMesh = UWVRMagicHandFx.CreateOrbMesh();
        Mesh lORingMesh = UWVRMagicHandFx.CreateRingMesh();
        try
        {
            fRequire(lOOrbMesh.vertexCount == 6 && lOOrbMesh.triangles.Length == 24
                    && lOOrbMesh.bounds.size.x > 1.9f
                    && lORingMesh.vertexCount >= 32
                    && lORingMesh.triangles.Length >= 96,
                "Magic preview must be closed 3D geometry and release ring must be triangulated");
            fRequire(UWVRMagicHandFx.FlashRadiusMetres(0f) <
                    UWVRMagicHandFx.FlashRadiusMetres(0.2f)
                && UWVRMagicHandFx.FlashRadiusMetres(0.8f) <= 0.301f
                && UWVRMagicHandFx.FlashOpacity(0f) > 0.99f
                && UWVRMagicHandFx.FlashOpacity(0.2f) < 0.5f
                && UWVRMagicHandFx.FlashOpacity(1f) == 0f,
                "Successful-cast flash must expand and disappear instead of persisting over world objects");
            fRequire(UWVRMagicHandFx.ColorForDamage(UWDamageTypes.Fire)
                    != UWVRMagicHandFx.ColorForDamage(UWDamageTypes.Cold)
                && UWVRMagicHandFx.ColorForDamage(UWDamageTypes.Cold)
                    != UWVRMagicHandFx.ColorForDamage(UWDamageTypes.Magic),
                "The emitted spell flash must distinguish fire, cold and magic projectiles");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOOrbMesh);
            UnityEngine.Object.DestroyImmediate(lORingMesh);
        }
    }

    private static void fTestVRHandMuzzle()
    {
        GameObject lORoot = new GameObject("VR hand origin offline test root");
        GameObject lOAvatar = new GameObject("VR hand origin test avatar");
        GameObject lOWall = new GameObject("VR hand origin test wall");
        try
        {
            lOAvatar.transform.SetParent(lORoot.transform, false);
            lOWall.transform.SetParent(lORoot.transform, false);
            lOAvatar.transform.position = new Vector3(0f, 4f, 1.1f);
            lOAvatar.AddComponent<BoxCollider>().size = new Vector3(0.5f, 0.5f, 0.3f);
            lOWall.transform.position = new Vector3(0f, 4f, 10f);
            BoxCollider lOWallCollider = lOWall.AddComponent<BoxCollider>();
            lOWallCollider.size = new Vector3(1f, 1f, 0.18f);
            Physics.SyncTransforms();

            Ray lOTrackedRight = new Ray(new Vector3(0.4f, 4f, 0.3f), Vector3.forward);
            Ray lOTrackedLeft = new Ray(new Vector3(-0.4f, 4f, 0.3f), Vector3.left);
            Vector3 lOEye = new Vector3(0f, 4f, 0f);
            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedRight, false,
                    16f, UWVRHandLaunch.RightMuzzleOffsetMetres,
                    lOAvatar.transform, out _) == UWVRHandLaunch.Result.Untracked,
                "No XR tracking must fall back to the existing player-origin missile");
            fRequire(UWVRHandLaunch.Validate(lOEye,
                    new Ray(new Vector3(float.NaN, 4f, 0f), Vector3.forward), true,
                    16f, 0.18f, lOAvatar.transform, out _) == UWVRHandLaunch.Result.Blocked,
                "Invalid tracked VR pose must not launch a projectile from player feet");

            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedRight, true,
                    1f, UWVRHandLaunch.RightMuzzleOffsetMetres,
                    lOAvatar.transform, out Ray lORightScale1) == UWVRHandLaunch.Result.Ready
                && (lORightScale1.origin - new Vector3(0.4f, 4f, 0.48f)).sqrMagnitude < 0.00001f,
                "Right Touch muzzle must offset only a physical 18cm at world scale 1");
            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedRight, true,
                    16f, UWVRHandLaunch.RightMuzzleOffsetMetres,
                    lOAvatar.transform, out Ray lORightScale16) == UWVRHandLaunch.Result.Ready
                && (lORightScale16.origin - new Vector3(0.4f, 4f, 3.18f)).sqrMagnitude < 0.0001f,
                "Right Touch muzzle must offset 18 physical cm, not 18 game units, at scale 16");
            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedLeft, true,
                    16f, UWVRHandLaunch.LeftMuzzleOffsetMetres,
                    lOAvatar.transform, out Ray lOLeftScale16) == UWVRHandLaunch.Result.Ready
                && (lOLeftScale16.origin - new Vector3(-2.32f, 4f, 0.3f)).sqrMagnitude < 0.0001f
                && (lOLeftScale16.direction - Vector3.left).sqrMagnitude < 0.00001f,
                "Left Touch spell must emit from its own hand and preserve its independent direction");

            UWProjectileWorld.AimFromDirection(lORightScale16.direction, 15,
                out int liRightHeading, out int liRightPitch);
            fRequire(UWVRHandLaunch.TryBuildOriginalLauncher(lORightScale16,
                    liRightHeading, out UWMobileObjectMotion.Launcher lOPlayerMuzzle)
                && lOPlayerMuzzle.IsPlayer && lOPlayerMuzzle.IsCreature
                && lOPlayerMuzzle.Index == 1 && lOPlayerMuzzle.HasExplicitStart,
                "Original player projectile launcher must retain player ownership and explicit hand start");
            fRequire(lOPlayerMuzzle.ExplicitFineX != (lOPlayerMuzzle.TileX << 8)
                    + (lOPlayerMuzzle.XPos << 5) + 0xF
                && (UWViewpoint.OriginalToWorld(
                        lOPlayerMuzzle.ExplicitFineX, lOPlayerMuzzle.ExplicitFineY,
                        lOPlayerMuzzle.ExplicitFineZ) - lORightScale16.origin).magnitude < 0.22f,
                "VR missile start must preserve the fine original-unit hand position, not snap to sub-tile centres");
            fRequire(!UWVRHandLaunch.TryBuildOriginalLauncher(
                    new Ray(new Vector3(-1000f, 4f, 0f), Vector3.forward),
                    liRightHeading, out _),
                "Controller beyond the mapped dungeon must not launch a missile into an invalid tile");

            // The real, engine-free projectile motion launcher is exercised
            // without Unity camera, player controller or HMD. Its record must
            // start at the fine hand position, retain owner=player and use
            // the original heading/vertical speed/mobile period.
            VRHandMuzzleTestWorld lOTestWorld = new VRHandMuzzleTestWorld();
            UWMobileObjectMotion lOMotion = new UWMobileObjectMotion(
                lOTestWorld, new VRHandMuzzleTestHost(), null);
            UWMobileRecord lOArrow = new UWMobileRecord
            {
                Object = new UWObject(0x11), Index = 1402
            };
            fRequire(lOMotion.TryLaunch(lOArrow, lOPlayerMuzzle, true, 0,
                    liRightPitch, 15),
                "Core rejected a hand-origin arrow in the test dungeon's empty tile");
            fRequire(lOArrow.FineX == lOPlayerMuzzle.ExplicitFineX
                && lOArrow.FineY == lOPlayerMuzzle.ExplicitFineY
                && lOArrow.FineZ == lOPlayerMuzzle.ExplicitFineZ
                && lOArrow.Owner == 1 && lOArrow.Index == 1402
                && lOArrow.HeadingByte == (liRightHeading & 0xFF)
                && lOArrow.SpeedByte == 15 && lOArrow.Period == 1,
                "Original mobile flight modified the explicit hand start, owner, heading or speed");

            UWMobileRecord lOLegacyArrow = new UWMobileRecord
            {
                Object = new UWObject(0x11), Index = 1403
            };
            UWMobileObjectMotion.Launcher lOLegacy = lOPlayerMuzzle;
            lOLegacy.HasExplicitStart = false;
            lOLegacy.Height = 0;
            fRequire(lOMotion.TryLaunch(lOLegacyArrow, lOLegacy, true, 0,
                    liRightPitch, 15)
                && lOLegacyArrow.FineX == (lOLegacyArrow.TileX << 8)
                    + (lOLegacyArrow.XPos << 5) + 0xF,
                "Legacy non-VR missile launcher must preserve its original sub-tile start");

            lOTestWorld.SolidTiles = true;
            UWMobileRecord lOBlockedArrow = new UWMobileRecord
            {
                Object = new UWObject(0x11), Index = 1404
            };
            fRequire(!lOMotion.TryLaunch(lOBlockedArrow, lOPlayerMuzzle, true, 0,
                    liRightPitch, 15),
                "Core must refuse a hand-origin projectile embedded in a solid dungeon tile");

            // A controller through a wall is still TRACKED; it must not cause
            // a legacy body-origin shot. Same for an invisible trigger (ignore).
            lOWall.transform.position = new Vector3(0.4f, 4f, 1.1f);
            Physics.SyncTransforms();
            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedRight, true,
                    16f, UWVRHandLaunch.RightMuzzleOffsetMetres,
                    lOAvatar.transform, out _) == UWVRHandLaunch.Result.Blocked,
                "Hand tracking through a solid wall must block the shot, not silently fall back");
            lOWallCollider.isTrigger = true;
            Physics.SyncTransforms();
            fRequire(UWVRHandLaunch.Validate(lOEye, lOTrackedRight, true,
                    16f, UWVRHandLaunch.RightMuzzleOffsetMetres,
                    lOAvatar.transform, out _) == UWVRHandLaunch.Result.Ready,
                "Trigger-only volumes must not obstruct tracked hand projectile starts");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lORoot);
        }
    }

    private sealed class VRHandMuzzleTestWorld : IUWMotionWorld
    {
        public bool SolidTiles;

        public bool TryGetTile(int piX, int piY,
            out int piType, out int piFloor, out int piTerrain)
        {
            piType = SolidTiles ? 0 : 1;
            piFloor = 0;
            piTerrain = 0;
            return piX >= 0 && piY >= 0 && piX < 64 && piY < 64;
        }

        public void GetBodiesInTile(int piX, int piY, List<UWMotionBody> pOInto) { }
        public int Random() => 0;
        public void UseOnMover(int piOtherIndex, int piMoverIndex, int piX, int piY) { }
        public int TriggerMove(int piMoverIndex, int piOtherIndex, int piX, int piY) => 2;
        public bool MissileHits(int piMoverIndex, int piOtherIndex, int piX, int piY) => false;
        public void PushObject(int piOtherIndex, int piHeading,
            int piSpeed, int piVz, int piX, int piY) { }
        public void PlaySoundAt(int piSound, int piX8, int piY8, int piVolume) { }
    }

    private sealed class VRHandMuzzleTestHost : IUWMobileObjectHost
    {
        public int DungeonLevel => 1;
        public int ClockPhase => 0;
        public void MoveToTile(UWMobileRecord pORecord, int piOldTileX,
            int piOldTileY, int piNewTileX, int piNewTileY) { }
        public bool DamageSelf(UWMobileRecord pORecord, int piDamage, int piType) => false;
        public void PlaySoundAtObject(int piSound, UWMobileRecord pORecord, int piVolume) { }
        public void Splash(UWMobileRecord pORecord) { }
        public bool TalismanBurns(UWMobileRecord pORecord, int piTileX, int piTileY) => false;
        public void OnDemoted(UWMobileRecord pORecord) { }
        public int Detonate(UWMobileRecord pORecord, int piTileX,
            int piTileY, int piOwner) => 0;
        public void Remove(UWMobileRecord pORecord) { }
        public void PlaceAtRest(UWMobileRecord pORecord, int piOctant, int piQuality) { }
    }

    private static void fTestVRBladeSweep()
    {
        GameObject lORoot = new GameObject("UW VR blade physics test parent");
        GameObject lOAvatar = new GameObject("UW VR test avatar");
        GameObject lOWall = new GameObject("UW VR test physical wall");
        GameObject lOTarget = new GameObject("UW VR test damageable enemy");
        try
        {
            lOAvatar.transform.SetParent(lORoot.transform, false);
            lOWall.transform.SetParent(lORoot.transform, false);
            lOTarget.transform.SetParent(lORoot.transform, false);
            lOAvatar.transform.position = new Vector3(-0.32f, 0f, 0.70f);
            BoxCollider lOAvatarCollider = lOAvatar.AddComponent<BoxCollider>();
            lOAvatarCollider.size = new Vector3(0.12f, 0.20f, 0.20f);
            lOTarget.transform.position = new Vector3(0f, 0f, 0.70f);
            BoxCollider lOTargetCollider = lOTarget.AddComponent<BoxCollider>();
            lOTargetCollider.size = new Vector3(0.18f, 0.20f, 0.16f);
            lOTarget.AddComponent<UWEntityInfo>();
            lOTarget.AddComponent<UWDamageable>().Initialise(12, 0, 0);
            lOWall.transform.position = new Vector3(0f, 0f, 0.35f);
            BoxCollider lOWallCollider = lOWall.AddComponent<BoxCollider>();
            lOWallCollider.size = new Vector3(0.45f, 0.35f, 0.07f);
            Physics.SyncTransforms();

            fRequire(UWVRBladeSweep.TryFirstSurface(
                    new Vector3(-0.50f, 0f, 0.70f), new Vector3(0.40f, 0f, 0.70f),
                    0.03f, lOAvatar.transform, out RaycastHit lOFirst)
                && lOFirst.collider == lOTargetCollider,
                "Blade swept-sphere failed to select a damageable instead of its own avatar collider");
            fRequire(!UWVRBladeSweep.HasClearPath(Vector3.zero,
                    new Vector3(0f, 0f, 0.70f), lOTargetCollider, lOAvatar.transform),
                "A wall between eye and an enemy must block blade contact even under one scene parent");

            // Nearest obstacle must win, not the ordering of RaycastAll.
            lOWall.transform.position = new Vector3(-0.24f, 0f, 0.70f);
            Physics.SyncTransforms();
            fRequire(UWVRBladeSweep.TryFirstSurface(
                    new Vector3(-0.50f, 0f, 0.70f), new Vector3(0.40f, 0f, 0.70f),
                    0.03f, lOAvatar.transform, out lOFirst)
                && lOFirst.collider == lOWallCollider,
                "Sword sweep must hit the nearer wall before the enemy, not penetrate the wall");
            lOWallCollider.isTrigger = true;
            Physics.SyncTransforms();
            fRequire(UWVRBladeSweep.TryFirstSurface(
                    new Vector3(-0.50f, 0f, 0.70f), new Vector3(0.40f, 0f, 0.70f),
                    0.03f, lOAvatar.transform, out lOFirst)
                && lOFirst.collider == lOTargetCollider,
                "Invisible trigger volumes must not stop a physical sword");
            lOWallCollider.isTrigger = false;
            lOWall.transform.position = new Vector3(1.0f, 0f, 0.35f);
            Physics.SyncTransforms();
            fRequire(UWVRBladeSweep.HasClearPath(Vector3.zero,
                    new Vector3(0f, 0f, 0.70f), lOTargetCollider, lOAvatar.transform),
                "Sufficiently exposed enemy should be reachable with blade contact");

            // Actual physics against a dummy game entity, but no damage
            // applied: the authoritative combat routine owns that later.
            UWVRBladeSweep lOSweep = new UWVRBladeSweep();
            UWVRWeaponShape.Profile lOProfile = UWVRWeaponShape.Get(UWVRWeaponShape.Kind.Sword);
            for (int li = 0; li <= 12; li++)
                lOSweep.Sample(true, new Vector3(-0.50f + 0.045f * li, 0f, 0f),
                    Quaternion.identity, lOProfile, 1f, Vector3.zero, 2f,
                    lOAvatar.transform, 0.02f);
            fRequire(lOSweep.TryFinish(out UWVRBladeSweep.Contact lOContact)
                && lOContact.Collider == lOTargetCollider && lOContact.Distance < 2f,
                "Actual swept Touch blade failed to record the first contact with a damageable");
            fRequire(!lOSweep.TryFinish(out _),
                "One blade gesture must never produce two independent physical hits");

            UWVRBladeSweep lODropped = new UWVRBladeSweep();
            for (int li = 0; li <= 12; li++)
                lODropped.Sample(true, new Vector3(-0.50f + 0.045f * li, 0f, 0f),
                    Quaternion.identity, lOProfile, 1f, Vector3.zero, 2f,
                    lOAvatar.transform, 0.02f);
            lODropped.Sample(false, Vector3.zero, Quaternion.identity, lOProfile,
                1f, Vector3.zero, 2f, lOAvatar.transform, 0.02f);
            fRequire(!lODropped.TryFinish(out _),
                "Losing Touch tracking must invalidate a previously contacted enemy");

            // A legitimate contact outside the original game's melee reach
            // must not turn into a free long-range attack.
            UWVRBladeSweep lOFar = new UWVRBladeSweep();
            for (int li = 0; li <= 12; li++)
                lOFar.Sample(true, new Vector3(-0.50f + 0.045f * li, 0f, 0f),
                    Quaternion.identity, lOProfile, 1f, Vector3.zero, 0.25f,
                    lOAvatar.transform, 0.02f);
            fRequire(!lOFar.TryFinish(out _),
                "A blade touch beyond the original attack distance must be refused");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lORoot);
        }
    }

    private static void fTestVRStroke(UWVRMeleeMotion.Strike peExpected, Vector3 pOTravel)
    {
        UWVRMeleeMotion lOClassifier = new UWVRMeleeMotion();
        lOClassifier.Sample(true, Vector3.zero, Vector3.forward, 0.02f);
        for (int li = 1; li <= 10; li++)
            lOClassifier.Sample(true, pOTravel * (li / 10f), Vector3.forward, 0.02f);

        UWVRMeleeMotion lOUntrackedRelease = new UWVRMeleeMotion();
        lOUntrackedRelease.Sample(true, Vector3.zero, Vector3.forward, 0.02f);
        for (int li = 1; li <= 10; li++)
            lOUntrackedRelease.Sample(true, pOTravel * (li / 10f), Vector3.forward, 0.02f);
        fRequire(!lOUntrackedRelease.TryFinish(false, out _, out _),
            "Losing the physical Touch controller exactly on release must cancel an otherwise valid swing");

        fRequire(lOClassifier.TryFinish(true, out UWVRMeleeMotion.Strike leActual,
                out float lfChargeLimit)
            && leActual == peExpected && lfChargeLimit >= 0.4f && lfChargeLimit <= 1f,
            "Synthetic right Touch movement did not classify " + peExpected);
        fRequire(!lOClassifier.TryFinish(true, out _, out _),
            "One VR gesture must not resolve into a second attack");
    }

    private static bool fRayMatches(Ray pOA, Ray pOB)
    {
        return (pOA.origin - pOB.origin).sqrMagnitude < 0.000001f
            && (pOA.direction - pOB.direction).sqrMagnitude < 0.000001f;
    }

    private static bool fHasBinding(InputAction pOAction, string psPath)
    {
        if (pOAction == null)
            return false;

        foreach (InputBinding lOBinding in pOAction.bindings)
            if (string.Equals(lOBinding.path, psPath, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static void fRequire(bool pbCondition, string psReason)
    {
        if (!pbCondition)
            throw new InvalidOperationException("[UWVR-OFFLINE] FAIL: " + psReason);
    }
}
