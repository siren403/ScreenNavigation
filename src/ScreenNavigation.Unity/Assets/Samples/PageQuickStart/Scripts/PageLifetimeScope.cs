using Samples.PageQuickStart.Pages;
using ScreenNavigation.Extensions;
using ScreenNavigation.Page.Commands;
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
                    _router.PublishAsync(new ToPageCommand(PageIds.Login));
                }

                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    _router.PublishAsync(new ToPageCommand(PageIds.Home));
                }

                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    _router.PublishAsync(new ToPageCommand(PageIds.Settings));
                }

                #endregion

                #region Push

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    _router.PublishAsync(new PushPageCommand(PageIds.Login));
                }

                if (Input.GetKeyDown(KeyCode.W))
                {
                    _router.PublishAsync(new PushPageCommand(PageIds.Home));
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    _router.PublishAsync(new PushPageCommand(PageIds.Settings));
                }

                #endregion

                #region Replace

                if (Input.GetKeyDown(KeyCode.A))
                {
                    _router.PublishAsync(new ReplacePageCommand(PageIds.Login));
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    _router.PublishAsync(new ReplacePageCommand(PageIds.Home));
                }

                if (Input.GetKeyDown(KeyCode.D))
                {
                    _router.PublishAsync(new ReplacePageCommand(PageIds.Settings));
                }

                #endregion

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _router.PublishAsync(new BackPageCommand());
                }
            }
        }
    }
}