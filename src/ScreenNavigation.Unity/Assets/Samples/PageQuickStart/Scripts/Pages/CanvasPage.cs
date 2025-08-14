using System;
using Cysharp.Threading.Tasks;
using LitMotion.Animation;
using ScreenNavigation.Page;
using ScreenNavigation.Page.Commands;
using UnityEngine;
using VitalRouter;

namespace Samples.PageQuickStart.Pages
{
    [RequireComponent(typeof(Canvas))]
    [Routes]
    public abstract partial class CanvasPage : MonoBehaviour, IPage
    {
        [SerializeField] private LitMotionAnimation showAnimation;
        [SerializeField] private LitMotionAnimation hideAnimation;

        [SerializeField] private bool throwOnShow = false;
        [SerializeField] private bool throwOnHide = false;

        public bool IsVisible
        {
            set
            {
                GetComponent<Canvas>().enabled = value;
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
            get => GetComponent<Canvas>().enabled;
        }

        [Route]
        private async UniTask On(ShowCommand command)
        {
            if (throwOnShow)
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));
                throw new Exception($"Show failed for page: {name}");
            }

            if (showAnimation != null)
            {
                showAnimation.Play();
                await UniTask.NextFrame();
                GetComponent<Canvas>().enabled = true;
                await UniTask.WaitWhile(
                    showAnimation,
                    motion => motion.IsPlaying,
                    cancellationToken: destroyCancellationToken
                );
                showAnimation.Stop();
            }
            else
            {
                GetComponent<Canvas>().enabled = true;
            }
        }

        [Route]
        private async UniTask On(HideCommand command)
        {
            if (throwOnHide)
            {
                await UniTask.Delay(TimeSpan.FromMilliseconds(100));
                throw new Exception($"Hide failed for page: {name}");
            }

            if (hideAnimation != null)
            {
                hideAnimation.Play();
                await UniTask.WaitWhile(
                    hideAnimation,
                    motion => motion.IsPlaying,
                    cancellationToken: destroyCancellationToken
                );
                hideAnimation.Stop();
            }

            GetComponent<Canvas>().enabled = false;
        }

        [Route]
        private void On(PageErrorCommand command)
        {
            IsVisible = false;
            Debug.LogError($"Error on page {name}: {command.ErrorCode} - {command.Message}");
        }
    }
}