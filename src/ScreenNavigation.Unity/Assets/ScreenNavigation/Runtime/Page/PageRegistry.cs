using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ScreenNavigation.Page
{
    public class PageRegistry : IDisposable
    {
        /// <summary>
        /// Id: IPage
        /// </summary>
        private readonly Dictionary<string, IPage> _cachedPages = new();
        /// <summary>
        /// Id: Addressable Key
        /// </summary>
        private readonly Dictionary<string, AsyncLazy<IPage>> _addressableFactories = new();
        private readonly List<AsyncOperationHandle> _loadedHandles = new();

        public IEnumerable<IPage> CachedPages => _cachedPages.Values;

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

            _cachedPages[pageId] = page ?? throw new ArgumentNullException(nameof(page), "Page cannot be null.");
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

        public async UniTask<IPage> GetPageAsync(string pageId)
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
            _cachedPages[pageId] = page ?? throw new Exception(
                $"Failed to create page with ID '{pageId}'. Factory returned null."
            );
            return page;
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

            _cachedPages.Clear();
            _addressableFactories.Clear();
            _loadedHandles.Clear();
        }
    }
}