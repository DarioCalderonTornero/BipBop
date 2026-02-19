using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay único que dibuja un marco oscuro con un agujero rectangular transparente.
/// Al ser una sola malla no hay solapamientos ni zonas más oscuras en las esquinas.
///
/// SETUP EN UNITY:
///  1. Crea un GameObject hijo del TutorialCanvas llamado "HoleOverlay".
///  2. Añádele este componente. Asegúrate de que su RectTransform cubra toda la pantalla
///     (anchors 0,0 → 1,1, offsets 0).
///  3. Asígna el color del overlay (p.ej. negro con alpha 0.85) desde el Inspector o por código.
///  4. En TutorialManager elimina las referencias a darkTop/Bottom/Left/Right y usa
///     las propiedades HoleRect y Visible de este componente en su lugar.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class HoleOverlay : Graphic
{
    // ── Rect del agujero en espacio LOCAL de este RectTransform (píxeles desde el centro) ──
    // Cuando holeRect está vacío (width/height == 0) se dibuja el overlay completo sin agujero.
    private Rect _holeRect = Rect.zero;

    public Rect HoleRect
    {
        get => _holeRect;
        set
        {
            _holeRect = value;
            SetVerticesDirty();
        }
    }

    /// <summary>
    /// Shortcut para poner o quitar el agujero sin cambiar su posición.
    /// Pasando Rect.zero se rellena todo (overlay completo).
    /// </summary>
    public void SetHole(Rect localRect)
    {
        HoleRect = localRect;
    }

    public void ClearHole()
    {
        HoleRect = Rect.zero;
    }

    // ── Generación de la malla ──────────────────────────────────────────────────────────────
    //
    // La malla tiene forma de "donut" rectangular:
    //
    //   0 ──────────────── 1
    //   |                  |
    //   |  4 ────────── 5  |
    //   |  |   (hole)   |  |
    //   |  7 ────────── 6  |
    //   |                  |
    //   3 ──────────────── 2
    //
    // 8 triángulos forman el marco. El interior (4-5-6-7) queda vacío.
    // Si no hay agujero (holeRect vacío) se usa un único quad 0-1-2-3.

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect outer = GetPixelAdjustedRect(); // rect del RectTransform en espacio local

        // Sin agujero → quad sólido simple
        if (_holeRect.width <= 0f || _holeRect.height <= 0f)
        {
            AddQuad(vh,
                new Vector2(outer.xMin, outer.yMin),
                new Vector2(outer.xMax, outer.yMax),
                color);
            return;
        }

        // Clampear el agujero dentro del outer para no tener vértices fuera
        float hxMin = Mathf.Clamp(_holeRect.xMin, outer.xMin, outer.xMax);
        float hxMax = Mathf.Clamp(_holeRect.xMax, outer.xMin, outer.xMax);
        float hyMin = Mathf.Clamp(_holeRect.yMin, outer.yMin, outer.yMax);
        float hyMax = Mathf.Clamp(_holeRect.yMax, outer.yMin, outer.yMax);

        // 8 vértices del donut
        //  outer corners        hole corners
        Vector2 o_bl = new Vector2(outer.xMin, outer.yMin); // 0
        Vector2 o_tl = new Vector2(outer.xMin, outer.yMax); // 1
        Vector2 o_tr = new Vector2(outer.xMax, outer.yMax); // 2
        Vector2 o_br = new Vector2(outer.xMax, outer.yMin); // 3

        Vector2 h_bl = new Vector2(hxMin, hyMin); // 4
        Vector2 h_tl = new Vector2(hxMin, hyMax); // 5
        Vector2 h_tr = new Vector2(hxMax, hyMax); // 6
        Vector2 h_br = new Vector2(hxMax, hyMin); // 7

        // Añadimos los 8 vértices
        AddVert(vh, o_bl, color); // 0
        AddVert(vh, o_tl, color); // 1
        AddVert(vh, o_tr, color); // 2
        AddVert(vh, o_br, color); // 3
        AddVert(vh, h_bl, color); // 4
        AddVert(vh, h_tl, color); // 5
        AddVert(vh, h_tr, color); // 6
        AddVert(vh, h_br, color); // 7

        // 8 triángulos (= 4 quads alrededor del agujero)
        // Bottom strip  (entre yMin_outer y yMin_hole)
        vh.AddTriangle(0, 4, 7);
        vh.AddTriangle(0, 7, 3);

        // Top strip     (entre yMax_hole y yMax_outer)
        vh.AddTriangle(1, 2, 6);
        vh.AddTriangle(1, 6, 5);

        // Left strip    (entre xMin_outer y xMin_hole, sólo la banda central Y)
        vh.AddTriangle(0, 1, 5);
        vh.AddTriangle(0, 5, 4);

        // Right strip   (entre xMax_hole y xMax_outer, sólo la banda central Y)
        vh.AddTriangle(7, 6, 2);
        vh.AddTriangle(7, 2, 3);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────────────

    private static void AddVert(VertexHelper vh, Vector2 pos, Color32 col)
    {
        var uiv = new UIVertex { position = pos, color = col, uv0 = Vector2.zero };
        vh.AddVert(uiv);
    }

    private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 col)
    {
        int start = vh.currentVertCount;
        AddVert(vh, new Vector2(min.x, min.y), col); // bl
        AddVert(vh, new Vector2(min.x, max.y), col); // tl
        AddVert(vh, new Vector2(max.x, max.y), col); // tr
        AddVert(vh, new Vector2(max.x, min.y), col); // br
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    // Que Unity recalcule cuando cambia el tamaño del RectTransform
    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}