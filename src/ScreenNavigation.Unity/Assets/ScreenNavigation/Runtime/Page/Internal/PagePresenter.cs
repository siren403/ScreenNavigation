using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameKit.Common.Results;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Errors;

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

        public async UniTask<FastResult<Void>> ShowPageAsync(PageEntry entry, CancellationToken ct = default)
        {
            var (id, _, router) = entry;
            try
            {
                await router.PublishAsync(new ShowCommand(), ct);
                _rendering.Add(id);
                return FastResult.Ok;
            }
            catch
            {
                _ = router.PublishAsync(new PageErrorCommand(
                    pageId: id,
                    operation: PageOperation.None,
                    errorCode: PageErrorCodes.ShowFailed,
                    message: $"Failed to show page '{id}'"
                ), ct);
                return FastResult<Void>.Fail(PageErrorCodes.ShowFailed, $"Failed to show page '{id}'");
            }
        }

        public void HidePage(PageEntry entry)
        {
            var (id, _, router) = entry;
            if (_rendering.Remove(id))
            {
                _ = router.PublishAsync(new HideCommand());
            }
        }

        internal bool IsRendering(string id)
        {
            return _rendering.Contains(id);
        }
    }
}