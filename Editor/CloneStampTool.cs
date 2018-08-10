using UnityEngine;

namespace UnityEditor
{
    public class CloneStampTool : TerrainPaintTool<CloneStampTool>
    {
        private enum ShaderPasses
        {
            CloneAlphamap = 0,
            CloneHeightmap
        }

        [SerializeField]
        bool m_PaintHeightmap = true;
        [SerializeField]
        bool m_PaintAlphamap = true;
        [SerializeField]
        bool m_Aligned = false;
        [SerializeField]
        float m_StampingOffsetFromClone = 0.0f;

        Material m_Material = null;
        private Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("CloneStamp"));
            return m_Material;
        }

        struct BrushLocationData
        {
            public Vector3 m_Position;
            public Vector2 m_UV;
            public Terrain m_Terrain;
        };

        private BrushLocationData m_Sample;
        private BrushLocationData m_SnapbackCache;
        private BrushLocationData m_LastPaintLocation;
        private Vector2Int m_CloneRectPixelOffsetFromStampRect = Vector2Int.zero; // offset from the stamp rect to where the clone rect would be
        private bool m_ActivePaint = false; // if we are in an active paint (left mouse held down)
        private bool m_PaintedOnce = false; // if we have painted once after choosing a clone location (used for aligned mode)

        public override string GetName()
        {
            return "Clone Stamp Tool";
        }

        public override string GetDesc()
        {
            return "Clones terrain from another area of the terrain map to the selected location.\n\n" +
                "Hold control and Left Click to assign the clone sample area.\n\n" +
                "Left Click to apply the cloned stamp.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits, float brushRotation, bool holdPosition)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength * 0.01f, brushSizeInTerrainUnits, brushRotation, 0.0f, holdPosition);

            bool drawCloneBrush = true;

            // on mouse up
            if (Event.current.type == EventType.MouseUp)
            {
                m_ActivePaint = false;
                if (!m_Aligned)
                {
                    m_Sample.m_UV = m_SnapbackCache.m_UV;
                    m_Sample.m_Terrain = m_SnapbackCache.m_Terrain;
                    m_Sample.m_Position = m_SnapbackCache.m_Position;
                }
            }

            // on mouse move
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
            {
                if (m_Aligned && m_PaintedOnce)
                {
                    RaycastHit hit;
                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity);

                    // check for moving across tiles
                    if (terrain != m_LastPaintLocation.m_Terrain && m_LastPaintLocation.m_Terrain != null)
                        UpdateLastPosition(hit, terrain);

                    if (m_Sample.m_Terrain != null)
                        PositionCloneBrush(hit);

                    // capture last (current) location for next frame
                    m_LastPaintLocation.m_UV = hit.textureCoord;
                    m_LastPaintLocation.m_Position = hit.point;
                    m_LastPaintLocation.m_Terrain = terrain;
                }

                if (m_Aligned && !m_PaintedOnce)
                    drawCloneBrush = false; // dont draw if we havent selected where to paint yet when aligned
            }

            // draw the clone brush preview
            if (m_Sample.m_Terrain != null && drawCloneBrush)
            {
                Rect sampleRect = TerrainPaintUtility.CalculateBrushRect(m_Sample.m_Terrain, m_Sample.m_UV, brushSizeInTerrainUnits);
                TerrainPaintUtility.PaintContext ctx = TerrainPaintUtility.BeginPaintHeightmap(m_Sample.m_Terrain, sampleRect, null);

                ctx.sourceRenderTexture.filterMode = FilterMode.Bilinear;
                brushTexture.filterMode = FilterMode.Bilinear;

                Vector2 topLeft = ctx.brushRect.min;
                float xfrac = ((topLeft.x - (int)topLeft.x) / (float)ctx.sourceRenderTexture.width);
                float yfrac = ((topLeft.y - (int)topLeft.y) / (float)ctx.sourceRenderTexture.height);

                Vector4 texScaleOffset = new Vector4(0.5f, 0.5f, 0.5f + xfrac + 0.5f / (float)ctx.sourceRenderTexture.width, 0.5f + yfrac + 0.5f / (float)ctx.sourceRenderTexture.height);

                RaycastHit hit = new RaycastHit();
                hit.point = m_Sample.m_Position;

                TerrainPaintUtilityEditor.DrawDefaultBrushPreviewMesh(m_Sample.m_Terrain, hit, ctx.sourceRenderTexture, brushTexture, 0.0001f, brushSizeInTerrainUnits, brushRotation, TerrainPaintUtilityEditor.defaultPreviewPatchMesh, true, texScaleOffset);
                TerrainPaintUtility.ReleaseContextResources(ctx);
            }
        }

        public override void OnInspectorGUI(Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();
            m_PaintAlphamap = EditorGUILayout.Toggle(new GUIContent("Clone terrain textures", "Will clone all textures in the clone area on the terrain."), m_PaintAlphamap);
            m_PaintHeightmap = EditorGUILayout.Toggle(new GUIContent("Clone terrain heightmap", "Will clone the heightmap in the clone area on the terrain."), m_PaintHeightmap);

            // aligned mode must reset when it is activated
            bool previousAligned = m_Aligned;
            m_Aligned = EditorGUILayout.Toggle(new GUIContent("Aligned", "Aligned mode will follow the mouse movement even when not actively painting."), m_Aligned);
            if (m_Aligned != previousAligned)
                m_PaintedOnce = false;

            m_StampingOffsetFromClone = EditorGUILayout.Slider(new GUIContent("Height Offset", "When stamping the heightmap, the cloned height will be added with this offset to raise or lower the cloned height at the stamp location."),
                    m_StampingOffsetFromClone,
                    -terrain.terrainData.size.y,
                    terrain.terrainData.size.y);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        private void StampRender(TerrainPaintUtility.PaintContext read, TerrainPaintUtility.PaintContext write, Material mat, int materialPass)
        {
            // since we only blit the areas that a writeRect is located, we need to get the rest of the texture in destination too
            Graphics.Blit(write.sourceRenderTexture, write.destinationRenderTexture, TerrainPaintUtility.GetBlitMaterial(), 0);

            read.sourceRenderTexture.filterMode = FilterMode.Point;
            mat.SetTexture("_CloneLocation", read.sourceRenderTexture);

            // only blit areas that we can read from in the read context (protects against writing uninitialized memory in the texture when read rect is off of terrain)
            for (int i = 0; i < read.validPaintRects.Length; ++i)
            {
                Rect writeRect = read.validPaintRects[i];
                if (writeRect.width == 0 || writeRect.height == 0)
                    continue;

                writeRect.x /= (float)read.brushRect.width;
                writeRect.y /= (float)read.brushRect.height;
                writeRect.width /= (float)read.brushRect.width;
                writeRect.height /= (float)read.brushRect.height;
                mat.SetVector("_WriteRect", new Vector4(writeRect.x, writeRect.y, writeRect.xMax, writeRect.yMax));
                Graphics.Blit(write.sourceRenderTexture, write.destinationRenderTexture, mat, materialPass);
            }
        }

        private void PaintHeightmap(Rect writeBrushRect, Terrain terrain, Material mat)
        {
            TerrainPaintUtility.PaintContext write = TerrainPaintUtility.BeginPaintHeightmap(terrain, writeBrushRect, "Terrain Paint - Clone Stamp Tool (Heightmap)");

            // read created manually to use write's rect with offset
            TerrainPaintUtility.PaintContext read = new TerrainPaintUtility.PaintContext();
            read.brushRect = new RectInt(write.brushRect.position + m_CloneRectPixelOffsetFromStampRect, write.brushRect.size);
            read.CreateTerrainTiles(m_Sample.m_Terrain,
                m_Sample.m_Terrain.terrainData.heightmapWidth,
                m_Sample.m_Terrain.terrainData.heightmapHeight);
            read.CreateRenderTargets(m_Sample.m_Terrain.terrainData.heightmapTexture.format);
            read.GatherHeightmap(m_Sample.m_Terrain, null);

            // render mix of clone and stamp areas
            StampRender(read, write, mat, (int)ShaderPasses.CloneHeightmap);

            TerrainPaintUtility.ReleaseContextResources(read);
            TerrainPaintUtility.EndPaintHeightmap(write);
        }

        private void PaintAlphamap(Rect writeBrushRect, Terrain terrain, Material mat)
        {
            // paint each layer from the sample to the clone location (adds layer if not present already)
            for (int i = 0; i < m_Sample.m_Terrain.terrainData.terrainLayers.Length; ++i)
            {
                TerrainPaintUtility.PaintContext write = TerrainPaintUtility.BeginPaintTexture(terrain,
                        writeBrushRect,
                        m_Sample.m_Terrain.terrainData.terrainLayers[i],
                        "Terrain Paint - Clone Stamp Tool (Alphamap layer " + i + ")");

                if (write == null)
                    continue;

                // read created manually to force read to not add the layer if it wasnt already on the terrain, and to use write's rect with offset
                TerrainPaintUtility.PaintContext read = new TerrainPaintUtility.PaintContext();

                read.brushRect = new RectInt(write.brushRect.position + m_CloneRectPixelOffsetFromStampRect, write.brushRect.size);
                read.CreateTerrainTiles(m_Sample.m_Terrain,
                    m_Sample.m_Terrain.terrainData.alphamapWidth,
                    m_Sample.m_Terrain.terrainData.alphamapHeight);
                read.CreateRenderTargets(RenderTextureFormat.R8);
                read.GatherAlphamap(m_Sample.m_Terrain, null, m_Sample.m_Terrain.terrainData.terrainLayers[i], false);

                // render mix of clone and stamp areas
                StampRender(read, write, mat, (int)ShaderPasses.CloneAlphamap);

                TerrainPaintUtility.ReleaseContextResources(read);
                TerrainPaintUtility.EndPaintTexture(write);
            }
        }

        private void PositionCloneBrush(RaycastHit hit)
        {
            // move the clone brush
            m_Sample.m_UV += hit.textureCoord - m_LastPaintLocation.m_UV;
            m_Sample.m_Position += hit.point - m_LastPaintLocation.m_Position;

            // adjust for terrain and uv over/under fill
            if (m_Sample.m_UV.x >= 1.0f && m_Sample.m_Terrain.rightNeighbor != null)
            {
                m_Sample.m_Terrain = m_Sample.m_Terrain.rightNeighbor;
                m_Sample.m_UV.x -= 1.0f;
            }
            else if (m_Sample.m_UV.x <= 0.0f && m_Sample.m_Terrain.leftNeighbor != null)
            {
                m_Sample.m_Terrain = m_Sample.m_Terrain.leftNeighbor;
                m_Sample.m_UV.x += 1.0f;
            }

            if (m_Sample.m_UV.y >= 1.0f && m_Sample.m_Terrain.topNeighbor != null)
            {
                m_Sample.m_Terrain = m_Sample.m_Terrain.topNeighbor;
                m_Sample.m_UV.y -= 1.0f;
            }
            else if (m_Sample.m_UV.y <= 0.0f && m_Sample.m_Terrain.bottomNeighbor != null)
            {
                m_Sample.m_Terrain = m_Sample.m_Terrain.bottomNeighbor;
                m_Sample.m_UV.y += 1.0f;
            }
        }

        private void UpdateLastPosition(RaycastHit hit, Terrain terrain)
        {
            if (terrain == m_LastPaintLocation.m_Terrain.rightNeighbor)
                m_LastPaintLocation.m_UV.x -= 1.0f;
            else if (terrain == m_LastPaintLocation.m_Terrain.leftNeighbor)
                m_LastPaintLocation.m_UV.x += 1.0f;
            else if (terrain == m_LastPaintLocation.m_Terrain.topNeighbor)
                m_LastPaintLocation.m_UV.y -= 1.0f;
            else if (terrain == m_LastPaintLocation.m_Terrain.bottomNeighbor)
                m_LastPaintLocation.m_UV.y += 1.0f;

            m_LastPaintLocation.m_Terrain = terrain;
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize, float brushRotation)
        {
            RaycastHit hit;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (!terrain.GetComponent<Collider>().Raycast(mouseRay, out hit, Mathf.Infinity))
                return false;

            // select clone location
            if (Event.current.control)
            {
                m_Sample.m_UV = uv;
                m_Sample.m_Terrain = terrain;
                m_Sample.m_Position = hit.point;
                m_SnapbackCache.m_UV = uv;
                m_SnapbackCache.m_Position = hit.point;
                m_SnapbackCache.m_Terrain = terrain;
                m_PaintedOnce = false;

                return false;   // no painting while setting sample location
            }

            // dont do anyting if we dont have a sample
            if (m_Sample.m_Terrain == null)
                return false;

            // if we are starting a new paint (first mouse down)
            if (m_ActivePaint == false)
            {
                if (m_Aligned)
                {
                    m_SnapbackCache.m_UV = m_Sample.m_UV;
                    m_SnapbackCache.m_Position = m_Sample.m_Position;
                    m_SnapbackCache.m_Terrain = m_Sample.m_Terrain;
                }

                // capture current position
                m_LastPaintLocation.m_UV = uv;
                m_LastPaintLocation.m_Position = hit.point;
                m_LastPaintLocation.m_Terrain = terrain;
            }

            m_ActivePaint = true;
            m_PaintedOnce = true;   // for aligned movement mode

            // check for moving across tiles
            if (terrain != m_LastPaintLocation.m_Terrain && m_LastPaintLocation.m_Terrain != null)
                UpdateLastPosition(hit, terrain);

            // aligned mode is updated in OnSceneGUI
            if (!m_Aligned)
                PositionCloneBrush(hit);

            Rect writeBrushRect = TerrainPaintUtility.CalculateBrushRect(terrain, uv, brushSize);

            m_CloneRectPixelOffsetFromStampRect.x = (int)((m_Sample.m_UV.x - hit.textureCoord.x) * (float)m_Sample.m_Terrain.terrainData.heightmapWidth);
            m_CloneRectPixelOffsetFromStampRect.y = (int)((m_Sample.m_UV.y - hit.textureCoord.y) * (float)m_Sample.m_Terrain.terrainData.heightmapHeight);

            // capture last (current) location for next frame
            m_LastPaintLocation.m_UV = uv;
            m_LastPaintLocation.m_Position = hit.point;
            m_LastPaintLocation.m_Terrain = terrain;

            Material mat = GetPaintMaterial();
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", new Vector4(brushStrength, m_StampingOffsetFromClone * 0.5f, terrain.terrainData.size.y, brushRotation));

            // draw
            if (m_PaintAlphamap)
                PaintAlphamap(writeBrushRect, terrain, mat);

            if (m_PaintHeightmap)
                PaintHeightmap(writeBrushRect, terrain, mat);

            return true;
        }
    }
}
