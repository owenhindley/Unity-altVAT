using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AltVATCreateTexturesFromCSV : EditorWindow
{
    private TextAsset sourceText;
    private int textureSize = 512;
    private int textureDepth = 512;
    
    
    [MenuItem("Window/[AltVAT] Create Mesh+Textures From CSV")]
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

        textureSize = (int) EditorGUI.IntField(new Rect(3, 50, position.width - 6, 20), "Texture Size", textureSize);
        textureDepth = (int) EditorGUI.IntField(new Rect(3, 70, position.width - 6, 20), "Texture Depth", textureDepth);

        if (sourceText)
        {
            if (GUI.Button(new Rect(3, 95, position.width - 6, 20), "Create Mesh and Textures"))
            {
                var frames = ExtractFramesFromCSV(sourceText.text, sourceText.name);

                // create mesh
                var newMesh = CreateStartingMesh(frames.meshFrames[0], textureSize);

                var sourcePath = AssetDatabase.GetAssetPath(sourceText);
                var targetPath = sourcePath.Replace("csv", "_mesh.asset");
                
                SaveAsset(newMesh, targetPath);
                
                // create textures
                
                // positions
                var diffInfo = CreateNormalizedDiffList(frames.meshFrames[0].verts,
                    frames.meshFrames.Select(info => info.verts).ToList());

                var positionTex = CreateTextureFromDiffs(diffInfo.diffs, textureSize, textureDepth);

                targetPath = targetPath.Replace("_mesh", "_posTexture");
                SaveAsset(positionTex, targetPath);
                
                // normals
                diffInfo = CreateNormalizedDiffList(frames.meshFrames[0].normals,
                    frames.meshFrames.Select(info => info.verts).ToList());

                var normalsTex = CreateTextureFromDiffs(diffInfo.diffs, textureSize, textureDepth);

                targetPath = targetPath.Replace("_posTexture", "_normalsTexture");
                SaveAsset(normalsTex, targetPath);
                
                // create material
                var material = new Material(Shader.Find("altVAT/altVAT_UnlitShader"));
                material.SetTexture("_PositionsTex", positionTex);
                material.SetTexture("_NormalsTex", normalsTex);
                material.SetFloat("_FrameCount", frames.meshFrames.Count);
                material.SetVector("_BoundsMin", diffInfo.minBounds);
                material.SetVector("_BoundsMax", diffInfo.maxBounds);
                
                SaveAsset(material, targetPath.Replace("_normalsTexture.asset", "_material.mat"));

            }
            
        }
    }

    private void SaveAsset(UnityEngine.Object obj, string path)
    {
        var existingObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (existingObject == null)
        {
            Debug.Log("Creating new asset");
            AssetDatabase.CreateAsset(obj,path);
        }
        else
        {
            Debug.Log("Deleting old asset");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj,path);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
    }

    private class AnimInfo
    {
        public int numFrames;
        public int numVerts;
        public Vector3 boundsMax;
        public Vector3 boundsMin;

    }

    private class MeshInfo
    {
        public MeshInfo()
        {
            verts = new List<Vector3>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
        }
        public List<Vector3> verts = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
    }
    
    private class MeshFrameList
    {
        public List<MeshInfo> meshFrames = new List<MeshInfo>();

    }


    

    private MeshFrameList ExtractFramesFromCSV(string csv, string name)
    {
        // CSV format
        // f
        // p
        // v,(x),(y),(z),
        // ..
        // n,(x),(y),(z),
        // ..
        // uv,(x),(y),(z),
        // p
        // and so on
        
        Debug.Log("converting " + name);
       
        
        var rows = csv.Split('\n');
        MeshFrameList frameList = new MeshFrameList();
        MeshInfo currentMesh = null;
        foreach (var row in rows)
        {
            var r = row.Replace(" ", string.Empty);
            r = row.Replace("\r", string.Empty);
            var cols = r.Split(',');
            
            var start = cols[0];
            switch (start)
            {
                case "f":
                    // new frame
                    if (currentMesh != null) frameList.meshFrames.Add(currentMesh);
                    currentMesh = new MeshInfo();
                    break;
                case "p":
                    // ignore primitives, everything just gets added
                    // // start new primitive
                    // if (currentMesh != null) { prims.Add(currentMesh); }
                    // currentMesh = new MeshInfo();
                    break;
                case "v":
                    // add vert
                    var p_x = float.Parse(cols[1]);
                    var p_y = float.Parse(cols[2]);
                    var p_z = float.Parse(cols[3]);
                    currentMesh.verts.Add(new Vector3(p_x, p_y, p_z));
                    break;
                case "n":
                    // add normal
                    var n_x = float.Parse(cols[1]);
                    var n_y = float.Parse(cols[2]);
                    var n_z = float.Parse(cols[3]);
                    currentMesh.normals.Add(new Vector3(n_x, n_y, n_z));
                    break;
                case "uv":
                    // add uv
                    var uv_x = float.Parse(cols[1]);
                    var uv_y = float.Parse(cols[2]);
                    currentMesh.uvs.Add(new Vector2(uv_x, uv_y));
                    break;
                default:

                    break;
            }
        }

        return frameList;

    }

    private (List<List<Vector3>> diffs, Vector3 maxBounds, Vector3 minBounds) CreateNormalizedDiffList(List<Vector3> origin, List<List<Vector3>> frames)
    {
        Vector3 maxBounds = Vector3.zero;
        Vector3 minBounds = Vector3.zero;
        
        if (origin.Count != frames[0].Count) throw new Exception("ERROR - vector length mismatch");
        var diffs = new List<List<Vector3>>();
        foreach (var frame in frames)
        {
            var frameDiff = new List<Vector3>();
            for (int i = 0; i < frame.Count; i++)
            {
                Vector3 diff = frame[i] - origin[i];
                frameDiff.Add(diff);
                // store bounds
                maxBounds.x = Mathf.Max(diff.x, maxBounds.x);
                minBounds.x = Mathf.Min(diff.x, minBounds.x);
                
                maxBounds.y = Mathf.Max(diff.y, maxBounds.y);
                minBounds.y = Mathf.Min(diff.y, minBounds.y);
                
                maxBounds.x = Mathf.Max(diff.z, maxBounds.z);
                minBounds.x = Mathf.Min(diff.z, minBounds.z);
            }
            diffs.Add(frameDiff);
        }
        
        // normalise diffs
        foreach (var frame in diffs)
        {
            for (int i = 0; i < frame.Count; i++)
            {
                frame[i] = new Vector3(
                    Mathf.InverseLerp(minBounds.x, maxBounds.x, frame[i].x),
                    Mathf.InverseLerp(minBounds.y, maxBounds.y, frame[i].y),
                    Mathf.InverseLerp(minBounds.z, maxBounds.z, frame[i].z));

            }
        }
    
        // return everything
        return (diffs, maxBounds, minBounds);
    }


    private Texture3D CreateTextureFromDiffs(List<List<Vector3>> diffs, int size, int depth)
    {
        if (diffs.Count > depth) throw new Exception("ERROR - number of frames exceeds 3d texture depth");
        var t = new Texture3D(size, size, depth, TextureFormat.RGB24, false);
        t.filterMode = FilterMode.Point;
        for (int frame = 0; frame < diffs.Count; frame++)
        {
            for (int vertex = 0; vertex < diffs[frame].Count; vertex++)
            {
                int pX = vertex % size;
                int pY = Mathf.FloorToInt(vertex / size);
                int pZ = frame;
                var v = diffs[frame][vertex];
                var c = new Color(v.x, v.y, v.z);
                t.SetPixel(pX, pY, pZ, c);
            }
        }
        t.Apply(false);
        
        return t;
    }
    
    private Mesh CreateStartingMesh(MeshInfo l, int uvTextureSize)
    {
        if (l.verts.Count > (uvTextureSize * uvTextureSize))
            throw new Exception("ERROR - texture too small for this many verts");
        
        var m = new Mesh();
        m.vertices = l.verts.ToArray();
        m.normals = l.normals.ToArray();
        
        m.uv = l.uvs.ToArray();
        
        // create UV2 lookup
        m.uv2 = Enumerable.Range(0, l.verts.Count).ToList().ConvertAll(input => GetUVForIndex(input, uvTextureSize)).ToArray();


        // calculate indices
        if (l.verts.Count % 3 != 0) throw new Exception("ERROR - verts not divisible by 3");
        var indices = Enumerable.Range(0, l.verts.Count).ToArray();
       
        m.SetIndices(indices, MeshTopology.Triangles, 0);

        return m;
    }

    private Vector2 GetUVForIndex(int index, int textureSizePixels)
    {
        float uvX = (float) (index % textureSizePixels) / (float) textureSizePixels;
        float uvY = ((float)index / (float)textureSizePixels) / (float) textureSizePixels;
        return new Vector2(uvX, uvY);
    }
    
    // private Vector2 GetUVForIndex(int index, int numVerts)
    // {
    //     int uvWidth = Mathf.CeilToInt(Mathf.Sqrt(numVerts));
    //     return new Vector2((float)index % uvWidth, Mathf.Floor((float)uvWidth / (float)index));
    // }

}
