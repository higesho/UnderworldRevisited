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

        Debug.Log("[UWVR-OFFLINE] PASS: OpenXR, Touch, stereo UI, 11 equipment zones, spatial ray, 14 procedural mesh families, near-hand reach, physical grip edge, mouse-up priority, lost-tracking fallback and cursor safety.");
        Debug.Log("[UWVR-OFFLINE] HMD stereo output, head tracking and UI visibility still require an actual headset.");
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
