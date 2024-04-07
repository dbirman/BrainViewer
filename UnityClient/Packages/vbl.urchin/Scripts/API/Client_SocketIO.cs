using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO3;
using System;
using UnityEngine.Events;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;

namespace Urchin.API
{/// <summary>
 /// Entry point for all client-side messages coming from the Python API
 /// Handles messages and tracks creation/destruction of prefab objects
 /// </summary>
    public class Client_SocketIO : MonoBehaviour
    {
        #region Static
        public static readonly int SOCKET_IO_MAX_CHUNK_BYTES = 10000000; // maximum SocketIO message size seems to be 256KB
        #endregion

        #region Events
        public UnityEvent<string> IDChangedEvent;
        #endregion

        #region variables
        private const string ID_SAVE_KEY = "id";
        private string _ID;
        public string ID
        {
            get { return _ID; }
            set
            {
                _ID = value;
                PlayerPrefs.SetString(ID_SAVE_KEY, ID);
                manager.Socket.Emit("ID", new List<string>() { ID, "receive" });
                IDChangedEvent.Invoke(ID);
            }
        }

        [SerializeField] private bool localhost;

        private static SocketManager manager;
        #endregion

        void Start()
        {
            // Only allow localhost when running in the editor
#if UNITY_EDITOR
        string url = localhost ? "http://localhost:5000" : "https://urchin-commserver.herokuapp.com/";
#else
            string url = "https://urchin-commserver.herokuapp.com/";
#endif
            Debug.Log("Attempting to connect: " + url);

            manager = localhost ? new SocketManager(new Uri(url)) : new SocketManager(new Uri(url));

            manager.Socket.On("connect", Connected);
#if UNITY_EDITOR
        manager.Socket.On("reconnect", () => { Debug.Log("(Client) client reconnected -- could be sign of a timeout issue"); });
#endif

            // Call the startup functions, these bind all the Socket.on events and setup the static Actions, which
            // other scripts can then listen to
            Start_Atlas();
            Start_Volume();
            Start_Particles();
            Start_Probes();
            Start_Camera();
            Start_Light();
            Start_Text();
            Start_LineRenderer();
            Start_Mesh();
            Start_FOV();
            Start_CustomMesh();
            Start_Dock();

            // Misc
            manager.Socket.On<string>("Clear", Clear);
        }

        #region Socket setup by action group
        public static Action<AtlasModel> AtlasUpdate;
        public static Action<string> AtlasLoad;
        public static Action AtlasLoadDefaults;

        //public static Action<CustomAtlasData> AtlasCreateCustom;
        //public static Action<Vector3Data> AtlasSetReferenceCoord;
        //public static Action<AreaGroupData> AtlasSetAreaVisibility;
        public static Action<Dictionary<string, string>> AtlasSetAreaColors;
        public static Action<Dictionary<string, float>> AtlasSetAreaIntensities;
        public static Action<string> AtlasSetColormap;
        public static Action<Dictionary<string, string>> AtlasSetAreaMaterials;
        public static Action<Dictionary<string, float>> AtlasSetAreaAlphas;
        public static Action<Dictionary<string, List<float>>> AtlasSetAreaData;
        public static Action<int> AtlasSetAreaDataIndex;

