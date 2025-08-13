using Cysharp.Threading.Tasks;
using LitMotion.Animation;
using ScreenNavigation.Page;
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

        private Router _router;

        public Router Router
        {
            get
            {
                if (_router != null)
                {
                    return _router;
                }

                _router = new Router(CommandOrdering.Drop);
                _router.AddTo(destroyCancellationToken);
                MapTo(_router);
                IsVisible = false;
                return _router;
            }
        }

        private bool IsVisible
        {
            set
            {
                GetComponent<Canvas>().enabled = value;
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
        }

        [Route]
        private async UniTask On(ShowCommand command)
        {
            GetComponent<Canvas>().enabled = true;
            if (showAnimation != null)
            {
                showAnimation.Play();
                await UniTask.WaitWhile(
                    showAnimation,
                    motion => motion.IsPlaying,
                    cancellationToken: destroyCancellationToken
                );
                showAnimation.Stop();
            }
            Debug.Log($"Page {name} is shown.");
        }

        [Route]
        private async UniTask On(HideCommand command)
        {
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
            Debug.Log($"Page {name} is hidden.");
            GetComponent<Canvas>().enabled = false;
        }

    }
}