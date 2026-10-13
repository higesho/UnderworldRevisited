using UnityEngine;

/// <summary>
/// Common ray origin for world actions. The old 2D cursor coordinates retain
/// their original behavior outside VR; in VR they are intentionally ignored
/// and all world actions start at the tracked center of the player's eyes.
/// This does not replace UI pointer navigation.
/// </summary>
public static class UWVRAim
{
    public static bool IsGazeActive => UWVRBootstrap.IsHeadsetActive;

    public static Ray ScreenPoint(Camera pOCamera, Vector2 pOPoint)
    {
        if (pOCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        return IsGazeActive ? Forward(pOCamera) : pOCamera.ScreenPointToRay(pOPoint);
    }

    public static Ray Forward(Camera pOCamera)
    {
        if (pOCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        // Forward from the tracked transform is unambiguous for both eyes,
        // avoiding dependence on a desktop viewport or on the mouse position.
        return IsGazeActive
            ? new Ray(pOCamera.transform.position, pOCamera.transform.forward)
            : pOCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
}
