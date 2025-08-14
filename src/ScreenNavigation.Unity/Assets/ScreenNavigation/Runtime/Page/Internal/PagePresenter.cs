using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ScreenNavigation.Page.Commands;

namespace ScreenNavigation.Page.Internal
{
    internal class PagePresenter
    {
        private readonly HashSet<string> _rendering = new();

        public void Initialize(IEnumerable<PageEntry> pages)
        {
            foreach (var (_, page, _) in pages)
            {
                page.IsVisible = false;
            }

            _rendering.Clear();
        }

        public async UniTask ShowPageAsync(PageEntry entry, CancellationToken ct = default)
        {
            var (id, _, router) = entry;
            await router.PublishAsync(new ShowCommand(), ct);
            _rendering.Add(id);
        }

        public UniTask HidePageAsync(PageEntry entry)
        {
            var (id, _, router) = entry;
            if (_rendering.Remove(id))
            {
                _ = router.PublishAsync(new HideCommand());
            }

            return UniTask.CompletedTask;
        }

        internal bool IsRendering(string id)
        {
            return _rendering.Contains(id);
        }
    }
}