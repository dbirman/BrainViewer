using System;
using System.Collections.Generic;
using UnityEngine;
using BrainAtlas;
using UnityEngine.Events;
using Urchin.Utils;
using System.Threading.Tasks;
using Urchin.API;

namespace Urchin.Managers
{
    public class AtlasManager : Manager
    {
        #region Serialized
        [SerializeField] private List<string> _apiNames;
        [SerializeField] private List<string> _atlasNames;
        [SerializeField] private List<bool> _availableOnWebGL;
        #endregion

        #region static
        public static HashSet<OntologyNode> VisibleNodes { get; private set; }

        public override ManagerType Type => ManagerType.AtlasManager;

        public static AtlasManager Instance;
        #endregion

        #region Data
        private AtlasModel Data;

        private Dictionary<string, (string name, bool webgl)> _apiNameMapping;

        /// <summary>
        /// Intensity values map to full, left, right
        /// </summary>
        private Dictionary<OntologyNode, (float full, float left, float right)> _areaIntensity;

        private Dictionary<int, List<float>> _areaData;
        private int _areaDataIndex;
        private Dictionary<int, (bool full, bool left, bool right)> _areaSides;

        private bool _colormapEnabled;
        private Colormap _localColormap;
        #endregion

        #region Events
        public UnityEvent<OntologyNode> NodeVisibleEvent;
        public UnityEvent<Converter<float, Color>, bool, float, float> ColormapChangedEvent;
        #endregion

        #region Unity
        private void Awake()
        {
            _areaSides = new();
            _areaData = new();
            _areaIntensity = new();
            VisibleNodes = new();

            if (Instance != null)
                throw new Exception("Only one AreaManager can exist in the scene!");
            Instance = this;

            // Organize dictionary
            _apiNameMapping = new();
            for (int i = 0; i < _apiNames.Count; i++)
            {
                _apiNameMapping.Add(_apiNames[i],
                    (_atlasNames[i], _availableOnWebGL[i]));
            }
        }

        private void Start()
        {
            _colormapEnabled = false;
            _localColormap = Colormaps.MainColormap;

            Client_SocketIO.AtlasUpdate += UpdateData;

            Client_SocketIO.ClearAreas += ClearAreas;

            Client_SocketIO.AtlasLoad += LoadAtlas;
            Client_SocketIO.AtlasLoadDefaults += LoadDefaultAreasVoid;

            //Client_SocketIO.AtlasCreateCustom += CustomAtlas;
        }
        #endregion

        #region Manager
        public override string ToSerializedData()
        {
            return JsonUtility.ToJson(Data);
        }

        public override void FromSerializedData(string serializedData)
        {
            Data = JsonUtility.FromJson<AtlasModel>(serializedData);

            UpdateData(Data);
        }

        #endregion

        #region Public

        public void UpdateData(AtlasModel data)
        {
#if UNITY_EDITOR
            Debug.Log("(AtlasManager) Area update called");
#endif
            Data = data;

            if (BrainAtlasManager.ActiveReferenceAtlas.Name != _apiNameMapping[data.Name].name)
            {
                Client_SocketIO.LogError($"Update failed. Atlas {BrainAtlasManager.ActiveReferenceAtlas.Name} is already loaded, re-start Urchin to change atlases.");
                return;
            }

            // Check for colormap
            if (data.Colormap.Name != "")
            {
                SetAreaColormap(data.Colormap.Name);
            }

            foreach (var structure in data.Areas)
            {
                UpdateArea(structure);
            }
        }

        public async void LoadAtlas(string apiName)
        {
            Task atlasTask;
            
            if (!_apiNameMapping.ContainsKey(apiName))
            {
                Client_SocketIO.LogError($"Atlas {apiName} does not exist.");
                return;
            }

            (string atlasName, bool availableOnWebGL) = _apiNameMapping[apiName];

#if UNITY_WEBGL
            if (!availableOnWebGL)
            {
                Client_SocketIO.LogError($"Atlas {atlasName} is not available on this platform.");
                return;
            }
#endif

            atlasTask = BrainAtlasManager.LoadAtlas(atlasName);

            await atlasTask;

            BrainAtlasManager.SetReferenceCoord(Utils.Utils.BregmaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name]);
#if UNITY_EDITOR
            Debug.Log($"Reference coordinate set to {Utils.Utils.BregmaDefaults[BrainAtlasManager.ActiveReferenceAtlas.Name]}");
#endif
        }

