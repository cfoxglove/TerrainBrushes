using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class ErodeHeightTool : TerrainPaintTool<ErodeHeightTool>
    {
        [SerializeField]
        float m_FeatureSize;

        [SerializeField]
        float m_Sharpness;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("ErodeHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Erode Height";
        }

        public override string GetDesc()
        {
            return "Click to erode the terrain height away fromt the local maxima.";
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
            EditorGUI.BeginChangeCheck();

            m_FeatureSize = EditorGUILayout.Slider(new GUIContent("Detail Size", "Larger value will enhance larger features, smaller values will enhance smaller features"), m_FeatureSize, 1.0f, 100.0f);
            //m_Sharpness = EditorGUILayout.Slider(new GUIContent("Erosion Sharpness", "Larger values will result in steeper erosion"), m_Sharpness, 0.8f, 3.0f);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(editContext.brushStrength, m_Sharpness, m_FeatureSize, editContext.brushRotation);
            mat.SetTexture("_BrushTex", editContext.brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Erode Height");
            return false;
        }
    }
}
