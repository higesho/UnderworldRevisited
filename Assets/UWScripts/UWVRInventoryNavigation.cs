using System.Collections.Generic;
using UnityEngine;
using UWDataImport.UWData;

/// <summary>
/// Geometry-only slot navigation for the existing inventory. This deliberately
/// selects the *original screen coordinates*, not a new world-space layout:
/// item dragging, context menus, tooltips and click hit tests continue to use
/// the one existing mouse input path.
/// </summary>
public static class UWVRInventoryNavigation
{
    /// <summary>
    /// Find a clickable position for one paperdoll slot, using the original
    /// hit-zone table and the widened page's exact source-column mapping.
    /// Sampling pixel centres and checking the table's winner also handles
    /// overlaps between rings/hands and legs/gloves. Discard points clipped
    /// by the character panel's scrolling viewport.
    /// </summary>
    public static bool TryGetEquipmentCentre(UWClickRules.InventorySpot peSpot,
        Rect pOPageRect, Rect pOViewport, float pfScale, out Vector2 pOCentre)
    {
        pOCentre = Vector2.zero;

        if (pfScale <= 0f || pOPageRect.width <= 0f || pOViewport.width <= 0f
            || !UWClickRules.TryGetInventorySpotArea(peSpot, out UWClickRules.Area lOArea)
            || !UWHudInventory.GetSlotOfSpot(peSpot).HasValue)
            return false;

        RectInt lOPage = UWModernHudArt.PaperdollPage;
        float lfMidX = (lOArea.X0 + lOArea.X1) * 0.5f;
        float lfMidY = (lOArea.Y0 + lOArea.Y1) * 0.5f;
        float lfBest = float.MaxValue;
        bool lbFound = false;

        for (int liColumn = 0; liColumn < lOPage.width; liColumn++)
        {
            int liMainX = UWModernHudArt.PageColumnToMain(liColumn);
            if (liMainX < lOArea.X0 || liMainX > lOArea.X1)
                continue;

            float lfX = pOPageRect.xMin + ((liColumn + 0.5f) * pfScale);
            if (lfX < pOViewport.xMin || lfX >= pOViewport.xMax)
                continue;

            for (int liY = lOArea.Y0; liY <= lOArea.Y1; liY++)
            {
                int liRow = 199 - liY - lOPage.y;
                if (liRow < 0 || liRow >= lOPage.height
                    || UWClickRules.InventorySpotAt(liMainX, liY) != peSpot)
                    continue;

                float lfY = pOPageRect.yMax - ((liRow + 0.5f) * pfScale);
                if (lfY < pOViewport.yMin || lfY >= pOViewport.yMax)
                    continue;

                float lfDX = liMainX - lfMidX;
                float lfDY = liY - lfMidY;
                float lfDistance = (lfDX * lfDX) + (lfDY * lfDY);
                if (lfDistance >= lfBest)
                    continue;

                lfBest = lfDistance;
                pOCentre = new Vector2(lfX, lfY);
                lbFound = true;
            }
        }

        return lbFound;
    }

    /// <summary>
    /// Find the nearest visible slot when arriving from elsewhere; otherwise
    /// move a single slot in the dominant stick direction. The chosen slot
    /// must be strictly ahead of the current slot, and alignment is weighted
    /// more heavily than raw distance so navigating a four-column bag is
    /// predictable even with several open containers.
    /// </summary>
    public static bool TryChooseNext(IReadOnlyList<Vector2> pOPoints,
        Vector2 pOCursor, Vector2 pODirection, bool pbCursorOnSlot, out Vector2 pONext)
    {
        pONext = Vector2.zero;

        if (pOPoints == null || pOPoints.Count == 0 || pODirection.sqrMagnitude < 0.01f)
            return false;

        int liNearest = -1;
        float lfNearestDistance = float.MaxValue;

        for (int liAt = 0; liAt < pOPoints.Count; liAt++)
        {
            float lfDistance = (pOPoints[liAt] - pOCursor).sqrMagnitude;
            if (lfDistance < lfNearestDistance)
            {
                lfNearestDistance = lfDistance;
                liNearest = liAt;
            }
        }

        if (liNearest < 0)
            return false;

        Vector2 lOAnchor = pOPoints[liNearest];

        if (!pbCursorOnSlot)
        {
            pONext = lOAnchor;
            return true;
        }

        bool lbHorizontal = Mathf.Abs(pODirection.x) >= Mathf.Abs(pODirection.y);
        float lfSign = Mathf.Sign(lbHorizontal ? pODirection.x : pODirection.y);

        int liBest = -1;
        float lfBestScore = float.MaxValue;

        for (int liAt = 0; liAt < pOPoints.Count; liAt++)
        {
            if (liAt == liNearest)
                continue;

            Vector2 lODelta = pOPoints[liAt] - lOAnchor;
            float lfForward = (lbHorizontal ? lODelta.x : lODelta.y) * lfSign;
            float lfSide = Mathf.Abs(lbHorizontal ? lODelta.y : lODelta.x);

            if (lfForward <= 0.5f)
                continue;

            float lfScore = lfForward + lfSide * 3f;
            if (lfScore < lfBestScore)
            {
                lfBestScore = lfScore;
                liBest = liAt;
            }
        }

        if (liBest < 0)
            return false;

        pONext = pOPoints[liBest];
        return true;
    }
}
