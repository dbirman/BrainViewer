using BrainAtlas;
using System;
using UnityEngine;
using Urchin.Utils;
using System.IO;
using BestHTTP.Decompression.Zlib;
using System.Collections.Generic;

public class VolumeRenderer : MonoBehaviour
{
    public Texture3D _volumeTexture;

    [SerializeField] GameObject _cubeGO;
    [SerializeField] Transform _sliceParentT;
    [SerializeField] GameObject _coronalSliceFGO;
    [SerializeField] GameObject _coronalSliceBGO;
    [SerializeField] GameObject _sagittalSliceFGO;
    [SerializeField] GameObject _sagittalSliceBGO;

    private Material _volumeRayMarchMaterial;
    private Material _coronalMaterialF;
    private Material _sagittalMaterialF;
    private Material _coronalMaterialB;
    private Material _sagittalMaterialB;
    private Vector3[] _coronalOrigWorldU;
    private Vector3[] _sagittalOrigWorldU;
    private bool _slicesActive;
    private int[] _backOrder = { 1, 0, 3, 2 };

    private Vector3 _prevSlicePosition;
    public Vector3 _slicePosition;

    private string _compressedString;
    private int _compressedStringLength;
    private Color[] _colormap;

    private int width;
    private int height;
    private int depth;

    #region Clicks
    private List<Vector3> _clicks;
    #endregion

    private void Awake()
    {
        _colormap = new Color[256];
        for (int i = 0; i < 255; i++)
            _colormap[i] = Color.black;
        _colormap[255] = new Color(0f, 0f, 0f, 0f);

        _coronalMaterialF = _coronalSliceFGO.GetComponent<Renderer>().material;
        _sagittalMaterialF = _sagittalSliceFGO.GetComponent<Renderer>().material;
        _coronalMaterialB = _coronalSliceBGO.GetComponent<Renderer>().material;
        _sagittalMaterialB = _sagittalSliceBGO.GetComponent<Renderer>().material;

        Vector3 dims = BrainAtlasManager.ActiveReferenceAtlas.Dimensions;
        Vector3 dimsWorld = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World_Vector(dims);
        _sliceParentT.localScale = new Vector3(Mathf.Abs(dimsWorld.x), Mathf.Abs(dimsWorld.y), Mathf.Abs(dimsWorld.z));
        _cubeGO.transform.localScale = dims;

        (width, height, depth) = BrainAtlasManager.ActiveReferenceAtlas.DimensionsIdx;

        _volumeTexture = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);
        _volumeTexture.filterMode = FilterMode.Point;
        _volumeTexture.wrapMode = TextureWrapMode.Clamp;

        _volumeRayMarchMaterial = _cubeGO.GetComponent<Renderer>().material;
        _volumeRayMarchMaterial.mainTexture = _volumeTexture;
        _coronalMaterialF.SetTexture("_Volume", _volumeTexture);
        _sagittalMaterialF.SetTexture("_Volume", _volumeTexture);
        _coronalMaterialB.SetTexture("_Volume", _volumeTexture);
        _sagittalMaterialB.SetTexture("_Volume", _volumeTexture);
        _volumeTexture.Apply();

        // x = ml
        // y = dv
        // z = ap

        // (dims.y, dims.z, dims.x)

        // vertex order -x-y, +x-y, -x+y, +x+y

        // -x+y, +x+y, +x-y, -x-y
        _sagittalOrigWorldU = new Vector3[] {
                new Vector3(0f, -0.5f, -0.5f),
                new Vector3(0f, -0.5f, 0.5f),
                new Vector3(0f, 0.5f, -0.5f),
                new Vector3(0f, 0.5f, 0.5f)
            };

        // -z+y, +z+y, +z-y, -z-y
        _coronalOrigWorldU = new Vector3[] {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
    }

    private void Start()
    {
        SetSliceVisibility(false);
    }

    private void Update()
    {
        if (_slicePosition != _prevSlicePosition)
        {
            _prevSlicePosition = _slicePosition;
            UpdateSlicePosition();
        }
    }

    public void Apply()
    {
#if UNITY_EDITOR
        Debug.Log("(VolumeRenderer) Applying texture");
#endif

        byte[] compressedData = Convert.FromBase64String(_compressedString);

        using (MemoryStream compressedStream = new MemoryStream(compressedData))
        using (MemoryStream decompressedStream = new MemoryStream())
        using (DeflateStream decompressor = new DeflateStream(compressedStream, CompressionMode.Decompress))
        {
            // Drop the zlib header, which is not read by .NET
            compressedStream.Seek(2, SeekOrigin.Begin);
            decompressor.CopyTo(decompressedStream);

            decompressedStream.Position = 0;

            using (BinaryReader reader = new BinaryReader(decompressedStream))
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    for (int z = 0; z < depth; z++)
                        _volumeTexture.SetPixel(x, y, z, _colormap[reader.ReadByte()]);
        }

        _volumeTexture.Apply();
        }

    public void SetColormap(Color[] colors)
    {
        Debug.Log("(UM_VolRend) Creating new colormap for: " + name);
        for (int i = 0; i < 255; i++)
        {
            if (i < colors.Length)
                _colormap[i] = colors[i];
            else
                _colormap[i] = Color.black;
        }
    }

    public void SetVolumeVisibility(bool visible)
    {
        Debug.Log("(UM_VolRend) Volume: " + name + " is now visible: " + visible);
        _cubeGO.GetComponent<Renderer>().enabled = visible;
    }

    private int _nextOffset;

    public void SetMetadata(int nCompressedBytes)
    {
        _compressedString = "";
        _compressedStringLength = nCompressedBytes;
        _nextOffset = 0;

        Debug.Log($"Waiting to receive {_compressedStringLength} bytes");
    }