        public async void UpdateArea(StructureModel structureData)
        {
            int atlasID = structureData.AtlasId;

            OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(atlasID);
            if (node==null)
            {
                Debug.LogWarning($"Can't find node {structureData.Acronym} with ID {atlasID}");
                return;
            }

            OntologyNode.OntologyNodeSide side = (OntologyNode.OntologyNodeSide)structureData.Side;

            // If the structure isn't loaded and it needs to be visible, load it
            if (structureData.Visible)
            {
                if (!(node.FullLoaded.IsCompleted || node.SideLoaded.IsCompleted))
                {
                    // Node needs to be loaded
                    LoadArea(node, structureData.Visible, side);
                    await (side == OntologyNode.OntologyNodeSide.Full ? node.FullLoaded : node.SideLoaded);
                }

                node.SetVisibility(structureData.Visible, side);
                if (!VisibleNodes.Contains(node))
                    VisibleNodes.Add(node);

                if (BrainAtlasManager.BrainRegionMaterials.ContainsKey(structureData.Material))
                    node.SetMaterial(BrainAtlasManager.BrainRegionMaterials[structureData.Material], side);
                else
                    Client_SocketIO.LogWarning($"Material {structureData.Material} does not exist");

                if (!_colormapEnabled)
                {
                    node.SetColor(structureData.Color, side);
                }
                else
                {
                    node.SetColor(_localColormap.Value(structureData.ColorIntensity));
                }

                node.SetShaderProperty("_Alpha", structureData.Color.a, side);
            }
            else
            {
                if (node.FullGO != null || node.LeftGO != null)
                    node.SetVisibility(structureData.Visible, side);
            }

        }

        public void LoadArea(OntologyNode node, bool visible, OntologyNode.OntologyNodeSide side)
        {
            if (node == null)
                return;

            bool set = false;

            bool full = side == OntologyNode.OntologyNodeSide.Full;
            bool leftSide = side == OntologyNode.OntologyNodeSide.Left;
            bool rightSide = side == OntologyNode.OntologyNodeSide.Right;

            if (full && node.FullLoaded.IsCompleted)
            {
                node.SetVisibility(visible, OntologyNode.OntologyNodeSide.Full);
                set = true;
#if UNITY_EDITOR
                Debug.Log("Setting full model visibility to true");
#endif
            }
            if (leftSide && node.SideLoaded.IsCompleted)
            {
                node.SetVisibility(visible, OntologyNode.OntologyNodeSide.Left);
                set = true;
#if UNITY_EDITOR
                Debug.Log("Setting left model visibility to true");
#endif
            }
            if (rightSide && node.SideLoaded.IsCompleted)
            {
                node.SetVisibility(visible, OntologyNode.OntologyNodeSide.Right);
                set = true;
#if UNITY_EDITOR
                Debug.Log("Setting right model visibility to true");
#endif
            }

            if (set)
                NodeVisibleEvent.Invoke(node);
            else
                LoadIndividualArea(node, full, leftSide, rightSide, visible);
        }

        // Area colormaps
        public void SetAreaColormap(string colormapName)
        {
            if (colormapName != "" && Colormaps.ColormapDict.ContainsKey(colormapName))
            {
                _colormapEnabled = true;
                _localColormap = Colormaps.ColormapDict[colormapName];
            }
            else
                _colormapEnabled = false;

            ColormapChangedEvent.Invoke(_localColormap.Value, _colormapEnabled, Data.Colormap.Min, Data.Colormap.Max);
        }


        // Area data
        public void SetAreaData(Dictionary<string, List<float>> areaData)
        {
            foreach (KeyValuePair<string, List<float>> kvp in areaData)
            {
                (int ID, bool full, bool leftSide, bool rightSide) = GetID(kvp.Key);

                if (_areaData.ContainsKey(ID))
                {
                    _areaData[ID] = kvp.Value;
                    _areaSides[ID] = (full, leftSide, rightSide);
                }
                else
                {
                    _areaData.Add(ID, kvp.Value);
                    _areaSides.Add(ID, (full, leftSide, rightSide));
                }
            }
        }

        public void SetAreaIndex(int areaDataIdx)
        {
            _areaDataIndex = areaDataIdx;
            UpdateAreaDataIntensity();
        }


