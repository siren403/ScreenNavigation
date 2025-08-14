using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VitalRouter;

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

        public void AddPage(string pageId, IPage page)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                throw new ArgumentException("Page ID cannot be null or empty.", nameof(pageId));
            }

            if (_cachedPages.ContainsKey(pageId))
            {
                throw new InvalidOperationException($"Page with ID '{pageId}' already exists in the registry.");
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
                throw new ArgumentException("Page ID cannot be null or empty.", nameof(pageId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Addressable key cannot be null or empty.", nameof(key));
            }

            var factory = UniTask.Lazy(async () =>
            {
                Debug.Log($"Adding page with ID '{pageId}'.");
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

            if (!_addressableFactories.TryAdd(pageId, factory))
            {
                throw new InvalidOperationException($"Page with ID '{pageId}' already exists in the registry.");
            }
        }

        public async UniTask<PageEntry> GetPageAsync(string pageId)
        {
            if (_cachedPages.TryGetValue(pageId, out var cached))
            {
                return cached;
            }

            if (!_addressableFactories.TryGetValue(pageId, out var factory))
            {
                throw new KeyNotFoundException($"Page with ID '{pageId}' not found in registry.");
            }

            var page = await factory;
            var router = new Router(CommandOrdering.Drop);
            page.MapTo(router);
            var entry = new PageEntry(
                pageId,
                page,
                router
            );
            _cachedPages[pageId] = entry;
            return entry;
        }

        public void Dispose()
        {
            foreach (var handle in _loadedHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
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