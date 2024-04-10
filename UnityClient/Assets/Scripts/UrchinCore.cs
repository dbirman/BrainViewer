using BrainAtlas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Urchin.Behaviors;
using Urchin.Cameras;
using Urchin.Managers;

public class UrchinCore : MonoBehaviour
{
    #region Static
    public static UrchinCore Instance;
    #endregion

    [SerializeField] private BrainCameraController cameraController;
    [SerializeField] private CameraBehavior _mainCameraBehavior;
    [SerializeField] private Canvas _uiCanvas;
    [SerializeField] private Canvas _uiCameraCanvas;
    [SerializeField] private ColormapPanel _colormapPanel;

    [SerializeField] private AtlasManager _atlasManager;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Transform brainControlsT;

    [SerializeField] private GameObject consolePanel;
    [SerializeField] private TextMeshProUGUI consoleText;

    [SerializeField] private GameObject idPanel;
    [SerializeField] private GameObject infoPanelText;

    [SerializeField] private bool loadDefaults;

    // Axes + Grid
    [SerializeField] private GameObject axesGO;
    [SerializeField] private GameObject gridGO;

    // Exploding
    [Range(0,1), SerializeField] private float percentageExploded = 0f;
    private float prevPerc = 0f;
    private bool explodeLeftOnly;
    private bool exploded;
    public bool colorLeftOnly { get; private set; }

    private Vector3 center = new Vector3(5.7f, 4f, -6.6f);

    // Neuron materials
    [SerializeField] private Dictionary<string, Material> neuronMaterials;


    private int[] cosmos = { 315, 698, 1089, 703, 623, 549, 1097, 313, 1065, 512 };
    private Dictionary<int, Vector3> cosmosMeshCenters;
    private Dictionary<OntologyNode, Vector3> originalTransformPositionsLeft;
    private Dictionary<OntologyNode, Vector3> originalTransformPositionsRight;
    private Dictionary<int, Vector3> cosmosVectors;
    
    // COSMOS
    [SerializeField] private List<GameObject> cosmosParentObjects;
    private int cosmosParentIdx = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            throw new Exception("There should only be one UrchinCore component in the scene");


        originalTransformPositionsLeft = new();
        originalTransformPositionsRight = new();

