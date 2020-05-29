﻿using UnityEngine;

public class HexGridChunk : MonoBehaviour {
    private static Color color1 = new Color(1, 0, 0);
    private static Color color2 = new Color(0, 1, 0);
    private static Color color3 = new Color(0, 0, 1);

    private HexCell[] cells;

    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    public HexFeatureManager features;
    private Canvas gridCanvas;

    private void Awake() {
        gridCanvas = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    private void LateUpdate() {
        Triangulate();
        enabled = false;
    }

    public void ShowUI(bool visible) {
        gridCanvas.gameObject.SetActive(visible);
    }

    public void AddCell(int index, HexCell cell) {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh() {
        enabled = true;
    }

    private void Triangulate() {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();

        features.Clear();

        foreach (var cell in cells) {
            Triangulate(cell);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();

        features.Apply();
    }

    private void Triangulate(HexCell cell) {
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }

        if (!cell.IsUnderwater) {
            if (!cell.HasRiver && !cell.HasRoads) {
                features.AddFeature(cell, cell.Position);
            }

            if (cell.IsSpecial) {
                features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    private void Triangulate(HexDirection dir, HexCell cell) {
        var center = cell.Position;
        var e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(dir),
            center + HexMetrics.GetSecondSolidCorner(dir)
        );

        if (cell.HasRiver) {
            if (cell.HasRiverThroughEdge(dir)) {
                e.v3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd) {
                    TriangulateWithRiverBeginOrEnd(dir, cell, center, e);
                }
                else {
                    TriangulateWithRiver(dir, cell, center, e);
                }
            }
            else {
                TriangulateAdjacentToRiver(dir, cell, center, e);
            }
        }
        else {
            TriangulateWithoutRiver(dir, cell, center, e);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(dir)) {
                features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3));
            }
        }

        if (dir <= HexDirection.SE) {
            TriangulateConnection(dir, cell, e);
        }

        if (cell.IsUnderwater) {
            TriangulateWater(dir, cell, center);
        }
    }