        private void Start_Atlas()
        {
            manager.Socket.On<string>("urchin-atlas-update", x => AtlasUpdate.Invoke(JsonUtility.FromJson<AtlasModel>(x)));
            manager.Socket.On<string>("urchin-atlas-load", x => AtlasLoad.Invoke(x));
            manager.Socket.On<string>("urchin-atlas-defaults", x => AtlasLoadDefaults.Invoke());


            // CCF Areas
            //manager.Socket.On<string>("LoadAtlas", x => AtlasLoad.Invoke(x));
            ////manager.Socket.On<string>("CustomAtlas", x => AtlasCreateCustom.Invoke(JsonUtility.FromJson<CustomAtlasData>(x)));
            ////manager.Socket.On<string>("AtlasSetReferenceCoord", x => AtlasSetReferenceCoord.Invoke(JsonUtility.FromJson<Vector3Data>(x)));
            //manager.Socket.On<string>("SetAreaVisibility", x => AtlasSetAreaVisibility.Invoke(JsonUtility.FromJson<AreaGroupData>(x)));
            //manager.Socket.On<Dictionary<string, string>>("SetAreaColors", x => AtlasSetAreaColors.Invoke(x));
            //manager.Socket.On<Dictionary<string, float>>("SetAreaIntensity", x => AtlasSetAreaIntensities.Invoke(x));
            //manager.Socket.On<string>("SetAreaColormap", x => AtlasSetColormap.Invoke(x));
            //manager.Socket.On<Dictionary<string, string>>("SetAreaMaterial", x => AtlasSetAreaMaterials.Invoke(x));
            //manager.Socket.On<Dictionary<string, float>>("SetAreaAlpha", x => AtlasSetAreaAlphas.Invoke(x));
            //manager.Socket.On<Dictionary<string, List<float>>>("SetAreaData", x => AtlasSetAreaData.Invoke(x));
            //manager.Socket.On<int>("SetAreaIndex", x => AtlasSetAreaDataIndex.Invoke(x));
            //manager.Socket.On<string>("LoadDefaultAreas", x => AtlasLoadAreaDefaults.Invoke());
        }

        //public static Action<VolumeDataChunk> SetVolumeData;
        //public static Action<VolumeMeta> UpdateVolume;
        public static Action<string[]> SetVolumeColormap;
        public static Action<string> DeleteVolume;

        private void Start_Volume()
        {
            //manager.Socket.On<string>("UpdateVolume", x => UpdateVolume.Invoke(JsonUtility.FromJson<VolumeMeta>(x)));
            //manager.Socket.On<string>("SetVolumeData", x => SetVolumeData.Invoke(JsonUtility.FromJson<VolumeDataChunk>(x)));
            manager.Socket.On<string>("DeleteVolume", x => DeleteVolume.Invoke(x));
        }


        public static Action<ParticleSystemModel> ParticlesUpdate;
        public static Action<IDData> ParticlesDelete;
        public static Action<Vector3List> ParticlesSetPositions;
        public static Action<FloatList> ParticlesSetSizes;
        public static Action<ColorList> ParticlesSetColors;

        private void Start_Particles()
        {
            manager.Socket.On<string>("urchin-particles-update", x => ParticlesUpdate.Invoke(JsonUtility.FromJson<ParticleSystemModel>(x)));
            manager.Socket.On<string>("urchin-particles-delete", x => ParticlesDelete.Invoke(JsonUtility.FromJson<IDData>(x)));
            manager.Socket.On<string>("urchin-particles-positions", x => ParticlesSetPositions.Invoke(JsonUtility.FromJson<Vector3List>(x)));
            manager.Socket.On<string>("urchin-particles-sizes", x => ParticlesSetSizes.Invoke(JsonUtility.FromJson<FloatList>(x)));
            manager.Socket.On<string>("urchin-particles-colors", x => ParticlesSetColors.Invoke(JsonUtility.FromJson<ColorList>(x)));
        }


        //pubic statc Action<ProbeModel

        public static Action<List<string>> CreateProbes;
        public static Action<List<string>> DeleteProbes;
        public static Action<Dictionary<string, string>> SetProbeColors;
        public static Action<Dictionary<string, List<float>>> SetProbePos;
        public static Action<Dictionary<string, List<float>>> SetProbeAngles;
        public static Action<Dictionary<string, string>> SetProbeStyle;
        public static Action<Dictionary<string, List<float>>> SetProbeSize;

