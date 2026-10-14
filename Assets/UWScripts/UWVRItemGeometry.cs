using System.Collections.Generic;
using UnityEngine;
using UWDataImport.UWData;

/// <summary>
/// Small procedural meshes for the world-anchored inventory. Shape selection
/// is based on actual Ultima Underworld object-category/ID ranges (not a
/// guessed item name). Several IDs still share a generic shape; these are
/// placeholder props with a real 3D silhouette, not final authored models.
/// The source game's icon is layered over each model for recognition.
/// </summary>
public static class UWVRItemGeometry
{
    public enum Form
    {
        Plain,
        Blade,
        Missile,
        Bow,
        Armor,
        Chest,
        Torch,
        Wand,
        Gem,
        Flask,
        Rune,
        Key,
        Book,
        Scroll
    }

    public static Form Classify(UWObject pOItem)
    {
        if (pOItem == null)
            return Form.Plain;

        switch (pOItem.GetCategory())
        {
            case UWObject.ObjectCategoryEnum.WeaponsAndMissiles:
                return pOItem.ID >= 24 ? Form.Bow
                    : pOItem.ID >= 16 ? Form.Missile : Form.Blade;
            case UWObject.ObjectCategoryEnum.ArmorAndClothing:
                return Form.Armor;
            case UWObject.ObjectCategoryEnum.Containers:
                return Form.Chest;
            case UWObject.ObjectCategoryEnum.LightSources:
                return Form.Torch;
            case UWObject.ObjectCategoryEnum.Wands:
                return Form.Wand;
            case UWObject.ObjectCategoryEnum.Treasure:
                return Form.Gem;
            case UWObject.ObjectCategoryEnum.Comestibles:
                return Form.Flask;
            case UWObject.ObjectCategoryEnum.RunesAndKeyOfInfinityParts:
                return Form.Rune;
            case UWObject.ObjectCategoryEnum.KeysLockpickLock:
                return Form.Key;
            case UWObject.ObjectCategoryEnum.BooksAndScrolls:
                // The low and high halves share a broad category in the
                // original data. This distinction is deliberately only a
                // visual convention until the exact object definitions are
                // mapped; original icons identify each specific item.
                return pOItem.ID < 312 ? Form.Book : Form.Scroll;
            default:
                return Form.Plain;
        }
    }

    public static Vector2 IconFaceScale(Form peForm)
    {
        switch (peForm)
        {
            case Form.Blade:
            case Form.Missile:
            case Form.Wand:
            case Form.Torch:
            case Form.Key:
                return new Vector2(0.37f, 0.53f);
            case Form.Bow:
            case Form.Flask:
                return new Vector2(0.57f, 0.62f);
            case Form.Gem:
            case Form.Rune:
                return new Vector2(0.62f, 0.63f);
            case Form.Scroll:
            case Form.Book:
                return new Vector2(0.72f, 0.7f);
            default:
                return new Vector2(0.8f, 0.76f);
        }
    }

    public static Mesh CreateMesh(Form peForm)
    {
        List<Vector3> lOVertices = new List<Vector3>(160);
        List<Vector2> lOUVs = new List<Vector2>(160);
        List<int> lOTriangles = new List<int>(280);

        switch (peForm)
        {
            case Form.Blade:
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.38f,
                    new Vector2(-0.10f, -0.10f), new Vector2(0.10f, -0.10f),
                    new Vector2(0.10f, 0.32f), new Vector2(0f, 0.49f),
                    new Vector2(-0.10f, 0.32f));
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.15f, 0.33f, 0.48f, 0.09f, 0.46f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.33f, 0f, 0.13f, 0.28f, 0.28f);
                break;