    private void TriangulateWithoutRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices e) {
        TriangulateEdgeFan(center, e, color1);

        if (cell.HasRoads) {
            var interpolators = GetRoadInterpolators(dir, cell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, cell.HasRoadThroughEdge(dir)
            );
        }
    }


    #region Water

    private void TriangulateWater(HexDirection dir, HexCell cell, Vector3 center) {
        center.y = cell.WaterSurfaceY;

        var neighbor = cell.GetNeighbor(dir);

        if (neighbor != null && !neighbor.IsUnderwater) {
            TriangulateWaterShore(dir, cell, neighbor, center);
        }
        else {
            TriangulateOpenWater(dir, cell, neighbor, center);
        }
    }

    private void TriangulateOpenWater(HexDirection dir, HexCell cell, HexCell neighbor, Vector3 center) {
        var c1 = center + HexMetrics.GetFirstWaterCorner(dir);
        var c2 = center + HexMetrics.GetSecondWaterCorner(dir);

        water.AddTriangle(center, c1, c2);

        if (dir <= HexDirection.SE && neighbor != null) {
            var bridge = HexMetrics.GetWaterBridge(dir);
            var e1 = c1 + bridge;
            var e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            if (dir <= HexDirection.E) {
                var nextNeighbor = cell.GetNeighbor(dir.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater) {
                    return;
                }

                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(dir.Next()));
            }
        }
    }

    private void TriangulateWaterShore(HexDirection dir, HexCell cell, HexCell neighbor, Vector3 center) {
        var e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(dir),
            center + HexMetrics.GetSecondWaterCorner(dir)
        );

        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        var center2 = neighbor.Position;
        center2.y = center.y;
        var e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(dir.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(dir.Opposite())
        );
        if (cell.HasRiverThroughEdge(dir)) {
            TriangulateEstuary(e1, e2, cell.IncomingRiver == dir);
        }
        else {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        var nextNeighbor = cell.GetNeighbor(dir.Next());
        if (nextNeighbor != null) {
            var v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater
                ? HexMetrics.GetFirstWaterCorner(dir.Previous())
                : HexMetrics.GetFirstSolidCorner(dir.Previous()));
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
        }
    }

    private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver) {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0));
        waterShore.AddTriangleUV(new Vector2(0, 1), new Vector2(0, 0), new Vector2(0, 0));

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 1),
            new Vector2(0, 0)
        );
        estuaries.AddTriangleUV(
            new Vector2(0, 0),
            new Vector2(1, 1),
            new Vector2(1, 1)
        );
        estuaries.AddQuadUV(
            new Vector2(0, 0),
            new Vector2(0, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        );

        // Curved UV2 coordinates that will make the water spread out
        if (incomingRiver) {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1),
                new Vector2(0.7f, 1.15f),
                new Vector2(1, 0.8f),
                new Vector2(.5f, 1.1f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(.5f, 1.1f),
                new Vector2(1, 0.8f),
                new Vector2(0, 0.8f)
            );
            estuaries.AddQuadUV2(
                new Vector2(.5f, 1.1f),
                new Vector2(0.3f, 1.15f),
                new Vector2(0, 0.8f),
                new Vector2(-0.5f, 1f)
            );
        }
        else {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f),
                new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f),
                new Vector2(0.5f, -0.3f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f),
                new Vector2(1.5f, -0.2f)
            );
        }
    }

    #endregion


    #region Roads

    private Vector2 GetRoadInterpolators(HexDirection dir, HexCell cell) {
        Vector2 interpolators;

        if (cell.HasRoadThroughEdge(dir)) {
            interpolators.x = interpolators.y = .5f;
        }
        else {
            interpolators.x = cell.HasRoadThroughEdge(dir.Previous()) ? .5f : .25f;
            interpolators.y = cell.HasRoadThroughEdge(dir.Next()) ? .5f : .25f;
        }

        return interpolators;
    }

    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR) {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 0));
    }

    private void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6) {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    private void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge) {
        if (hasRoadThroughCellEdge) {
            var mC = Vector3.Lerp(mL, mR, .5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(new Vector2(1, 0), new Vector2(0, 0), new Vector2(1, 0));
            roads.AddTriangleUV(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0));
        }
        else {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    private void TriangulateRoadAdjacentToRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices e) {
        var hasRoadThroughEdge = cell.HasRoadThroughEdge(dir);
        var previousHasRiver = cell.HasRiverThroughEdge(dir.Previous());
        var nextHasRiver = cell.HasRiverThroughEdge(dir.Next());

        var interpolators = GetRoadInterpolators(dir, cell);
        var roadCenter = center;

        if (cell.HasRiverBeginOrEnd) {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) {
            Vector3 corner;
            if (previousHasRiver) {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(dir.Next())) return;

                corner = HexMetrics.GetSecondSolidCorner(dir);
            }
            else {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(dir.Previous())) return;

                corner = HexMetrics.GetFirstSolidCorner(dir);
            }

            roadCenter += corner * .5f;
            if (cell.IncomingRiver == dir.Next() &&
                (cell.HasRoadThroughEdge(dir.Next2()) || cell.HasRoadThroughEdge(dir.Opposite()))) {
                features.AddBridge(roadCenter, center - corner * .5f);
            }

            center += corner * .25f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * .2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next()) {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * .2f;
        }
        else if (previousHasRiver && nextHasRiver) {
            if (!hasRoadThroughEdge) return;
            var offset = HexMetrics.GetSolidEdgeMiddle(dir) * HexMetrics.innerToOuter;
            roadCenter += offset * .7f;
            center += offset * .5f;
        }
        else {
            HexDirection middle;
            if (previousHasRiver) {
                middle = dir.Next();
            }
            else if (nextHasRiver) {
                middle = dir.Previous();
            }
            else {
                middle = dir;
            }

            if (!cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
            ) {
                return;
            }

            var offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * .25f;
            if (dir == middle && cell.HasRoadThroughEdge(dir.Opposite())) {
                features.AddBridge(roadCenter, center - offset * (HexMetrics.innerToOuter * .7f));
            }
        }

        var mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        var mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        if (cell.HasRiverThroughEdge(dir.Previous())) {
            TriangulateRoadEdge(roadCenter, center, mL);
        }

        if (cell.HasRiverThroughEdge(dir.Next())) {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    #endregion


    #region Rivers

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed) {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed) {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        rivers.AddQuad(v1, v2, v3, v4);

        if (reversed) {
            rivers.AddQuadUV(1f, 0f, .8f - v, .6f - v);
        }
        else {
            rivers.AddQuadUV(0f, 1f, v, v + .2f);
        }
    }

    private void TriangulateWithRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices e) {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(dir.Opposite())) {
            centerL = center + HexMetrics.GetFirstSolidCorner(dir.Previous()) * .25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(dir.Next()) * .25f;
        }
        else if (cell.HasRiverThroughEdge(dir.Next())) {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(dir.Previous())) {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(dir.Next2())) {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(dir.Next()) * (.5f * HexMetrics.innerToOuter);
        }
        else {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(dir.Previous()) * (.5f * HexMetrics.innerToOuter);
            centerR = center;
        }

        center = Vector3.Lerp(centerL, centerR, .5f);

        var m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, .5f), Vector3.Lerp(centerR, e.v5, .5f), 1f / 6f);
        m.v3.y = center.y = e.v3.y;

        TriangulateEdgeStrip(m, color1, e, color1);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        terrain.AddTriangleColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddQuadColor(color1);
        terrain.AddTriangleColor(color1);

        if (!cell.IsUnderwater) {
            var reversed = cell.IncomingRiver == dir;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, .4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, .6f, reversed);
        }
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices e) {
        var m = new EdgeVertices(Vector3.Lerp(center, e.v1, .5f), Vector3.Lerp(center, e.v5, .5f));

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, color1, e, color1);
        TriangulateEdgeFan(center, m, color1);

        if (!cell.IsUnderwater) {
            var reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, .6f, reversed);
            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed) {
                rivers.AddTriangleUV(new Vector2(.5f, .4f), new Vector2(1f, .2f), new Vector2(0f, .2f));
            }
            else {
                rivers.AddTriangleUV(new Vector2(.5f, .4f), new Vector2(0f, .6f), new Vector2(1f, .6f));
            }
        }
    }

    private void TriangulateAdjacentToRiver(HexDirection dir, HexCell cell, Vector3 center, EdgeVertices e) {
        if (cell.HasRoads) {
            TriangulateRoadAdjacentToRiver(dir, cell, center, e);
        }

        if (cell.HasRiverThroughEdge(dir.Next())) {
            if (cell.HasRiverThroughEdge(dir.Previous())) {
                center += HexMetrics.GetSolidEdgeMiddle(dir) * (HexMetrics.innerToOuter * .5f);
            }
            else if (cell.HasRiverThroughEdge(dir.Previous2())) {
                center += HexMetrics.GetFirstSolidCorner(dir) * .25f;
            }
        }
        else if (cell.HasRiverThroughEdge(dir.Previous()) && cell.HasRiverThroughEdge(dir.Next2())) {
            center += HexMetrics.GetSecondSolidCorner(dir) * .25f;
        }

        var m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, .5f),
            Vector3.Lerp(center, e.v5, .5f)
        );

        TriangulateEdgeStrip(m, color1, e, color1);
        TriangulateEdgeFan(center, m, color1);

        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(dir)) {
            features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3));
        }
    }

    private void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY) {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        var t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);
        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, .8f, 1f);
    }

    #endregion


    #region Grid

    private void TriangulateConnection(HexDirection dir, HexCell cell, EdgeVertices e1) {
        var neighbor = cell.GetNeighbor(dir);
        if (neighbor == null) {
            return;
        }

        var bridge = HexMetrics.GetBridge(dir);
        bridge.y = neighbor.Position.y - cell.Position.y;
        var e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

        var hasRiver = cell.HasRiverThroughEdge(dir);
        var hasRoad = cell.HasRoadThroughEdge(dir);

        if (hasRiver) {
            e2.v3.y = neighbor.StreamBedY;

            if (!cell.IsUnderwater) {
                if (!neighbor.IsUnderwater) {
                    TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == dir
                    );
                }
                else if (cell.Elevation > neighbor.WaterLevel) {
                    TriangulateWaterfallInWater(e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, neighbor.WaterSurfaceY);
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel) {
                TriangulateWaterfallInWater(e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY, cell.WaterSurfaceY);
            }
        }

        // Create cell edge
        if (cell.GetEdgeType(dir) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        }
        else {
            TriangulateEdgeStrip(e1, color1, e2, color2, hasRoad);
        }

        features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        // Create corner triangle
        var nextNeighbor = cell.GetNeighbor(dir.Next());
        if (dir <= HexDirection.E && nextNeighbor != null) {
            var v5 = e1.v5 + HexMetrics.GetBridge(dir.Next());
            v5.y = nextNeighbor.Position.y;

            // Figure out the order in which to orient the corner triangle
            if (cell.Elevation <= neighbor.Elevation) {
                if (cell.Elevation <= nextNeighbor.Elevation) {
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }
        }
    }

    private void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right,
        HexCell rightCell) {
        var leftEdgeType = bottomCell.GetEdgeType(leftCell);
        var rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else {
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(color1, color2, color3);
        }

        features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }

    #endregion


    #region Elevation

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        var v3 = HexMetrics.TerraceLerp(begin, left, 1);
        var v4 = HexMetrics.TerraceLerp(begin, right, 1);
        var c3 = HexMetrics.TerraceLerp(color1, color2, 1);
        var c4 = HexMetrics.TerraceLerp(color2, color3, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(color1, c3, c4);

        for (var i = 2; i < HexMetrics.terraceSteps; i++) {
            var v1 = v3;
            var v2 = v4;
            var c1 = c3;
            var c2 = c4;

            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(color1, color2, i);
            c4 = HexMetrics.TerraceLerp(color2, color3, i);

            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, color2, color3);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        var b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        var boundaryColor = Color.Lerp(color1, color3, b);

        TriangulateBoundaryTriangle(begin, color1, left, color2, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor);
        }
        else {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell) {
        var b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        var boundaryColor = Color.Lerp(color1, color2, b);

        TriangulateBoundaryTriangle(right, color3, begin, color1, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(left, color2, right, color3, boundary, boundaryColor);
        }
        else {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(color2, color3, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, Color beginColor, Vector3 left, Color leftColor,
        Vector3 boundary, Color boundaryColor) {
        var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        var c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginColor, c2, boundaryColor);

        for (var i = 2; i < HexMetrics.terraceSteps; i++) {
            var v1 = v2;
            var c1 = c2;

            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftColor, boundaryColor);
    }

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad) {
        var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        var c2 = HexMetrics.TerraceLerp(color1, color2, 1);

        TriangulateEdgeStrip(begin, color1, e2, c2, hasRoad);

        for (var i = 2; i < HexMetrics.terraceSteps; i++) {
            var e1 = e2;
            var c1 = c2;

            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(color1, color2, i);

            TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, end, color2, hasRoad);
    }

    #endregion


    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color) {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);
        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false) {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);

        if (hasRoad) {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }
}
