using System;
using Cysharp.Threading.Tasks;
using ScreenNavigation.Page;
using ScreenNavigation.Page.Commands;
using UnityEngine;
using VitalRouter;

namespace ScreenNavigation.Tests
{
    [Routes]
    public partial class FailingMockPage : IPage
    {
        public const string Failing = "FailingPage";

        public enum FailureMode
        {
            None,
            ShowThrows,
            HideThrows,
            BothThrow
        }

        public string Id { get; set; }
        public bool IsVisible { get; set; }
        public FailureMode Mode { get; set; } = FailureMode.None;

        public PageErrorCommand PageError { get; private set; }

        [Route]
        private async UniTask On(ShowCommand command)
        {
            await UniTask.Yield();

            if (Mode is FailureMode.ShowThrows or FailureMode.BothThrow)
            {
                throw new Exception($"Show failed for page: {Id}");
            }

            IsVisible = true;
            Debug.Log($"Showing failing page: {Id}");
        }

        [Route]
        private async UniTask On(HideCommand command)
        {
            await UniTask.Yield();

            if (Mode is FailureMode.HideThrows or FailureMode.BothThrow)
            {
                throw new Exception($"Hide failed for page: {Id}");
            }

            IsVisible = false;
            Debug.Log($"Hiding failing page: {Id}");
        }

        [Route]
        private void On(PageErrorCommand command)
        {
            IsVisible = false;
            PageError = command;
        }
    }
}