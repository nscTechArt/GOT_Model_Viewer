using System.Collections.Generic;
using UnityEngine;
using System.IO;
// ReSharper disable InconsistentNaming

public class MeshReader
{
    // singleton
    // ---------
    public static MeshReader Instance = new();

    // ---------------------------------------------------
    // ---------------------Core APIs---------------------
    // ---------------------------------------------------
    
    public List<MeshData> ReadMesh(string path)
    {
        BinaryReader br;
        try
        {
            br = new BinaryReader(new FileStream(path,
                FileMode.Open));
        }
        catch (IOException e)
        {
            Debug.LogError("Load Error " + e.Message);
            return null;
        }

        List<MeshData> meshs = new List<MeshData>();
        MeshData meshData = null;
        try
        {
            bool isCreatingMesh = false;
            while (true)
            {
                if (meshData == null)
                {
                    ushort triIndex = br.ReadUInt16();
                    if (triIndex == 0 && !isCreatingMesh)
                    {
                        long triangleStartIndex = br.BaseStream.Position - 2;
                        ushort nextTriIndex = br.ReadUInt16();
                        if (nextTriIndex == 1)
                        {
                            short thirdTriIndex = br.ReadInt16();
                            if (thirdTriIndex == 2)
                            {
                                isCreatingMesh = true;
                                meshData = new MeshData();
                                meshData.triangles = new List<int>();
                                meshData.triangles.Add(triIndex);
                                meshData.triangles.Add(nextTriIndex);
                                meshData.triangles.Add(thirdTriIndex);
                                meshData.maxTriangleIndex = 2;
                                meshData.readType = MeshData.ReadType.Triangles;
                                meshData.TriangleStartIndex = (int)triangleStartIndex;
                                continue;
                            }
                            else
                            {
                                br.BaseStream.Position -= 5;
                            }
                        }
                        else
                        {
                            br.BaseStream.Position -= 3;
                        }

                        triangleStartIndex = -1;
                    }
                    else
                    {
                        br.BaseStream.Position -= 1;
                    }
                }
                else
                {
                    if (meshData.readType == MeshData.ReadType.Triangles)
                    {
                        ushort newTriandleIndex0 = br.ReadUInt16();
                        ushort newTriandleIndex1 = br.ReadUInt16();
                        ushort newTriandleIndex2 = br.ReadUInt16();
                        if (!CheckTriangleEnd(newTriandleIndex0, newTriandleIndex1, newTriandleIndex2,
                                meshData.maxTriangleIndex))
                        {
                            if (newTriandleIndex0 > meshData.maxTriangleIndex)
                            {
                                meshData.maxTriangleIndex = newTriandleIndex0;
                            }

                            if (newTriandleIndex1 > meshData.maxTriangleIndex)
                            {
                                meshData.maxTriangleIndex = newTriandleIndex1;
                            }

                            if (newTriandleIndex2 > meshData.maxTriangleIndex)
                            {
                                meshData.maxTriangleIndex = newTriandleIndex2;
                            }

                            meshData.triangles.Add(newTriandleIndex0);
                            meshData.triangles.Add(newTriandleIndex1);
                            meshData.triangles.Add(newTriandleIndex2);
                            continue;
                        }
                        else
                        {
                            meshData.readType = MeshData.ReadType.Vertices;
                            short verticleX = 0;
                            short verticleY = 0;
                            short verticleZ = 0;
                            if (newTriandleIndex0 == 0)
                            {
                                //sometimes has a 0x00 between triangles and verticles data 
                                verticleX = (short)newTriandleIndex1;
                                verticleY = (short)newTriandleIndex2;
                                verticleZ = br.ReadInt16();
                                meshData.triangleToVertexSkipByte = 1;
                            }
                            else
                            {
                                verticleX = (short)newTriandleIndex0;
                                verticleY = (short)newTriandleIndex1;
                                verticleZ = (short)newTriandleIndex2;
                            }

                            float fX = (verticleX * Mathf.Pow(2, -8));
                            float fY = (verticleY * Mathf.Pow(2, -8));
                            float fZ = (verticleZ * Mathf.Pow(2, -8));

                            Vector3 verticle = new Vector3(fX, fY, fZ);
                            meshData.vertices = new Vector3[meshData.maxTriangleIndex + 1];
                            meshData.normals = new Vector3[meshData.maxTriangleIndex + 1];
                            meshData.uvs = new List<Vector2>();
                            meshData.vertices[0] = verticle;

                            for (int i = 0; i < MeshData.vertexPadding; i++)
                            {
                                br.ReadByte();
                            }

                            continue;
                        }
                    }
                    else if (meshData.readType == MeshData.ReadType.Vertices)
                    {
                        for (int i = 1; i < meshData.vertices.Length; i++)
                        {
                            short verticleX = br.ReadInt16();
                            short verticleY = br.ReadInt16();
                            short verticleZ = br.ReadInt16();
                            float fX = (verticleX * Mathf.Pow(2, -8));
                            float fY = (verticleY * Mathf.Pow(2, -8));
                            float fZ = (verticleZ * Mathf.Pow(2, -8));
                            Vector3 verticle = new Vector3(fX, fY, fZ);
                            meshData.vertices[i] = verticle;
                            for (int j = 0; j < MeshData.vertexPadding; j++)
                            {
                                br.ReadByte();
                            }
                        }

                        meshData.readType = MeshData.ReadType.Normals;
                        continue;
                    }
                    else if (meshData.readType == MeshData.ReadType.Normals)
                    {
                        for (int i = 0; i < meshData.normals.Length; i++)
                        {
                            short normalX = br.ReadInt16();
                            short normalY = br.ReadInt16();
                            short normalZ = br.ReadInt16();
                            float fX = (normalX * Mathf.Pow(2, -8));
                            float fY = (normalY * Mathf.Pow(2, -8));
                            float fZ = (normalZ * Mathf.Pow(2, -8));
                            Vector3 normal = new Vector3(fX, fY, fZ);
                            meshData.normals[i] = normal;
                            for (int j = 0; j < MeshData.normalPadding; j++)
                            {
                                br.ReadByte();
                            }
                        }

                        meshData.readType = MeshData.ReadType.UVs;
                        continue;
                    }
                    else if (meshData.readType == MeshData.ReadType.UVs)
                    {
                        ushort readFirst = br.ReadUInt16();

                        if (readFirst == 0)
                        {
                            ushort second = br.ReadUInt16();
                            if (second == 1)
                            {
                                ushort third = br.ReadUInt16();
                                if (third == 2)
                                {
                                    meshData.readType = MeshData.ReadType.Triangles;

                                    meshs.Add(meshData);
                                    isCreatingMesh = false;
                                    meshData = null;
                                    
                                    br.BaseStream.Position -= 6;
                                    continue;
                                }
                                else
                                {
                                    br.BaseStream.Position -= 6;
                                }
                            }
                            else
                            {
                                br.BaseStream.Position -= 4;
                            }
                        }
                        else
                        {
                            br.BaseStream.Position -= 2;
                        }
                        
                        float uvX = Mathf.HalfToFloat(br.ReadUInt16());
                        float uvY = Mathf.HalfToFloat(br.ReadUInt16());
                        Vector2 uv = new Vector2(uvX, uvY);
                        meshData.uvs.Add(uv);
                        continue;
                    }
                }
            }
        }
        catch (IOException e)
        {
            Debug.Log("Read End ");
        }
        finally
        {
            br.Close();
            br.Dispose();
        }

        if (meshData != null)
        {
            meshs.Add(meshData);
        }
        
        
        return meshs;
    }
    