        private void Start_Probes()
        {
            manager.Socket.On<List<string>>("CreateProbes", x => CreateProbes.Invoke(x));
            manager.Socket.On<List<string>>("DeleteProbes", x => DeleteProbes.Invoke(x));
            manager.Socket.On<Dictionary<string, string>>("SetProbeColors", x => SetProbeColors.Invoke(x));
            manager.Socket.On<Dictionary<string, List<float>>>("SetProbePos", x => SetProbePos.Invoke(x));
            manager.Socket.On<Dictionary<string, List<float>>>("SetProbeAngles", x => SetProbeAngles.Invoke(x));
            manager.Socket.On<Dictionary<string, string>>("SetProbeStyle", x => SetProbeStyle.Invoke(x));
            manager.Socket.On<Dictionary<string, List<float>>>("SetProbeSize", x => SetProbeSize.Invoke(x));
        }

        // New Camera
        public static Action<CameraRotationModel> SetCameraLerpRotation;
        public static Action<FloatData> SetCameraLerp;
        public static Action<FloatData> CameraBrainYaw;
        public static Action<string> RequestScreenshot;

        public static Action<CameraModel> UpdateCamera;
        public static Action<IDData> DeleteCamera;

        private void Start_Camera()
        {
            //New
            manager.Socket.On<string>("urchin-camera-update", x => UpdateCamera.Invoke(JsonUtility.FromJson<CameraModel>(x)));
            manager.Socket.On<string>("urchin-camera-delete", x => DeleteCamera.Invoke(JsonUtility.FromJson<IDData>(x)));

            manager.Socket.On<string>("urchin-camera-lerp-set", x => SetCameraLerpRotation.Invoke(JsonUtility.FromJson<CameraRotationModel>(x)));
            manager.Socket.On<string>("urchin-camera-lerp", x => SetCameraLerp.Invoke(JsonUtility.FromJson<FloatData>(x)));
            manager.Socket.On<string>("urchin-camera-screenshot-request", x => RequestScreenshot.Invoke(x));


            manager.Socket.On<string>("urchin-brain-yaw", x => CameraBrainYaw.Invoke(JsonUtility.FromJson<FloatData>(x)));

        }

        public static Action ResetLightLink;
        public static Action<string> SetLightLink;
        public static Action<List<float>> SetLightRotation;

        private void Start_Light()
        {
            manager.Socket.On("ResetLightLink", () => ResetLightLink.Invoke());
            manager.Socket.On<string>("SetLightLink", x => SetLightLink.Invoke(x));
            manager.Socket.On<List<float>>("SetLightRotation", x => SetLightRotation.Invoke(x));
        }



        public static Action<TextModel> TextUpdate;
        public static Action<IDData> TextDelete;
        public static Action<IDListStringList> TextSetTexts;
        public static Action<IDListColorList> TextSetColors;
        public static Action<IDListFloatList> TextSetSizes;
        public static Action<IDListVector2List> TextSetPositions;

        private void Start_Text()
        {
            manager.Socket.On<string>("urchin-text-update", x => TextUpdate.Invoke(JsonUtility.FromJson<TextModel>(x)));
            manager.Socket.On<string>("urchin-text-delete", x => TextDelete.Invoke(JsonUtility.FromJson<IDData>(x)));
            manager.Socket.On<string>("urchin-text-texts", x => TextSetTexts.Invoke(JsonUtility.FromJson<IDListStringList>(x)));
            manager.Socket.On<string>("urchin-text-colors", x => TextSetColors.Invoke(JsonUtility.FromJson<IDListColorList>(x)));
            manager.Socket.On<string>("urchin-text-sizes", x => TextSetSizes.Invoke(JsonUtility.FromJson<IDListFloatList>(x)));
            manager.Socket.On<string>("urchin-text-positions", x => TextSetPositions.Invoke(JsonUtility.FromJson<IDListVector2List>(x)));
        }

        public static Action<LineModel> UpdateLine;
        public static Action<IDData> DeleteLine;

