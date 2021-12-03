using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AltVATCreateTexturesFromCSV : EditorWindow
{
    private TextAsset sourceText;
   
    
    [MenuItem("Window/altVAT Create Textures From CSV")]
    public static void Init()
    {
        AltVATCreateTexturesFromCSV window =
            (AltVATCreateTexturesFromCSV) EditorWindow.GetWindow(typeof(AltVATCreateTexturesFromCSV));
        window.Show();
    }

    private void OnGUI()
    {
        
        GUILayout.Label("Create Textures From CSV", EditorStyles.boldLabel);

        sourceText = (TextAsset) EditorGUI.ObjectField(new Rect(3, 20, position.width - 6, 20), "Source CSV", sourceText,
            typeof(TextAsset));

        if (sourceText)
        {
            if (GUI.Button(new Rect(3, 65, position.width - 6, 20), "Create Mesh and Textures"))
            {
                var newMesh = CreateMeshFromLinesCSV(sourceText.text, sourceText.name);

                var sourcePath = AssetDatabase.GetAssetPath(sourceText);
                var targetPath = sourcePath.Replace("csv", "_mesh.asset");
                var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(targetPath);
                if (existingMesh == null)
                {
                    Debug.Log("Creating new mesh");
                    AssetDatabase.CreateAsset(newMesh,targetPath);
                }
                else
                {
                    Debug.Log("Deleting old mesh");
                    AssetDatabase.DeleteAsset(targetPath);
                    AssetDatabase.CreateAsset(newMesh,targetPath);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            
        }
    }

    private class LinePrim
    {
        public LinePrim()
        {
            verts = new List<Vector3>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            lineIndices = new List<int>();

        }
        public List<Vector3> verts = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<int> lineIndices = new List<int>();
    }
    
    private struct AnimatedMesh
	{
		public AnimatedMesh(){

		}

		

	}


    
    

    private Mesh CreateMeshFromLinesCSV(string csv, string name)
    {
        // CSV format
        // p
        // v,(x),(y),(z),
        // ..
        // n,(x),(y),(z),
        // ..
        // uv,(x),(y),(z),
        // p
        // and so on
        
        Debug.Log("converting " + name);
        var m = new Mesh();
        m.name = name;
        
        var rows = csv.Split('\n');
        List<LinePrim> lines = new List<LinePrim>();
        LinePrim currentPrim = null;
        foreach (var row in rows)
        {
            var r = row.Replace(" ", string.Empty);
            r = row.Replace("\r", string.Empty);
            var cols = r.Split(',');
            
            var start = cols[0];
            switch (start)
            {
                case "p":
                    // start new primitive
                    if (currentPrim != null) { lines.Add(currentPrim); }
                    currentPrim = new LinePrim();
                    break;
                case "v":
                    // add vert
                    var p_x = float.Parse(cols[1]);
                    var p_y = float.Parse(cols[2]);
                    var p_z = float.Parse(cols[3]);
                    currentPrim.verts.Add(new Vector3(p_x, p_y, p_z));
                    break;
                case "n":
                    // add normal
                    var n_x = float.Parse(cols[1]);
                    var n_y = float.Parse(cols[2]);
                    var n_z = float.Parse(cols[3]);
                    currentPrim.normals.Add(new Vector3(n_x, n_y, n_z));
                    break;
                case "uv":
                    // add uv
                    var uv_x = float.Parse(cols[1]);
                    var uv_y = float.Parse(cols[2]);
                    currentPrim.uvs.Add(new Vector2(uv_x, uv_y));
                    break;
                default:

                    break;
            }
        }
        lines.Add(currentPrim);

        var meshVerts = new List<Vector3>();
        var meshNormals = new List<Vector3>();
        var meshUVs = new List<Vector2>();
        var meshIndices = new List<int>();

        foreach (var line in lines)
        {
            var lineIndices = new List<int>();
            for (int i = 0; i < line.verts.Count; i++)
            {
                lineIndices.Add(meshVerts.Count);
                meshVerts.Add(line.verts[i]);
                meshNormals.Add(line.normals[i]);
                meshUVs.Add(line.uvs[i]);
            }

            int idx = 0;
            while (idx < lineIndices.Count)
            {
                // make pairs of indices for lines
                if (idx < (lineIndices.Count - 1))
                {
                    meshIndices.Add(lineIndices[idx]);
                    meshIndices.Add(lineIndices[idx+1]);
                }
                idx++;
            }
        }

        Debug.Log("Finished exporting mesh, " + meshVerts.Count + " verts, " + (meshIndices.Count / 2) + " lines");
        //
        m.SetVertices(meshVerts.ToArray());
        m.SetNormals(meshNormals.ToArray());
        m.SetUVs(0, meshUVs);
        m.SetIndices(meshIndices.ToArray(), MeshTopology.Lines, 0);
        return m;
        
    }

}
