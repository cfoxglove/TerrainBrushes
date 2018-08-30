using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class TerraceErosion : TerrainPaintTool<TerraceErosion>
    {
        [SerializeField]
        float m_FeatureSize;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TerraceErosion"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Erosion/Terrace Erosion";
        }

        public override string GetDesc()
        {
            return "Use to terrace terrain";
        }

        public override void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain,
                                                              editContext.brushTexture,
                                                              editContext.brushStrength, 
                                                              editContext.brushSize,  
                                                              0.0f);
        }

        public override void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext)
        {
            editContext.ShowBrushesGUI(0);

            EditorGUI.BeginChangeCheck();

            m_FeatureSize = EditorGUILayout.Slider(new GUIContent("Terrace Count", "Larger value will result in more terraces"), m_FeatureSize, 1.0f, 300.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, 0.0f, m_FeatureSize, 0.0f);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Terrace Erosion");
            return false;
        }
    }
}
