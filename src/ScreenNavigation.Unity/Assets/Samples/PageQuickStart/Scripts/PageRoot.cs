using ScreenNavigation.Page;
using UnityEngine;

namespace Samples.PageQuickStart
{
    public class PageRoot : MonoBehaviour, IParentProvider
    {
        public Transform Parent => transform;
    }
}