using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColormapPanel : MonoBehaviour
{
    [SerializeField] TMP_Text _lowText;
    [SerializeField] TMP_Text _highText;

    private RawImage _colormapImage;
    private Texture2D _colormapTexture2D;

    private void Awake()
    {
        _colormapImage = GetComponent<RawImage>();
        int pxHeight = (int)_colormapImage.rectTransform.rect.height;
        int pxWidth = (int)_colormapImage.rectTransform.rect.width;
        _colormapTexture2D = new Texture2D(pxWidth, pxHeight);
        _colormapTexture2D.wrapMode = TextureWrapMode.Clamp;
        _colormapTexture2D.filterMode = FilterMode.Point;
        _colormapImage.texture = _colormapTexture2D;

        SetColormapVisibility(false);
    }

    public void ColormapChanged(Converter<float, Color> func, bool state, float low, float high)
    {
        SetColormap(func);
        SetColormapVisibility(state);

        _lowText.text = low.ToString();
        _highText.text = high.ToString();
    }

    public void SetColormap(Converter<float,Color> colormapFunc)
    {
        Debug.Log("Setting colormap");
        int pxHeight = (int)_colormapImage.rectTransform.rect.height;
        int pxWidth = (int)_colormapImage.rectTransform.rect.width;

        for (int y = 0; y < pxHeight; y++)
        {
            Color lineColor = colormapFunc(y / (float)pxHeight);

            for (int x = 0; x < pxWidth; x++)
            {
                _colormapTexture2D.SetPixel(x, y, lineColor);
            }
        }

        _colormapTexture2D.Apply();
    }

    public void SetColormapVisibility(bool state)
    {
        _colormapImage.enabled = state;
    }
}
