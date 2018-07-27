using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public class SmudgeHeightTool : TerrainPaintTool<SmudgeHeightTool>
    {
        //[SerializeField]
        //float m_SmudgeAmount;

        EventType m_PreviousEvent = EventType.Ignore;
        Vector2 m_PrevBrushPos = new Vector2(0.0f, 0.0f);

        Material m_Material = null;
        Material GetPaintMaterial()
        {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("SmudgeHeight"));
            return m_Material;
        }

        public override string GetName()
        {
            return "Smudge Height";
        }

        public override string GetDesc()
        {
            return "Click to Smudge the terrain height in the direction of the brush stroke.";
        }

        public override void OnSceneGUI(SceneView sceneView, Terrain terrain, Texture brushTexture, float brushStrength, int brushSizeInTerrainUnits, float brushRotation = 0.0f, bool holdPosition = false)
        {
            TerrainPaintUtilityEditor.ShowDefaultPreviewBrush(terrain, brushTexture, brushStrength, brushSizeInTerrainUnits, brushRotation, 0.0f, holdPosition);
        }
        public override void OnInspectorGUI(Terrain terrain)
        {
            EditorGUI.BeginChangeCheck();

            //m_SmudgeAmount = EditorGUILayout.Slider(new GUIContent("Smudge Amount", ""), m_SmudgeAmount, 0.0f, 100.0f);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool Paint(Terrain terrain, Texture brushTexture, Vector2 uv, float brushStrength, int brushSize, float brushRotation = 0.0f)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                Debug.Log("Begin Stroke");
                m_PrevBrushPos = uv;
                return false;
            }
            if (Event.current.type == EventType.MouseDrag && m_PreviousEvent == EventType.MouseDrag) 
            {
                Rect brushRect = TerrainPaintUtility.CalculateBrushRect(terrain, uv, brushSize, brushRotation);
                TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect, "Terrain Paint - Smudge Height");

                Vector2 smudgeDir = uv - m_PrevBrushPos;

                Material mat = GetPaintMaterial();
                Vector4 brushParams = new Vector4(brushStrength, smudgeDir.x, smudgeDir.y, brushRotation);
                mat.SetTexture("_BrushTex", brushTexture);
                mat.SetVector("_BrushParams", brushParams);
                Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

                TerrainPaintUtility.EndPaintHeightmap(paintContext);

                m_PrevBrushPos = uv;
            }
            m_PreviousEvent = Event.current.type;
            return false;
        }
    }
}
