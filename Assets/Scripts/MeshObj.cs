using System;
using UnityEngine;

public class MeshObj : MonoBehaviour
{
    [NonSerialized] 
    public MeshReader.MeshData M_MeshData;

    private MeshRenderer mRenderer;
    private MeshCollider mCollider;

    public void Initialize(MeshReader.MeshData inputMeshData)
    {
        M_MeshData = inputMeshData;
        mRenderer = GetComponent<MeshRenderer>();
        mCollider = GetComponent<MeshCollider>();
    }
    public void Show()
    {
        M_MeshData.isHide = false;
        mRenderer.enabled = true;
        mCollider.enabled = true;
    }
    
    public void Hide()
    {
        M_MeshData.isHide = true;
        mRenderer.enabled = false;
        mCollider.enabled = false;
    }
}
