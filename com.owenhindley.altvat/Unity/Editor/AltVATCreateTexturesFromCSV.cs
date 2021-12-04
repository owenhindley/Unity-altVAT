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

                Debug.Log("Extracted " + frames.meshFrames.Count + " frames");
                Debug.Log("Num verts : " + frames.meshFrames[0].verts.Count);

                if (textureDepth < frames.meshFrames.Count)
                {
                    Debug.LogError("ERROR - number of frames exceeds texture depth, frames will be dropped");
                    int diff = frames.meshFrames.Count - textureDepth;
                    frames.meshFrames.RemoveRange(frames.meshFrames.Count - diff, diff);
                }

                if (frames.meshFrames[0].verts.Count > Mathf.Pow(textureSize, 2))
                {
                    throw new Exception("ERROR - texture size " + textureSize +
                                        " (squared) is smaller than the number of vertices, cannot continue");
                }
                
                var sourcePath = AssetDatabase.GetAssetPath(sourceText);

                // create textures
                
                // positions
                var diffInfoPos = CreateNormalizedDiffList(frames.meshFrames[0].verts,
                    frames.meshFrames.Select(info => info.verts).ToList());

                Debug.Log("Diff info pos : ");
                Debug.Log("Max bounds pos:");
                Debug.Log(diffInfoPos.maxBounds);
                Debug.Log("Min bounds pos:");
                Debug.Log(diffInfoPos.minBounds);

                var positionTex = CreateTextureFromDiffs(diffInfoPos.diffs, textureSize, textureDepth);

                var targetPath = sourcePath.Replace(".csv", "_posTexture.asset");
                SaveAsset(positionTex, targetPath);
                
                // normals
                var diffInfoNorm = CreateNormalizedDiffList(frames.meshFrames[0].normals,
                    frames.meshFrames.Select(info => info.normals).ToList());
                
                Debug.Log("Diff info normals : ");
                Debug.Log("Max bounds normals:");
                Debug.Log(diffInfoNorm.maxBounds);
                Debug.Log("Min bounds normals:");
                Debug.Log(diffInfoNorm.minBounds);

                var normalsTex = CreateTextureFromDiffs(diffInfoNorm.diffs, textureSize, textureDepth);

                targetPath = sourcePath.Replace(".csv", "_normalsTexture.asset");
                SaveAsset(normalsTex, targetPath);
                
                /// create mesh
                var newMesh = CreateStartingMesh(frames.meshFrames[0], textureSize);
                
                // extend bounds
                newMesh.RecalculateBounds();
                
                targetPath = sourcePath.Replace(".csv", "_mesh.asset");
                
                SaveAsset(newMesh, targetPath);
                
                // create material
                var material = new Material(Shader.Find("altVAT/altVAT_SimpleDirectionalLitShader"));
                material.SetTexture("_PositionsTex", positionTex);
                material.SetTexture("_NormalsTex", normalsTex);
                material.SetFloat("_FrameCount", frames.meshFrames.Count);
                material.SetVector("_BoundsMinPos", diffInfoPos.minBounds);
                material.SetVector("_BoundsMaxPos", diffInfoPos.maxBounds);
                material.SetVector("_BoundsMinNorm", diffInfoNorm.minBounds);
                material.SetVector("_BoundsMaxNorm", diffInfoNorm.maxBounds);
                
                // search for light
                if (GameObject.FindObjectOfType<Light>() != null)
                {
                    var l = GameObject.FindObjectOfType<Light>();
                    var lightDir = l.transform.TransformDirection(Vector3.forward);
                    material.SetVector("_LightDirection", lightDir);

                }
                
                SaveAsset(material, sourcePath.Replace(".csv", "_material.mat"));
                
                // create prefab
                GameObject animPrefab = new GameObject();
                animPrefab.name = sourceText.name + "_prefab";
                var mf = animPrefab.AddComponent<MeshFilter>();
                mf.mesh = newMesh;
                var mr = animPrefab.AddComponent<MeshRenderer>();
                mr.material = material;

                var prefabPath = sourcePath.Replace(".csv", ".prefab");
                try
                {
                    AssetDatabase.DeleteAsset(prefabPath);
                }
                catch (Exception ex)
                {
                }

                PrefabUtility.SaveAsPrefabAsset(animPrefab, prefabPath);
                
                // remove from heirarchy 
                DestroyImmediate(animPrefab);
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
                    currentMesh.verts.Add(new Vector3(-p_x, p_y, p_z));
                    break;
                case "n":
                    // add normal
                    var n_x = float.Parse(cols[1]);
                    var n_y = float.Parse(cols[2]);
                    var n_z = float.Parse(cols[3]);
                    currentMesh.normals.Add(Vector3.Normalize(new Vector3(-n_x, n_y, n_z)));
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
                
                maxBounds.z = Mathf.Max(diff.z, maxBounds.z);
                minBounds.z = Mathf.Min(diff.z, minBounds.z);
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
                    Mathf.InverseLerp(minBounds.z, maxBounds.z, frame[i].z)
                    );

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
