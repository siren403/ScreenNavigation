using Samples.PageQuickStart.Pages;
using ScreenNavigation.Extensions;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Extensions;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter;

namespace Samples.PageQuickStart
{
    public class PageLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterPages(pages =>
            {
                pages.InHierarchy<LoginPage>(PageIds.Login);
                pages.InHierarchy<HomePage>(PageIds.Home);
                pages.InAddressable<SettingsPage, PageRoot>(
                    PageIds.Settings,
                    "Assets/Samples/PageQuickStart/Prefabs/SettingsPage.prefab"
                );
            });

            builder.RegisterEntryPoint<EntryPoint>();
        }

        class EntryPoint : ITickable
        {
            private readonly Router _router;

            public EntryPoint(Router router)
            {
                _router = router;
            }

            public void Initialize()
            {
                // _router.PublishAsync(new ToPageCommand(PageIds.Login));
            }

            public void Tick()
            {
                #region To

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    _router.ToPageAsync(PageIds.Login);
                }

                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    _router.ToPageAsync(PageIds.Home);
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    _router.ToPageAsync(PageIds.Settings);
                }

                #endregion

                #region Push

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    _router.PushPageAsync(PageIds.Login);
                }

                if (Input.GetKeyDown(KeyCode.W))
                {
                    _router.PushPageAsync(PageIds.Home);
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    _router.PushPageAsync(PageIds.Settings);
                }

                #endregion

                #region Replace

                if (Input.GetKeyDown(KeyCode.A))
                {
                    _router.ReplacePageAsync(PageIds.Login);
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    _router.ReplacePageAsync(PageIds.Home);
                }

                if (Input.GetKeyDown(KeyCode.D))
                {
                    _router.ReplacePageAsync(PageIds.Settings);
                }

                #endregion

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _router.BackPageAsync();
                }
            }
        }
    }
}