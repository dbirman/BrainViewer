using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Urchin.API;

namespace Urchin.Managers
{
    public class TextManager : MonoBehaviour
    {
        #region Serialized
        [SerializeField] private GameObject _textParent;
        [SerializeField] private GameObject _textPrefab;
        [SerializeField] private Canvas uiCanvas;
        #endregion

        #region Private variables
        private Dictionary<string, GameObject> _textGOs;
        #endregion

        #region Unity
        private void Awake()
        {
            _textGOs = new Dictionary<string, GameObject>();
        }

        private void Start()
        {
            Client_SocketIO.TextUpdate += UpdateData;
            Client_SocketIO.TextDelete += Delete;
            Client_SocketIO.TextSetTexts += SetTexts;
            Client_SocketIO.TextSetColors += SetColors;
            Client_SocketIO.TextSetSizes += SetSizes;
            Client_SocketIO.TextSetPositions += SetPositions;

            Client_SocketIO.ClearText += Clear;
        }
        #endregion

        #region Public functions

        public void UpdateData(TextModel data)
        {
            if (_textGOs.ContainsKey(data.ID))
            {
                // Update
                TMP_Text text = _textGOs[data.ID].GetComponent<TMP_Text>();
                text.text = data.Text;
                text.color = data.Color;
                text.fontSize = data.FontSize;
                SetPosition(_textGOs[data.ID], data.Position);
            }
            else
            {
                // Create
                Create(data);
            }
        }

        public void Create(TextModel data)
        {
            GameObject textGO = Instantiate(_textPrefab, _textParent.transform);
            _textGOs.Add(data.ID, textGO);
        }

        public void Delete(IDData data)
        {
            if (_textGOs.ContainsKey(data.ID))
            {
                Destroy(_textGOs[data.ID]);
                _textGOs.Remove(data.ID);
            }
        }

        public void SetTexts(IDListStringList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_textGOs.ContainsKey(data.IDs[i]))
                {
                    _textGOs[data.IDs[i]].GetComponent<TMP_Text>().text =  data.Values[i];
                }
                else
                    Client_SocketIO.LogError($"Cannot set position. Text object with ID {data.IDs[i]} does not exist in Urchin");
            }
        }

        public void SetColors(IDListColorList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_textGOs.ContainsKey(data.IDs[i]))
                {
                    _textGOs[data.IDs[i]].GetComponent<TMP_Text>().color = data.Values[i];
                }
                else
                    Client_SocketIO.LogError($"Cannot set position. Text object with ID {data.IDs[i]} does not exist in Urchin");
            }
        }

        public void SetSizes(IDListFloatList data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_textGOs.ContainsKey(data.IDs[i]))
                {
                    _textGOs[data.IDs[i]].GetComponent<TMP_Text>().fontSize = data.Values[i];
                }
                else
                    Client_SocketIO.LogError($"Cannot set position. Text object with ID {data.IDs[i]} does not exist in Urchin");
            }
        }

        public void SetPositions(IDListVector2List data)
        {
            for (int i = 0; i < data.IDs.Length; i++)
            {
                if (_textGOs.ContainsKey(data.IDs[i]))
                {
                    SetPosition(_textGOs[data.IDs[i]], data.Values[i]);
                }
                else
                    Client_SocketIO.LogError($"Cannot set position. Text object with ID {data.IDs[i]} does not exist in Urchin");
            }
        }

        private void SetPosition(GameObject textGO, Vector2 position)
        {
            Vector2 canvasWH = new Vector2(uiCanvas.GetComponent<RectTransform>().rect.width, uiCanvas.GetComponent<RectTransform>().rect.height);

            textGO.transform.localPosition = new Vector2(canvasWH.x * position.x / 2, canvasWH.y * position.y / 2);
        }

        public void Clear()
        {
            Debug.Log("(Client) Clearing text");
            foreach (GameObject text in _textGOs.Values)
                Destroy(text);

            _textGOs.Clear();
        }
        #endregion
    }
}