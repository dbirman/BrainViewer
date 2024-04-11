using UnityEngine;

namespace Urchin.Managers
{
    public abstract class Manager : MonoBehaviour
    {
        public abstract ManagerType Type { get; }

        public abstract string ToSerializedData();
        public abstract void FromSerializedData(string serializedData);

    }

    public enum ManagerType
    {
        PrimitiveMeshManager = 0,
        AtlasManager = 1,
        CameraManager = 2,
        CustomMeshManager = 3,
        FOVManager = 4,
        LightManager = 5,
        LineRendererManager = 6,
        ParticleManager = 7,
        ProbeManager = 8,
        TextManager = 9,
        VolumeManager = 10
    }
}