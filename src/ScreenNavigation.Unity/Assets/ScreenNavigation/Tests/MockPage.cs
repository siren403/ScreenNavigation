using Cysharp.Threading.Tasks;
using ScreenNavigation.Page;
using ScreenNavigation.Page.Commands;
using UnityEngine;
using VitalRouter;

namespace ScreenNavigation.Tests
{
    [Routes]
    public partial class MockPage : IPage
    {
        public const string Login = "LoginPage";
        public const string Home = "HomePage";
        public const string Settings = "SettingsPage";

        public string Id { get; set; }
        public bool IsVisible { get; set; }

        [Route]
        private async UniTask On(ShowCommand command, PublishContext context)
        {
            IsVisible = true;
            Debug.Log($"Showing page: {Id}");
            await UniTask.DelayFrame(3, cancellationToken: context.CancellationToken); // Simulate async operation
        }

        [Route]
        private async UniTask On(HideCommand command)
        {
            Debug.Log($"Hiding page: {Id}");
            await UniTask.DelayFrame(3); // Simulate async operation
            IsVisible = false;
        }
    }
}