using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class TwistHeightTool : TerrainPaintTool<TwistHeightTool>
    {
        [SerializeField]
        float m_TwistAmount;

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("TwistHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Twist Height";
        }

        public override string GetDesc()
        {
            return "Click to Twist the terrain height.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits, float brushRotation = 0.0f, bool holdPosition = false)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength, brushSizeInTerrainUnits, brushRotation, 0.0f, holdPosition);
        }
        public override void OnInspectorGUI(Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();

            m_TwistAmount = EditorGUILayout.Slider(new GUIContent("Twist Amount", "Negative values twist clockwise, Positive values twist counter clockwise"), m_TwistAmount, 0.0f, 100.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize, float brushRotation = 0.0f)
        {
            Rect brushRect = TerrainPaintUtility.CalculateBrushRect(terrain, uv, brushSize, brushRotation);
            TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect, "Terrain Paint - Twist Height");

            float finalTwistAmount = m_TwistAmount * -0.001f; //scale to a reasonable value and negate so default mode is clockwise
            if (Event.current.shift) {
                finalTwistAmount *= -1.0f;
            }

            Material mat = GetPaintMaterial();
            Vector4 brushParams = new Vector4(brushStrength, 0.0f, finalTwistAmount, brushRotation);
            mat.SetTexture("_BrushTex", brushTexture);
            mat.SetVector("_BrushParams", brushParams);
            Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

            TerrainPaintUtility.EndPaintHeightmap(paintContext);
            return false;
        }
    }
}
