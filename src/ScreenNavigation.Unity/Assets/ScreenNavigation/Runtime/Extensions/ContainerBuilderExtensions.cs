using System;
using Microsoft.Extensions.Logging;
using ScreenNavigation.Page;
using ScreenNavigation.Page.Internal;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

namespace ScreenNavigation.Extensions
{
    public static class ContainerBuilderExtensions
    {
        public static void RegisterPages(this IContainerBuilder builder, Action<PagesBuilder> configuration)
        {
            builder.Register<PageRegistry>(Lifetime.Singleton);
            builder.Register<PagePresenter>(Lifetime.Singleton);
            builder.Register<PageStack>(Lifetime.Singleton);

            builder.RegisterVitalRouter(routing => { routing.Map<PageNavigator>(); });

            var pages = new PagesBuilder(builder);
            configuration?.Invoke(pages);

            builder.RegisterBuildCallback(container =>
            {
                var presenter = container.Resolve<PagePresenter>();
                var registry = container.Resolve<PageRegistry>();
                presenter.Initialize(registry.CachedPages);
            });

#if !USE_ZLOGGER
            builder.Register(typeof(UnityLogger<>), Lifetime.Singleton).As(typeof(ILogger<>));
#endif
        }

        public class PagesBuilder
        {
            private readonly IContainerBuilder _builder;

            public PagesBuilder(IContainerBuilder builder)
            {
                _builder = builder;
            }

            public void InMemory<T>(string id, T instance) where T : IPage
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Page ID cannot be null or empty.", nameof(id));
                }

                if (instance == null)
                {
                    throw new ArgumentNullException(nameof(instance), "Page instance cannot be null.");
                }

                _builder.RegisterBuildCallback(container =>
                {
                    var registry = container.Resolve<PageRegistry>();
                    registry.AddPage(id, instance);
                });
            }

            public void InMemory<T>(string id) where T : IPage
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Page ID cannot be null or empty.", nameof(id));
                }

                _builder.Register<T>(Lifetime.Transient).AsSelf();
                _builder.RegisterBuildCallback(container =>
                {
                    var registry = container.Resolve<PageRegistry>();
                    var page = container.Resolve<T>();
                    registry.AddPage(id, page);
                });
            }

            public void InHierarchy<T>(string id) where T : IPage
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Page ID cannot be null or empty.", nameof(id));
                }

                _builder.RegisterComponentInHierarchy<T>();
                _builder.RegisterBuildCallback(container =>
                {
                    var registry = container.Resolve<PageRegistry>();
                    var page = container.Resolve<T>();
                    registry.AddPage(id, page);
                });
            }

            public void InAddressable<T>(string id, string key) where T : IPage
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Page ID cannot be null or empty.", nameof(id));
                }

                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Addressable key cannot be null or empty.", nameof(key));
                }

                _builder.RegisterBuildCallback(container =>
                {
                    var registry = container.Resolve<PageRegistry>();
                    registry.AddPage<T>(id, key);
                });
            }

            public void InAddressable<TPage, TParent>(string id, string key)
                where TPage : IPage
                where TParent : IParentProvider
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Addressable key cannot be null or empty.", nameof(key));
                }

                if (!_builder.Exists(typeof(TPage)))
                {
                    _builder.RegisterComponentInHierarchy<TParent>();
                }

                _builder.RegisterBuildCallback(container =>
                {
                    var registry = container.Resolve<PageRegistry>();
                    var parentProvider = container.Resolve<TParent>();
                    registry.AddPage<TPage>(id, key, parentProvider.Parent);
                });
            }
        }
    }
}