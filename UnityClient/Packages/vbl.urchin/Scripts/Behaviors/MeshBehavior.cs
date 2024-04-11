using BrainAtlas;
using UnityEngine;
using UnityEngine.UIElements;
using Urchin.API;

public class MeshBehavior : MonoBehaviour
{
    #region Properties
    public MeshModel Data;
    public MeshRenderer Renderer { get { return GetComponent<MeshRenderer>(); } }
    #endregion

    #region Unity

    private void OnMouseEnter()
    {
        if (Data.Interactive)
        {
            transform.localScale = Data.Scale * 4f;
            Renderer.material.color = Color.red;
        }
    }

    private void OnMouseExit()
    {
        if (Data.Interactive)
        {
            transform.localScale = Data.Scale;
            Renderer.material.color = Data.Color;
        }
    }

    private void OnMouseDown()
    {
        if (Data.Interactive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // left mouse click
                Client_SocketIO.Emit("NeuronCallback", Data.ID);
            }
        }
    }
    #endregion

    #region Settings
    public void UpdateAll()
    {
        // Update all settings
        _SetPosition();
        _SetScale();
        _SetMaterial();
        _SetColor();
    }

    public void SetPosition(Vector3 coordAtlasU)
    {
        Data.Position = coordAtlasU;
        _SetPosition();
    }

    private void _SetPosition()
    {
        // Set position expects coordinates in raw AP/ML/DV coordinates, not reference
        transform.localPosition = BrainAtlasManager.ActiveReferenceAtlas.Atlas2World(Data.Position, false);
    }

    public void SetScale(Vector3 scale)
    {
        Data.Scale = scale;
        _SetScale();
    }

    private void _SetScale()
    {
        transform.localScale = Data.Scale;
    }

    public void SetColor(Color color)
    {
        Data.Color = color;
        _SetColor();
    }

    private void _SetColor()
    {
        Renderer.material.color = Data.Color;
    }

    public void SetMaterial(string materialName)
    {
        Data.Material = materialName;
        _SetMaterial();
    }

    private void _SetMaterial()
    {
        Renderer.material = MaterialManager.GetMaterial(Data.Material);
    }

    public void SetMesh(Mesh mesh)
    {
        GetComponent<MeshFilter>().mesh = mesh;
    }
    #endregion
}