    public void SetData(string newData)
    {
        Debug.Log($"Received {newData.Length} bytes putting in offset {_nextOffset}");
        //Buffer.BlockCopy(newData, 0, _compressedData, _nextOffset, newData.Length);
        //_nextOffset += newData.Length;

        _compressedString += newData;

        Debug.Log((_compressedString.Length, _compressedStringLength));
        if (_compressedString.Length == _compressedStringLength)
            Apply();
    }

    public void SetSliceVisibility(bool visible)
    {
        _slicesActive = visible;

        _coronalSliceFGO.SetActive(_slicesActive);
        _sagittalSliceFGO.SetActive(_slicesActive);
        _coronalSliceBGO.SetActive(_slicesActive);
        _sagittalSliceBGO.SetActive(_slicesActive);

        UpdateVolumeSlicing();
    }

    /// <summary>
    /// Shift the position of the sagittal and coronal slices to match the tip of the active probe
    /// </summary>
    public void UpdateSlicePosition()
    {
        // vertex order -x-y, +x-y, -x+y, +x+y

        // compute the world vertex positions from the raw coordinates
        // then get the four corners, and warp these according to the active warp
        Vector3[] newCoronalVerts = new Vector3[4];
        Vector3[] newSagittalVerts = new Vector3[4];
        for (int i = 0; i < _coronalOrigWorldU.Length; i++)
        {
            newCoronalVerts[i] = new Vector3(_coronalOrigWorldU[i].x, _coronalOrigWorldU[i].y, -_slicePosition.x);
            newSagittalVerts[i] = new Vector3(_slicePosition.y, _sagittalOrigWorldU[i].y, _sagittalOrigWorldU[i].z);
        }
        Vector3[] newCoronalBack = new Vector3[4];
        Vector3[] newSagittalBack = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            newCoronalBack[i] = newCoronalVerts[_backOrder[i]];
            newSagittalBack[i] = newSagittalVerts[_backOrder[i]];
        }

        _coronalSliceFGO.GetComponent<MeshFilter>().mesh.vertices = newCoronalVerts;
        _coronalSliceFGO.GetComponent<MeshCollider>().sharedMesh = _coronalSliceFGO.GetComponent<MeshFilter>().mesh;
        _coronalSliceBGO.GetComponent<MeshFilter>().mesh.vertices = newCoronalBack;
        _coronalSliceBGO.GetComponent<MeshCollider>().sharedMesh = _coronalSliceBGO.GetComponent<MeshFilter>().mesh;

        _sagittalSliceFGO.GetComponent<MeshFilter>().mesh.vertices = newSagittalVerts;
        _sagittalSliceFGO.GetComponent<MeshCollider>().sharedMesh = _sagittalSliceFGO.GetComponent<MeshFilter>().mesh;
        _sagittalSliceBGO.GetComponent<MeshFilter>().mesh.vertices = newSagittalBack;
        _sagittalSliceBGO.GetComponent<MeshCollider>().sharedMesh = _sagittalSliceBGO.GetComponent<MeshFilter>().mesh;

        _coronalMaterialF.SetFloat("_SlicePercentage", _slicePosition.x + 0.5f);
        _sagittalMaterialF.SetFloat("_SlicePercentage", _slicePosition.y + 0.5f);
        _coronalMaterialB.SetFloat("_SlicePercentage", _slicePosition.x + 0.5f);
        _sagittalMaterialB.SetFloat("_SlicePercentage", _slicePosition.y + 0.5f);

        UpdateVolumeSlicing();
    }

    private void UpdateVolumeSlicing()
    {
        float min = -0.5f;
        float max = 0.5f;
        if (_slicesActive)
        {

            // camYBack means the camera is looking from the back

            // the clipping goes from -0.5 to 0.5 on each dimension
            // _slicePosition goes from -0.5 to 0.5 on each dimension
            if (camYBack)
                // if we're looking from the back, we want to show the brain in the front
                _cubeGO.GetComponent<Renderer>().material.SetVector("_APClip", new Vector2(min, _slicePosition.x));
            else
                _cubeGO.GetComponent<Renderer>().material.SetVector("_APClip", new Vector2(_slicePosition.x, max));

            if (camXLeft)
                // clip from mlPosition forward
                _cubeGO.GetComponent<Renderer>().material.SetVector("_MLClip", new Vector2(_slicePosition.y, max));
            else
                _cubeGO.GetComponent<Renderer>().material.SetVector("_MLClip", new Vector2(min, _slicePosition.y));
        }
        else
        {
            _cubeGO.GetComponent<Renderer>().material.SetVector("_APClip", new Vector2(min, max));
            _cubeGO.GetComponent<Renderer>().material.SetVector("_MLClip", new Vector2(min, max));
        }
    }

    public void SetRayMarchAlpha(float alpha)
    {
        _volumeRayMarchMaterial.SetFloat("_Alpha", alpha);
    }

    public void SetRayMarchStepSize(float stepSize)
    {
        _volumeRayMarchMaterial.SetFloat("_StepSize", stepSize);
    }

    private bool camXLeft;
    private bool camYBack;
    public void UpdateCameraPosition()
    {
        Vector3 camPosition = Camera.main.transform.position;
        bool changed = false;
        if (!camXLeft && camPosition.x < 0)
        {
            camXLeft = true;
            changed = true;
        }
        else if (camXLeft && camPosition.x > 0)
        {
            camXLeft = false;
            changed = true;
        }
        else if (!camYBack && camPosition.z < 0)
        {
            camYBack = true;
            changed = true;
        }
        else if (camYBack && camPosition.z > 0)
        {
            camYBack = false;
            changed = true;
        }
        if (changed)
            UpdateVolumeSlicing();
    }
}