        // Auto-loaders
        public async void LoadDefaultAreasVoid()
        {
            int[] defaultAreaIDs = BrainAtlasManager.ActiveReferenceAtlas.DefaultAreas;

            foreach (int areaID in defaultAreaIDs)
            {
                OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(areaID);

                // Load all models
                _ = node.LoadMesh(OntologyNode.OntologyNodeSide.All);

                await Task.WhenAll(new Task[] { node.FullLoaded, node.SideLoaded });

                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Full);
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Left);
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Right);

                VisibleNodes.Add(node);
            }
        }

        public async Task<List<OntologyNode>> LoadDefaultAreas(string defaultName)
        {
            List<OntologyNode> nodes = new();
            int[] defaultAreaIDs = BrainAtlasManager.ActiveReferenceAtlas.DefaultAreas;

            foreach (int areaID in defaultAreaIDs)
            {
                OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(areaID);
                nodes.Add(node);

                // Load all models
                _ = node.LoadMesh(OntologyNode.OntologyNodeSide.All);

                await Task.WhenAll(new Task[] { node.FullLoaded, node.SideLoaded });

                node.SetVisibility(false, OntologyNode.OntologyNodeSide.Full);
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Left);
                node.SetVisibility(true, OntologyNode.OntologyNodeSide.Right);

                VisibleNodes.Add(node);
            }

            return nodes;
        }

        #endregion

        #region Public helpers
        public void ClearAreas()
        {
            Debug.Log("(Client) Clearing areas");
            foreach (OntologyNode node in VisibleNodes)
                node.SetVisibility(false, OntologyNode.OntologyNodeSide.All);
            VisibleNodes.Clear();

            if (_colormapEnabled)
            {
                SetAreaColormap("");
            }
        }

        /// <summary>
        /// Convert an acronym or ID label into the Allen CCF area ID
        /// </summary>
        /// <param name="idOrAcronym">An ID number (e.g. 0) or an acronym (e.g. "root")</param>
        /// <returns>(int Allen CCF ID, bool left side model, bool right side model)</returns>
        public static (int ID, bool full, bool leftSide, bool rightSide) GetID(string idOrAcronym)
        {
            // Check whether a suffix was included
            int leftIndex = idOrAcronym.IndexOf("-lh");
            int rightIndex = idOrAcronym.IndexOf("-rh");
            bool leftSide = leftIndex > 0;
            bool rightSide = rightIndex > 0;
            bool full = !(leftSide || rightSide);

            //Remove the suffix
            if (leftSide)
                idOrAcronym = idOrAcronym.Substring(0, leftIndex);
            if (rightSide)
                idOrAcronym = idOrAcronym.Substring(0, rightIndex);

            // Lowercase
            string lower = idOrAcronym.ToLower();

            // Check for root (special case, which we can't currently handle)
            if (lower.Equals("void"))
                return (-1, full, leftSide, rightSide);

            try
            {
                int ID = BrainAtlasManager.ActiveReferenceAtlas.Ontology.Acronym2ID(idOrAcronym);
                return (ID, full, leftSide, rightSide);
            }
            catch
            {
                // It wasn't an acronym, so it has to be an integer
                int ret;
                if (int.TryParse(idOrAcronym, out ret))
                    return (ret, full, leftSide, rightSide);
            }

            // We failed to figure out what this was
            return (-1, full, leftSide, rightSide);
        }

        #endregion

        #region Private helpers

        private async void LoadIndividualArea(OntologyNode node, bool full, bool leftSide, bool rightSide, bool visibility)
        {
#if UNITY_EDITOR
            Debug.Log("Loading model");
#endif
            VisibleNodes.Add(node);

            await node.LoadMesh(OntologyNode.OntologyNodeSide.All);

            node.SetVisibility(full && visibility, OntologyNode.OntologyNodeSide.Full);

            await node.SideLoaded;
            node.SetVisibility(leftSide && visibility, OntologyNode.OntologyNodeSide.Left);
            node.SetVisibility(rightSide && visibility, OntologyNode.OntologyNodeSide.Right);

            node.ResetColor();

            NodeVisibleEvent.Invoke(node);
        }

        private async void UpdateAreaDataIntensity()
        {
            foreach (KeyValuePair<int, List<float>> kvp in _areaData)
            {
                OntologyNode node = BrainAtlasManager.ActiveReferenceAtlas.Ontology.ID2Node(kvp.Key);

                (bool full, bool leftSide, bool rightSide) = _areaSides[kvp.Key];

                float currentValue = kvp.Value[_areaDataIndex];

                if (node != null)
                {
                    if (full)
                    {
                        await node.FullLoaded;
                        node.SetShaderProperty("_Alpha", currentValue, OntologyNode.OntologyNodeSide.Full);
                    }
                    else
                    {
                        await node.SideLoaded;
                        if (leftSide)
                            node.SetShaderProperty("_Alpha", currentValue, OntologyNode.OntologyNodeSide.Left);
                        if (rightSide)
                            node.SetShaderProperty("_Alpha", currentValue, OntologyNode.OntologyNodeSide.Right);
                    }
                    Color color = _localColormap.Value(currentValue);
                    if (full)
                        node.SetColor(color, OntologyNode.OntologyNodeSide.Full);
                    if (leftSide)
                        node.SetColor(color, OntologyNode.OntologyNodeSide.Left);
                    if (rightSide)
                        node.SetColor(color, OntologyNode.OntologyNodeSide.Right);
                }
                else
                    Debug.Log("Failed to set " + kvp.Key + " to " + kvp.Value);
            }
        }
        #endregion
    }
}