        RecomputeCosmosCenters();
    }

    public void ChangeCosmosIdx(int newIdx)
    {
        cosmosParentIdx = newIdx;
        RecomputeCosmosCenters();
        UpdateExploded(percentageExploded);
    }

    private void RecomputeCosmosCenters()
    {
        GameObject parentGO = cosmosParentObjects[cosmosParentIdx];
        parentGO.SetActive(true);

        cosmosMeshCenters = new Dictionary<int, Vector3>();
        cosmosVectors = new Dictionary<int, Vector3>();

        // save the cosmos transform positions
        foreach (int cosmosID in cosmos)
        {
            GameObject cosmosGO = parentGO.transform.Find(cosmosID + "L").gameObject;
            cosmosMeshCenters.Add(cosmosID, cosmosGO.GetComponentInChildren<Renderer>().bounds.center);
            cosmosGO.SetActive(false);

            cosmosVectors.Add(cosmosID, cosmosGO.transform.localPosition);
        }

        parentGO.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        //Check if we need to make an update
        if (prevPerc != percentageExploded)
        {
            prevPerc = percentageExploded;

            // for each tree node, move it's model away from the 0,0,0 point
            UpdateExploded();
        }

        // Before checking for keydown events, return if the user is typing in the input box
        if (idPanel.activeSelf && idPanel.GetComponentInChildren<TMP_InputField>().isFocused)
            return;

        // Check for key down events
        if (Input.GetKeyDown(KeyCode.C))
        {
            consolePanel.SetActive(!consolePanel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            // Hacky workaround because the brain controls doesn't attach to the UI element properly
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            if (settingsPanel.activeSelf)
                brainControlsT.localPosition = new Vector3(-82.3f, -85.6f, 0f);
            else
                brainControlsT.localPosition = new Vector3(20.2f, -85.6f, 0f);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            axesGO.SetActive(!axesGO.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            gridGO.SetActive(!gridGO.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            idPanel.SetActive(!idPanel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            KeyExplodeImplode(2f);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            KeyExplodeImplode(5f);
        }
    }

    public void RegisterNode(OntologyNode node)
    {
        if (node != null && node.SideLoaded.IsCompleted)
        {
            if (!originalTransformPositionsLeft.ContainsKey(node))
                originalTransformPositionsLeft.Add(node, node.LeftGO.transform.localPosition);
            if (!originalTransformPositionsRight.ContainsKey(node))
                originalTransformPositionsRight.Add(node, node.RightGO.transform.localPosition);
        }
    }


    public static void Log(string text)
    {
        // Todo: deal with log running off the screen
        Debug.Log(text);
        Instance.consoleText.text += "\n" + text;
    }

    public void SetLeftExplodeOnly(bool state)
    {
        explodeLeftOnly = state;
        UpdateExploded();
    }

    public void UpdateExploded(float newPercExploded)
    {
        percentageExploded = newPercExploded;
        UpdateExploded();
    }

    private void UpdateExploded()
    {
        //cameraController.SetControlBlock(true);

        //Vector3 flipVector = new Vector3(1f, 1f, -1f);
        //foreach (OntologyNode node in AtlasManager.VisibleNodes)
        //{
        //    int cosmos = AtlasManager..GetCosmosID(node.ID);
        //    Transform nodeTLeft = node.NodeModelLeftGO.transform;
        //    Transform nodeTright = node.NodeModelRightGO.transform;

        //    nodeTLeft.localPosition = originalTransformPositionsLeft[node.ID] +
        //        cosmosVectors[cosmos] * percentageExploded;

        //    if (explodeLeftOnly)
        //        nodeTright.localPosition = originalTransformPositionsRight[node.ID];
        //    else
        //        nodeTright.localPosition = originalTransformPositionsRight[node.ID] +
        //                Vector3.Scale(cosmosVectors[cosmos], flipVector) * percentageExploded;
        //}
    }

    public void UpdateDataIndex(float newIdx)
    {
        //TODO
    }

    public void UpdateTextColor(bool state)
    {
        foreach (TMP_Text text in infoPanelText.GetComponentsInChildren<TMP_Text>())
            text.color = state ? Color.black : Color.white;
    }

    public void KeyExplodeImplode(float explodeTime)
    { 
        exploded = !exploded;
        if (exploded)
            StartCoroutine(AnimateExplodeHelper(explodeTime));
        else
            StartCoroutine(AnimateImplodeHelper(explodeTime));
    }

    public void AnimateExplode(float explodeTime)
    {
        StartCoroutine(AnimateExplodeHelper(explodeTime));
    }
    public void AnimateImplode(float explodeTime)
    {
        StartCoroutine(AnimateImplodeHelper(explodeTime));
    }

    public void SetMainCameraMode(bool orthographic)
    {
        _mainCameraBehavior.Data.Mode = orthographic ? CameraMode.orthographic : CameraMode.perspective;
        _mainCameraBehavior.UpdateSettings();
        _uiCanvas.worldCamera = _mainCameraBehavior.ActiveCamera;
        _uiCameraCanvas.worldCamera = _mainCameraBehavior.ActiveCamera;
    }

    private IEnumerator AnimateExplodeHelper(float explodeTime)
    {
        float start = Time.realtimeSinceStartup;
        float end = explodeTime + start;

        while (Time.realtimeSinceStartup <= end)
        {
            yield return null;
            percentageExploded = Mathf.InverseLerp(start, end, Time.realtimeSinceStartup);
        }
    }
    private IEnumerator AnimateImplodeHelper(float explodeTime)
    {
        float start = Time.realtimeSinceStartup;
        float end = explodeTime + start;

        while (Time.realtimeSinceStartup <= end)
        {
            yield return null;
            percentageExploded = 1f - Mathf.InverseLerp(start, end, Time.realtimeSinceStartup);
        }
    }
}