        private void Start_LineRenderer()
        {
            manager.Socket.On<string>("urchin-line-update", x => UpdateLine.Invoke(JsonUtility.FromJson<LineModel>(x)));
            manager.Socket.On<string>("urchin-line-delete", x => DeleteLine.Invoke(JsonUtility.FromJson<IDData>(x)));
        }

        #region Mesh

        // singular
        public static Action<MeshModel> MeshUpdate;
        public static Action<IDData> MeshDelete;
        // plural
        public static Action<IDList> MeshDeletes;
        public static Action<IDListVector3List> MeshSetPositions;
        public static Action<IDListVector3List> MeshSetScales;
        public static Action<IDListColorList> MeshSetColors;
        public static Action<IDListStringList> MeshSetMaterials;

        private void Start_Mesh()
        {
            // Singular
            manager.Socket.On<string>("urchin-meshes-update", x => MeshUpdate.Invoke(JsonUtility.FromJson<MeshModel>(x)));
            manager.Socket.On<string>("urchin-meshes-delete", x => MeshDelete.Invoke(JsonUtility.FromJson<IDData>(x)));

            // Plural
            manager.Socket.On<string>("urchin-meshes-deletes", x => MeshDeletes.Invoke(JsonUtility.FromJson<IDList>(x)));
            manager.Socket.On<string>("urchin-meshes-positions", x => MeshSetPositions.Invoke(JsonUtility.FromJson<IDListVector3List>(x)));
            manager.Socket.On<string>("urchin-meshes-scales", x => MeshSetScales.Invoke(JsonUtility.FromJson<IDListVector3List>(x)));
            manager.Socket.On<string>("urchin-meshes-colors", x => MeshSetColors.Invoke(JsonUtility.FromJson<IDListColorList>(x)));
            manager.Socket.On<string>("urchin-meshes-materials", x => MeshSetMaterials.Invoke(JsonUtility.FromJson<IDListStringList>(x)));
        }

        #endregion

        public static Action<List<string>> CreateFOV;
        public static Action<List<string>> DeleteFOV;
        public static Action<Dictionary<string, List<List<float>>>> SetFOVPos;
        public static Action<Dictionary<string, float>> SetFOVOffset;
        public static Action<List<object>> SetFOVTextureDataMetaInit;
        public static Action<List<object>> SetFOVTextureDataMeta;
        public static Action<byte[]> SetFOVTextureData;
        public static Action<Dictionary<string, bool>> SetFOVVisibility;

        private void Start_FOV()
        {
            manager.Socket.On<List<string>>("CreateFOV", x => CreateFOV.Invoke(x));
            manager.Socket.On<List<string>>("DeleteFOV", x => DeleteFOV.Invoke(x));
            manager.Socket.On<Dictionary<string, List<List<float>>>>("SetFOVPos", x => SetFOVPos.Invoke(x));
            manager.Socket.On<Dictionary<string, float>>("SetFOVOffset", x => SetFOVOffset.Invoke(x));
            manager.Socket.On<List<object>>("SetFOVTextureDataMetaInit", x => SetFOVTextureDataMetaInit.Invoke(x));
            manager.Socket.On<List<object>>("SetFOVTextureDataMeta", x => SetFOVTextureDataMeta.Invoke(x));
            manager.Socket.On<byte[]>("SetFOVTextureData", x => SetFOVTextureData.Invoke(x));
            manager.Socket.On<Dictionary<string, bool>>("SetFOVVisibility", x => SetFOVVisibility.Invoke(x));
        }

        //public static Action<CustomMeshData> CustomMeshCreate;
        //public static Action<CustomMeshDestroy> CustomMeshDestroy;
        //public static Action<CustomMeshPosition> CustomMeshPosition;
        //public static Action<Vector3Data> CustomMeshScale;

