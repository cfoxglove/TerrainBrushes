using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class SlopeFlattenTool : TerrainPaintTool<SlopeFlattenTool>
    {
        [SerializeField]
        float m_FeatureSize;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("SlopeFlatten"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Slope Flatten Height";
        }

        public override string GetDesc()
        {
            return "Click to flatten terrain, while maintaining slope.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits, float brushRotation = 0.0f, bool holdPosition = false)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength, brushSizeInTerrainUnits, brushRotation, 0.0f, holdPosition);
        }
        public override void OnInspectorGUI(Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();

            m_FeatureSize = EditorGUILayout.Slider(new GUIContent("Detail Size", "Larger value will enhance larger features, smaller values will enhance smaller features"), m_FeatureSize, 1.0f, 100.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize, float brushRotation = 0.0f)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRect(terrain, uv, brushSize, brushRotation);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect, "Terrain Paint - Flatten Slope Height");

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, m_FeatureSize, brushRotation);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext);
            return false;
        }
    }
}
