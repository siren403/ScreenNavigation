using System;
using ScreenNavigation.Page;
using UnityEngine;

namespace Samples.PageQuickStart.Pages
{

    [RequireComponent(typeof(Canvas))]
    public abstract class CanvasPage : MonoBehaviour, IPage
    {
        public void OnShow()
        {
            GetComponent<Canvas>().enabled = true;
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        public void OnHide()
        {
            GetComponent<Canvas>().enabled = false;
            GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }
}