    public void SaveMesh(string path, string fileName, string orinalFileFullPath, List<MeshData> data)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(orinalFileFullPath);
        foreach (MeshData item in data)
        {
            if (item.isHide)
            {
                int startIndex = item.TriangleStartIndex;
                int hideCount = CalculateHideCount(item);
                for (int i = startIndex; i < startIndex + hideCount; i++)
                {
                    bytes[i] = 0;
                }
            }
        }
        
        System.IO.File.WriteAllBytes(path + "\\" + fileName, bytes);
    }
    
    // ---------------------------------------------------
    // ----------------Helper Functions-------------------
    // ---------------------------------------------------
    private bool CheckIsTriangle(BinaryReader br)
    {
        ushort triIndex = br.ReadUInt16();
        if (triIndex == 0)
        {
            ushort nextTriIndex = br.ReadUInt16();
            if (nextTriIndex == 1)
            {
                short thirdTriIndex = br.ReadInt16();
                if (thirdTriIndex == 2)
                {
                    return true;
                    // meshData.TriangleStartIndex = readIndex;
                    // continue;
                }
                else
                {
                    br.BaseStream.Position -= 5;
                }
            }
            else
            {
                br.BaseStream.Position -= 3;
            }
        }
        else
        {
            br.BaseStream.Position -= 1;
        }

        return false;
    }
    
    private bool CheckTriangleEnd(ushort index0, ushort index1, ushort index2, int maxIndex)
    {
        int max = maxIndex + 3;
        if (index0 > max || index1 > max || index2 > max)
        {
            return true;
        }

        return false;
    }
    
    private static int CalculateHideCount(MeshData data)
    {
        int triangleByteCount = data.triangles.Count * 2 + data.triangleToVertexSkipByte;
        int vertexByteCount = data.vertices.Length * 3 * 2 + data.vertices.Length * MeshData.vertexPadding;
        int normalByteCount = data.normals.Length * 3 * 2 + data.normals.Length * MeshData.normalPadding;
        int uvByteCount = data.uvs.Count * 2 * 2;

        return triangleByteCount + vertexByteCount + normalByteCount + uvByteCount;
        // var verticalByteCount
    }
    
    // ---------------------------------------------------
    // ------------------Data Structures------------------
    // ---------------------------------------------------
    
    // MeshData class represents one sub-mesh extracted from the binary file
    // ---------------------------------------------------------------------
    public class MeshData
    {
        // state machine for parsing phases
        // --------------------------------
        public enum ReadType
        {
            None,
            Triangles,
            Vertices,
            Normals,
            UVs,
        }
        
        // parsed mesh data
        // ----------------
        public List<int>     triangles;
        public Vector3[]     vertices;
        public Vector3[]     normals;
        public List<Vector2> uvs;
        
        // parsing meta data
        // -----------------
        public int maxTriangleIndex = -1; // highest vertex index referenced by triangles
        public ReadType readType = ReadType.None; // current parsing phase
        public int TriangleStartIndex = -1; // file position of mesh start (for saving)
        
        
        // format config
        // -------------
        public const int vertexPadding = 2; // padding bytes after each vertex
        public const int normalPadding = 2; // padding bytes after each normal
        public int triangleToVertexSkipByte;
        
        // editor state
        // ------------
        public bool isHide = false;
    }
}
