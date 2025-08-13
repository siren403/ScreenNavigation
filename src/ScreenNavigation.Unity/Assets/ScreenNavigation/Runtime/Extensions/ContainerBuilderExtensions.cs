using System;
using ScreenNavigation.Page;
using UnityEngine;
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
            builder.Register<PageStack>(Lifetime.Singleton);

            builder.RegisterVitalRouter(routing =>
            {
                routing.Map<PagePresenter>();
            });

            var pages = new PagesBuilder(builder);
            configuration?.Invoke(pages);

            builder.RegisterBuildCallback(container =>
            {
                var presenter = container.Resolve<PagePresenter>();
                presenter.Initialize();
            });
        }

        public class PagesBuilder
        {
            private readonly IContainerBuilder _builder;

            public PagesBuilder(IContainerBuilder builder)
            {
                _builder = builder;
            }

            public void InHierarchy<T>(string id) where T : IPage
            {
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