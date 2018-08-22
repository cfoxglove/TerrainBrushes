using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;
using UnityEditor.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
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

            //m_SmudgeAmount = EditorGUILayout.Slider(new GUIContent("Smudge Amount", ""), m_SmudgeAmount, 0.0f, 100.0f);

            editContext.ShowBrushesGUI(0);

            if (EditorGUI.EndChangeCheck())
                Save(true);
        }

        public override bool OnPaint(Terrain terrain, IOnPaint editContext)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                Debug.Log("Begin Stroke");
                m_PrevBrushPos = editContext.uv;
                return false;
            }
            if (Event.current.type == EventType.MouseDrag && m_PreviousEvent == EventType.MouseDrag) 
            {
                Rect brushRect = TerrainPaintUtility.CalculateBrushRectInTerrainUnits(terrain, editContext.uv, editContext.brushSize);
                TerrainPaintUtility.PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushRect);

                Vector2 smudgeDir = editContext.uv - m_PrevBrushPos;

                Material mat = GetPaintMaterial();
                Vector4 brushParams = new Vector4(editContext.brushStrength, smudgeDir.x, smudgeDir.y, 0);
                mat.SetTexture("_BrushTex", editContext.brushTexture);
                mat.SetVector("_BrushParams", brushParams);
                Graphics.Blit(paintContext.sourceRenderTexture, paintContext.destinationRenderTexture, mat, 0);

                TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smudge Height");

                m_PrevBrushPos = editContext.uv;
            }
            m_PreviousEvent = Event.current.type;
            return false;
        }
    }
}
