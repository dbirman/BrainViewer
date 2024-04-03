using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Urchin.API;

namespace Urchin.Managers
{
    public class LineRendererManager : MonoBehaviour
    {
        #region Public
        [SerializeField] private GameObject _lineRendererPrefabGO;
        [SerializeField] private Transform _lineRendererParentT;
        #endregion

        //Keep a dictionary that maps string names to line renderer components 
        private Dictionary<string, LineBehavior> _lineBehaviors;

        private void Awake()
        {
            _lineBehaviors = new();
        }

        private void Start()
        {
            Client_SocketIO.UpdateLine += UpdateData;
            Client_SocketIO.DeleteLine += Delete;
        }

        public void UpdateData(LineModel data)
        {
            if (_lineBehaviors.ContainsKey(data.ID))
            {
                _lineBehaviors[data.ID].UpdateData(data);
            }
            else
                Create(data);
        }

        public void Create(LineModel data)
        {
            GameObject lineGO = Instantiate(_lineRendererPrefabGO, _lineRendererParentT);
            lineGO.name = $"lineRenderer_{data.ID}";
            _lineBehaviors.Add(data.ID, lineGO.GetComponent<LineBehavior>());
        }

        public void Delete(IDData data)
        {
            if (_lineBehaviors.ContainsKey(data.ID))
            {
                Destroy(_lineBehaviors[data.ID].gameObject);
                _lineBehaviors.Remove(data.ID);
            }
            else
                Client_SocketIO.LogError($"Cannot delete line {data.ID}, doesn't exist in Unity");
        }
    }
}