            case Form.Missile:
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.045f, -0.48f), new Vector2(0.045f, 0.30f),
                    new Vector2(0.08f, 0.31f), new Vector2(0f, 0.49f)
                }, 8);
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.2f,
                    new Vector2(-0.19f, -0.45f), new Vector2(0.19f, -0.45f),
                    new Vector2(0.08f, -0.29f), new Vector2(-0.08f, -0.29f));
                break;

            case Form.Bow:
                AddBox(lOVertices, lOUVs, lOTriangles, -0.10f, 0.12f, 0f, 0.10f, 0.68f, 0.22f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.15f, 0f, 0.38f, 0.10f, 0.16f);
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.18f,
                    new Vector2(-0.24f, -0.49f), new Vector2(-0.1f, -0.42f),
                    new Vector2(0.02f, -0.05f), new Vector2(-0.12f, 0.48f),
                    new Vector2(-0.30f, 0.40f));
                break;

            case Form.Armor:
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.65f,
                    new Vector2(-0.33f, -0.43f), new Vector2(0.33f, -0.43f),
                    new Vector2(0.39f, 0.34f), new Vector2(0.25f, 0.49f),
                    new Vector2(-0.25f, 0.49f), new Vector2(-0.39f, 0.34f));
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.32f, -0.03f, 0.62f, 0.11f, 0.77f);
                break;

            case Form.Chest:
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.05f, 0f, 0.85f, 0.64f, 0.88f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, 0.34f, 0f, 0.94f, 0.18f, 0.94f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, 0.03f, -0.48f, 0.16f, 0.2f, 0.04f);
                break;

            case Form.Torch:
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.07f, -0.49f), new Vector2(0.08f, 0.27f),
                    new Vector2(0.18f, 0.32f), new Vector2(0.15f, 0.38f)
                }, 10);
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.25f,
                    new Vector2(-0.12f, 0.32f), new Vector2(0.12f, 0.32f),
                    new Vector2(0.22f, 0.43f), new Vector2(0f, 0.5f),
                    new Vector2(-0.22f, 0.43f));
                break;

            case Form.Wand:
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.04f, -0.48f), new Vector2(0.075f, -0.18f),
                    new Vector2(0.05f, 0.36f), new Vector2(0.03f, 0.48f)
                }, 12);
                break;

            case Form.Gem:
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.6f,
                    new Vector2(-0.24f, -0.42f), new Vector2(0.24f, -0.42f),
                    new Vector2(0.46f, 0f), new Vector2(0.18f, 0.45f),
                    new Vector2(-0.18f, 0.45f), new Vector2(-0.46f, 0f));
                break;

            case Form.Flask:
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.13f, -0.46f), new Vector2(0.29f, -0.36f),
                    new Vector2(0.34f, -0.08f), new Vector2(0.22f, 0.22f),
                    new Vector2(0.12f, 0.28f), new Vector2(0.12f, 0.40f),
                    new Vector2(0.15f, 0.42f), new Vector2(0.15f, 0.47f)
                }, 16);
                break;

            case Form.Rune:
                AddPolygon(lOVertices, lOUVs, lOTriangles, 0.45f,
                    new Vector2(-0.22f, -0.46f), new Vector2(0.22f, -0.46f),
                    new Vector2(0.41f, -0.19f), new Vector2(0.41f, 0.25f),
                    new Vector2(0.18f, 0.48f), new Vector2(-0.18f, 0.48f),
                    new Vector2(-0.41f, 0.25f), new Vector2(-0.41f, -0.19f));
                break;

            case Form.Key:
                AddTorus(lOVertices, lOUVs, lOTriangles,
                    new Vector3(0f, 0.22f, 0f), 0.19f, 0.075f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, -0.2f, 0f, 0.12f, 0.43f, 0.15f);
                AddBox(lOVertices, lOUVs, lOTriangles, 0.09f, -0.39f, 0f, 0.21f, 0.1f, 0.15f);
                break;

            case Form.Scroll:
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, 0f, 0f, 0.67f, 0.61f, 0.12f);
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.115f, -0.43f), new Vector2(0.115f, 0.43f)
                }, 12, new Vector3(0f, -0.29f, 0f), Quaternion.Euler(0f, 0f, -90f));
                AddLathe(lOVertices, lOUVs, lOTriangles, new[]
                {
                    new Vector2(0.115f, -0.43f), new Vector2(0.115f, 0.43f)
                }, 12, new Vector3(0f, 0.29f, 0f), Quaternion.Euler(0f, 0f, -90f));
                break;

            case Form.Book:
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, 0f, 0f, 0.74f, 0.90f, 0.29f);
                AddBox(lOVertices, lOUVs, lOTriangles, -0.37f, 0f, 0f, 0.09f, 0.95f, 0.32f);
                break;

            default:
                AddBox(lOVertices, lOUVs, lOTriangles, 0f, 0f, 0f, 0.8f, 0.83f, 0.7f);
                break;
        }

        Mesh lOMesh = new Mesh { name = "UW VR item shape " + peForm };
        lOMesh.SetVertices(lOVertices);
        lOMesh.SetUVs(0, lOUVs);
        lOMesh.SetTriangles(lOTriangles, 0);
        lOMesh.RecalculateNormals();
        lOMesh.RecalculateBounds();
        return lOMesh;
    }

    private static void AddBox(List<Vector3> pOVertices, List<Vector2> pOUVs,
        List<int> pOTriangles, float pfX, float pfY, float pfZ,
        float pfWidth, float pfHeight, float pfDepth)
    {
        int liStart = pOVertices.Count;
        AddPolygon(pOVertices, pOUVs, pOTriangles, pfDepth,
            new Vector2(pfX - pfWidth * 0.5f, pfY - pfHeight * 0.5f),
            new Vector2(pfX + pfWidth * 0.5f, pfY - pfHeight * 0.5f),
            new Vector2(pfX + pfWidth * 0.5f, pfY + pfHeight * 0.5f),
            new Vector2(pfX - pfWidth * 0.5f, pfY + pfHeight * 0.5f));
        for (int liAt = liStart; liAt < pOVertices.Count; liAt++)
            pOVertices[liAt] += Vector3.forward * pfZ;
    }

    private static void AddPolygon(List<Vector3> pOVertices, List<Vector2> pOUVs,
        List<int> pOTriangles, float pfDepth, params Vector2[] pOPoints)
    {
        if (pOPoints == null || pOPoints.Length < 3)
            return;

        // Both front and back polygon fans share the exact same outline.
        // Resolve winding, since source silhouettes are authored in 2D.
        float lfSignedArea = 0f;
        for (int liAt = 0; liAt < pOPoints.Length; liAt++)
        {
            Vector2 lOA = pOPoints[liAt];
            Vector2 lOB = pOPoints[(liAt + 1) % pOPoints.Length];
            lfSignedArea += lOA.x * lOB.y - lOB.x * lOA.y;
        }

        int liCount = pOPoints.Length;
        int liStart = pOVertices.Count;
        for (int liSide = 0; liSide < 2; liSide++)
        {
            float lfZ = liSide == 0 ? -pfDepth * 0.5f : pfDepth * 0.5f;
            for (int liAt = 0; liAt < liCount; liAt++)
            {
                int liIndex = lfSignedArea >= 0f ? liAt : liCount - 1 - liAt;
                Vector2 lOPoint = pOPoints[liIndex];
                pOVertices.Add(new Vector3(lOPoint.x, lOPoint.y, lfZ));
                pOUVs.Add(lOPoint + new Vector2(0.5f, 0.5f));
            }
        }

        // All supplied outlines are convex (the sword's guard is its own
        // cuboid), so triangle fans never create inverted cross-sections.
        for (int liAt = 1; liAt < liCount - 1; liAt++)
        {
            pOTriangles.Add(liStart);
            pOTriangles.Add(liStart + liAt + 1);
            pOTriangles.Add(liStart + liAt);
            pOTriangles.Add(liStart + liCount);
            pOTriangles.Add(liStart + liCount + liAt);
            pOTriangles.Add(liStart + liCount + liAt + 1);
        }

        for (int liAt = 0; liAt < liCount; liAt++)
        {
            int liNext = (liAt + 1) % liCount;
            int liFront = liStart + liAt;
            int liFrontNext = liStart + liNext;
            int liBack = liFront + liCount;
            int liBackNext = liFrontNext + liCount;
            pOTriangles.Add(liFront);
            pOTriangles.Add(liFrontNext);
            pOTriangles.Add(liBack);
            pOTriangles.Add(liFrontNext);
            pOTriangles.Add(liBackNext);
            pOTriangles.Add(liBack);
        }
    }

    private static void AddLathe(List<Vector3> pOVertices, List<Vector2> pOUVs,
        List<int> pOTriangles, Vector2[] pOProfile, int piSides,
        Vector3 pOOffset = default, Quaternion pORotation = default)
    {
        if (pOProfile == null || pOProfile.Length < 2 || piSides < 3)
            return;

        if (pORotation == default)
            pORotation = Quaternion.identity;

        int liStart = pOVertices.Count;
        for (int liRow = 0; liRow < pOProfile.Length; liRow++)
            for (int liSide = 0; liSide < piSides; liSide++)
            {
                float lfAngle = 2f * Mathf.PI * liSide / piSides;
                Vector2 lORing = pOProfile[liRow];
                Vector3 lOPoint = new Vector3(Mathf.Cos(lfAngle) * lORing.x,
                    lORing.y, Mathf.Sin(lfAngle) * lORing.x);
                pOVertices.Add(pOOffset + pORotation * lOPoint);
                pOUVs.Add(new Vector2(liSide / (float)piSides,
                    liRow / (float)(pOProfile.Length - 1)));
            }

        for (int liRow = 0; liRow < pOProfile.Length - 1; liRow++)
            for (int liSide = 0; liSide < piSides; liSide++)
            {
                int liA = liStart + liRow * piSides + liSide;
                int liB = liStart + liRow * piSides + (liSide + 1) % piSides;
                int liC = liA + piSides;
                int liD = liB + piSides;
                pOTriangles.Add(liA); pOTriangles.Add(liC); pOTriangles.Add(liB);
                pOTriangles.Add(liB); pOTriangles.Add(liC); pOTriangles.Add(liD);
            }

        // Separate face centres cap the cylinder ends.
        for (int liCap = 0; liCap < 2; liCap++)
        {
            int liRow = liCap == 0 ? 0 : pOProfile.Length - 1;
            int liCentre = pOVertices.Count;
            pOVertices.Add(pOOffset + pORotation * new Vector3(0f, pOProfile[liRow].y, 0f));
            pOUVs.Add(new Vector2(0.5f, 0.5f));
            for (int liSide = 0; liSide < piSides; liSide++)
            {
                int liA = liStart + liRow * piSides + liSide;
                int liB = liStart + liRow * piSides + (liSide + 1) % piSides;
                pOTriangles.Add(liCentre);
                pOTriangles.Add(liCap == 0 ? liA : liB);
                pOTriangles.Add(liCap == 0 ? liB : liA);
            }
        }
    }

    private static void AddTorus(List<Vector3> pOVertices, List<Vector2> pOUVs,
        List<int> pOTriangles, Vector3 pOCentre, float pfMajor, float pfMinor)
    {
        const int MajorSides = 18;
        const int MinorSides = 9;
        int liStart = pOVertices.Count;
        for (int liMajor = 0; liMajor < MajorSides; liMajor++)
            for (int liMinor = 0; liMinor < MinorSides; liMinor++)
            {
                float lfMajorAngle = 2f * Mathf.PI * liMajor / MajorSides;
                float lfMinorAngle = 2f * Mathf.PI * liMinor / MinorSides;
                float lfRadius = pfMajor + Mathf.Cos(lfMinorAngle) * pfMinor;
                pOVertices.Add(pOCentre + new Vector3(Mathf.Cos(lfMajorAngle) * lfRadius,
                    Mathf.Sin(lfMajorAngle) * lfRadius,
                    Mathf.Sin(lfMinorAngle) * pfMinor));
                pOUVs.Add(new Vector2(liMajor / (float)MajorSides, liMinor / (float)MinorSides));
            }

        for (int liMajor = 0; liMajor < MajorSides; liMajor++)
            for (int liMinor = 0; liMinor < MinorSides; liMinor++)
            {
                int liA = liStart + liMajor * MinorSides + liMinor;
                int liB = liStart + liMajor * MinorSides + (liMinor + 1) % MinorSides;
                int liC = liStart + ((liMajor + 1) % MajorSides) * MinorSides + liMinor;
                int liD = liStart + ((liMajor + 1) % MajorSides) * MinorSides
                    + (liMinor + 1) % MinorSides;
                pOTriangles.Add(liA); pOTriangles.Add(liB); pOTriangles.Add(liC);
                pOTriangles.Add(liC); pOTriangles.Add(liB); pOTriangles.Add(liD);
            }
    }
}