        private void Start_CustomMesh()
        {
            //manager.Socket.On<string>("CustomMeshCreate", x => CustomMeshCreate.Invoke(JsonUtility.FromJson<CustomMeshData>(x)));
            //manager.Socket.On<string>("CustomMeshDestroy", x => CustomMeshDestroy.Invoke(JsonUtility.FromJson<CustomMeshDestroy>(x)));
            //manager.Socket.On<string>("CustomMeshPosition", x => CustomMeshPosition.Invoke(JsonUtility.FromJson<CustomMeshPosition>(x)));
            //manager.Socket.On<string>("CustomMeshScale", x => CustomMeshScale.Invoke(JsonUtility.FromJson<Vector3Data>(x)));
        }

        public static Action<SaveRequest> Save;
        public static Action<LoadRequest> Load;

        private void Start_Dock()
        {
            manager.Socket.On<string>("urchin-save", x => Save.Invoke(JsonUtility.FromJson<SaveRequest>(x)));
            manager.Socket.On<string>("urchin-load", x => Load.Invoke(JsonUtility.FromJson<LoadRequest>(x)));
        }

        #endregion

        #region Clear

        public static Action ClearProbes;
        public static Action ClearAreas;
        public static Action ClearVolumes;
        public static Action ClearText;
        public static Action ClearParticles;
        public static Action ClearMeshes;
        public static Action ClearFOV;
        public static Action ClearCustomMeshes;
        public static Action ClearLines;
        public static List<Action> ClearAll = new List<Action> { ClearProbes, ClearAreas, ClearVolumes,
            ClearText, ClearParticles, ClearMeshes, ClearFOV, ClearCustomMeshes, ClearLines};
        private void Clear(string val)
        {
            switch (val)
            {
                case "all":
                    foreach (var action in ClearAll)
                        action?.Invoke();
                    break;
                case "probes":
                    ClearProbes?.Invoke();
                    break;
                case "area":
                    ClearAreas?.Invoke();
                    break;
                case "volume":
                    ClearVolumes?.Invoke();
                    break;
                case "text":
                    ClearText?.Invoke();
                    break;
                case "particles":
                    ClearParticles?.Invoke();
                    break;
                case "mesh":
                    ClearMeshes?.Invoke();
                    break;
                case "texture":
                    ClearFOV?.Invoke();
                    break;
                case "custom":
                    ClearCustomMeshes?.Invoke();
                    break;
                case "lines":
                    ClearLines?.Invoke();
                    break;
                    
            }
        }
        #endregion

        ////
        //// SOCKET FUNCTIONS
        ////

        public static void Emit(string header, string data)
        {
            manager.Socket.Emit(header, data);
        }

        public void UpdateID(string newID)
        {
            ID = newID;
            Debug.Log($"ID updated to {ID}");
        }

        private void OnDestroy()
        {
            manager.Close();
        }

        private void Connected()
        {

            // If we are building to WebGL or to Standalone, switch how you acquire the user's ID
            string queryID;
            bool webGLID = Utils.Utils.ParseQueryForID(out queryID);

            if (webGLID)
            {
                UpdateID(queryID);
                Debug.Log("Found ID in Query string, setting to: " + ID);
            }
            else if (PlayerPrefs.HasKey(ID_SAVE_KEY))
            {
                UpdateID(PlayerPrefs.GetString(ID_SAVE_KEY));
                Debug.Log("Found ID in PlayerPrefs, setting to: " + ID);
            }
        }

        public static void Log(string msg)
        {
            manager.Socket.Emit("log", JsonUtility.ToJson(new Log(msg)));
        }

        public static void LogWarning(string msg)
        {
            manager.Socket.Emit("log-warning", JsonUtility.ToJson(new LogWarning(msg)));
        }

        public static void LogError(string msg)
        {
            manager.Socket.Emit("log-error", JsonUtility.ToJson(new LogError($"{msg} -- errors can be reported at https://github.com/VirtualBrainLab/Urchin/issues")));
        }
    }

}
