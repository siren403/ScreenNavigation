using UnityEngine;

namespace ScreenNavigation.Page
{
    public interface IParentProvider
    {
        Transform Parent { get; }
    }
}