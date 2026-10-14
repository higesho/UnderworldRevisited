using UnityEngine;
using UWDataImport.UWData;

/// <summary>
/// Physical dimensions shared by the right-hand VR mesh and its optional swept
/// collision. Distances are tracked metres, NOT arbitrary Underworld world
/// units. Models are provisional representatives of weapon FAMILIES, not
/// hand-authored geometry for every original game item ID.
/// </summary>
public static class UWVRWeaponShape
{
    public enum Kind { Sword, Axe, Mace, Fist }

    public readonly struct Profile
    {
        public readonly Kind Form;
        public readonly float ContactStartMetres;
        public readonly float ContactEndMetres;
        public readonly float RadiusMetres;

        public Profile(Kind peForm, float pfStart, float pfEnd, float pfRadius)
        {
            Form = peForm;
            ContactStartMetres = pfStart;
            ContactEndMetres = pfEnd;
            RadiusMetres = pfRadius;
        }
    }

    /// <summary>Use the original melee-table-based weapon animation family,
    /// not fragile guesses from the displayed icon or item name.</summary>
    public static Kind Classify(WeaponTypes peType)
    {
        switch (peType)
        {
            case WeaponTypes.RightHandAxe:
            case WeaponTypes.LeftHandAxe: return Kind.Axe;
            case WeaponTypes.RightHandMace:
            case WeaponTypes.LeftHandMace: return Kind.Mace;
            case WeaponTypes.RightHandFist:
            case WeaponTypes.LeftHandFist: return Kind.Fist;
            default: return Kind.Sword;
        }
    }

    public static Profile Get(Kind peForm)
    {
        switch (peForm)
        {
            case Kind.Axe: return new Profile(peForm, 0.58f, 0.78f, 0.07f);
            case Kind.Mace: return new Profile(peForm, 0.56f, 0.73f, 0.08f);
            case Kind.Fist: return new Profile(peForm, 0.05f, 0.16f, 0.075f);
            default: return new Profile(Kind.Sword, 0.20f, 0.79f, 0.033f);
        }
    }

    /// <summary>The existing model's 6cm palm offset is subtracted here too.
    /// This keeps the contact path physically aligned with the mesh on Touch.</summary>
    public static Vector3 ContactPoint(Ray pOControllerRay, Profile pOProfile,
        float pfAlongBlade, float pfUnitsPerMetre)
    {
        float lfMeters = Mathf.Lerp(pOProfile.ContactStartMetres,
            pOProfile.ContactEndMetres, Mathf.Clamp01(pfAlongBlade)) - 0.06f;
        return pOControllerRay.origin + pOControllerRay.direction.normalized
            * (lfMeters * Mathf.Max(0.001f, pfUnitsPerMetre));
    }
}
