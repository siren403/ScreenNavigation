using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameKit.Common.Results;
using Microsoft.Extensions.Logging;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VitalRouter;
using Void = GameKit.Common.Results.Void;

namespace ScreenNavigation.Page.Internal
{
    public readonly struct PageEntry : IDisposable
    {
        public readonly string Id;
        public readonly IPage Page;
        public readonly Router Router;

        public PageEntry(string id, IPage page, Router router)
        {
            Id = id;
            Page = page ?? throw new ArgumentNullException(nameof(page), "Page cannot be null.");
            Router = router ?? throw new ArgumentNullException(nameof(router), "Router cannot be null.");
        }

        public void Deconstruct(out string id, out IPage page, out Router router)
        {
            id = Id;
            page = Page;
            router = Router;
        }

        public void Dispose()
        {
            Router?.Dispose();
        }
    }

    internal class PageRegistry : IDisposable
    {
        private readonly ILogger<PageRegistry> _logger;

        /// <summary>
        /// Id: IPage
        /// </summary>
        private readonly Dictionary<string, PageEntry> _cachedPages = new();

        /// <summary>
        /// Id: Addressable Key
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<IPage>> _addressableFactories = new();

        private readonly List<AsyncOperationHandle> _loadedHandles = new();

        public IEnumerable<PageEntry> CachedPages => _cachedPages.Values;

        public PageRegistry(ILogger<PageRegistry> logger)
        {
            _logger = logger;
        }

        public void AddPage(string pageId, IPage page)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                _logger.LogDebug("Page.AddPage: {pageId} cannot be null or empty.", pageId);
                return;
            }

            if (_cachedPages.ContainsKey(pageId))
            {
                _logger.LogDebug("Page.AddPage: Page with ID '{pageId}' already exists in the registry.", pageId);
                return;
            }

            var router = new Router(CommandOrdering.Drop);
            page.MapTo(router);
            var entry = new PageEntry(
                pageId,
                page,
                router
            );
            _cachedPages[pageId] = entry;
        }

        public void AddPage<T>(string pageId, string key, Transform parent = null) where T : IPage
        {
            if (string.IsNullOrEmpty(pageId))
            {
                _logger.LogDebug("Page.AddPage: {pageId} cannot be null or empty.", pageId);
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                _logger.LogDebug("Page.AddPage: {key} cannot be null or empty.", key);
                return;
            }

            var factory = UniTask.Lazy(async () =>
            {
                var handle = Addressables.InstantiateAsync(key, parent);
                await handle;
                if (!handle.IsValid())
                {
                    throw new Exception(
                        $"Failed to load page with ID '{pageId}' using Addressables. Status: {handle.Status}");
                }

                _loadedHandles.Add(handle);
                return handle.Result.GetComponent<T>() as IPage;
            });

            if (_addressableFactories.TryAdd(pageId, factory))
            {
                return;
            }

            _logger.LogDebug(
                "Page.AddPage: Page with ID '{pageId}' already exists in the registry.",
                pageId
            );
        }

        public async UniTask<FastResult<PageEntry>> GetPageAsync(string pageId)
        {
            if (_cachedPages.TryGetValue(pageId, out var cached))
            {
                return FastResult<PageEntry>.Ok(cached);
            }

            if (!_addressableFactories.TryGetValue(pageId, out var factory))
            {
                return FastResult<PageEntry>.Fail(
                    "Page.GetPageAsync",
                    $"Page with ID '{pageId}' not found in registry."
                );
            }

            var router = new Router(CommandOrdering.Drop);
            try
            {
                var page = await factory;
                page.MapTo(router);
                var entry = new PageEntry(
                    pageId,
                    page,
                    router
                );
                _cachedPages[pageId] = entry;
                return FastResult<PageEntry>.Ok(entry);
            }
            catch (Exception e)
            {
                return FastResult<PageEntry>.Fail(
                    "Page.GetPage",
                    $"Failed to load page with ID '{pageId}': {e.Message}"
                );
            }
        }

        public void Dispose()
        {
            foreach (var handle in _loadedHandles.Where(handle => handle.IsValid()))
            {
                Addressables.Release(handle);
            }

            foreach (var entry in _cachedPages.Values)
            {
                entry.Dispose();
            }

            _cachedPages.Clear();
            _addressableFactories.Clear();
            _loadedHandles.Clear();
        }
    }
}