using UnityEngine;
using System;

[Serializable]
public struct AtlasModel
{
    public string Name;
    public Vector3 ReferenceCoord;
    public StructureModel[] Areas;
    public ColormapModel Colormap;

    public AtlasModel(string name, Vector3 referenceCoord, StructureModel[] areas, ColormapModel colormap)
    {
        Name = name;
        ReferenceCoord = referenceCoord;
        Areas = areas;
        Colormap = colormap;
    }
}

using System;

[Serializable]
public struct CameraModel
{
    public float Id;
    public string Type;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Target;
    public float Zoom;
    public Vector2 Pan;
    public CameraMode Mode;
    public bool Controllable;
    public bool Main;

    public CameraModel(float id, string type, Vector3 position, Vector3 rotation, Vector3 target, float zoom, Vector2 pan, CameraMode mode, bool controllable, bool main)
    {
        Id = id;
        Type = type;
        Position = position;
        Rotation = rotation;
        Target = target;
        Zoom = zoom;
        Pan = pan;
        Mode = mode;
        Controllable = controllable;
        Main = main;
    }
}


public enum CameraMode
{
    orthographic = 0,
    perspective = 1,
}

using System;

[Serializable]
public struct CameraRotationModel
{
    public Vector3 StartRotation;
    public Vector3 EndRotation;

    public CameraRotationModel(Vector3 startRotation, Vector3 endRotation)
    {
        StartRotation = startRotation;
        EndRotation = endRotation;
    }
}

using System;

[Serializable]
public struct ColormapModel
{
    public string Name;
    public float Min;
    public float Max;

    public ColormapModel(string name, float min, float max)
    {
        Name = name;
        Min = min;
        Max = max;
    }
}

using System;

[Serializable]
public struct CustomAtlasModel
{
    public string Name;
    public Vector3 Dimensions;
    public Vector3 Resolution;

    public CustomAtlasModel(string name, Vector3 dimensions, Vector3 resolution)
    {
        Name = name;
        Dimensions = dimensions;
        Resolution = resolution;
    }
}

using System;

[Serializable]
public struct CustomMeshData
{
    public string ID;
    public Vector3[] Vertices;
    public int[] Triangles;
    public Vector3[] Normals;

    public CustomMeshData(string id, Vector3[] vertices, int[] triangles, Vector3[] normals)
    {
        ID = id;
        Vertices = vertices;
        Triangles = triangles;
        Normals = normals;
    }
}

using System;

[Serializable]
public struct CustomMeshModel
{
    public string ID;
    public Vector3 Position;
    public bool UseReference;
    public string Material;
    public Vector3 Scale;
    public Color Color;

    public CustomMeshModel(string id, Vector3 position, bool useReference, string material, Vector3 scale, Color color)
    {
        ID = id;
        Position = position;
        UseReference = useReference;
        Material = material;
        Scale = scale;
        Color = color;
    }
}

using System;

[Serializable]
public struct MeshModel
{
    public string ID;
    public string Shape;
    public Vector3 Position;
    public Color Color;
    public Vector3 Scale;
    public string Material;
    public bool Interactive;

    public MeshModel(string id, string shape, Vector3 position, Color color, Vector3 scale, string material, bool interactive)
    {
        ID = id;
        Shape = shape;
        Position = position;
        Color = color;
        Scale = scale;
        Material = material;
        Interactive = interactive;
    }
}

using System;

[Serializable]
public struct ParticleGroupModel
{
    public string ID;
    public Vector3 Scale;
    public string Shape;
    public string Material;
    public float[] Xs;
    public float[] Ys;
    public float[] Zs;
    public Color[] Colors;

    public ParticleGroupModel(string id, Vector3 scale, string shape, string material, float[] xs, float[] ys, float[] zs, Color[] colors)
    {
        ID = id;
        Scale = scale;
        Shape = shape;
        Material = material;
        Xs = xs;
        Ys = ys;
        Zs = zs;
        Colors = colors;
    }
}

using System;

[Serializable]
public struct PrimitiveMeshModel
{
    public MeshModel[] Data;

    public PrimitiveMeshModel(MeshModel[] data)
    {
        Data = data;
    }
}

using System;

[Serializable]
public struct StructureModel
{
    public string Name;
    public string Acronym;
    public int AtlasId;
    public Color Color;
    public bool Visible;
    public float ColorIntensity;
    public int Side;
    public string Material;

    public StructureModel(string name, string acronym, int atlasId, Color color, bool visible, float colorIntensity, int side, string material)
    {
        Name = name;
        Acronym = acronym;
        AtlasId = atlasId;
        Color = color;
        Visible = visible;
        ColorIntensity = colorIntensity;
        Side = side;
        Material = material;
    }
}

