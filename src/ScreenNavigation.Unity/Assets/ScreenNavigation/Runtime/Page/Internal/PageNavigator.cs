using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScreenNavigation.Page.Commands;
using VitalRouter;

namespace ScreenNavigation.Page.Internal
{
    [Routes(CommandOrdering.Drop)]
    internal partial class PageNavigator
    {
        private readonly PagePresenter _presenter;
        private readonly PageStack _stack;
        private readonly PageRegistry _registry;
        private readonly ILogger<PageNavigator> _logger;

        public PageNavigator(
            PagePresenter presenter,
            PageStack stack,
            PageRegistry registry,
            ILogger<PageNavigator> logger
        )
        {
            _presenter = presenter;
            _stack = stack;
            _registry = registry;
            _logger = logger;
        }

        public bool IsTopPage(string pageId)
        {
            return _presenter.IsRendering(pageId)
                   && _stack.TryPeek(out var topPageId)
                   && topPageId == pageId;
        }

        [Route]
        private async UniTask On(ToPageCommand command, PublishContext context)
        {
            var newPageId = command.PageId;
            // 1. 중복 체크 (Navigator가 Stack 상태 확인)
            if (_stack.TryPeek(out var currentId) && currentId == newPageId)
            {
                _logger.LogDebug("Cannot replace with the same page.");
                return;
            }

            var newPage = await _registry.GetPageAsync(newPageId);

            // 3. 모든 기존 페이지 숨기기 + Stack 조작
            while (_stack.TryPop(out var id))
            {
                var popPage = await _registry.GetPageAsync(id);
                await _presenter.HidePageAsync(popPage);
            }

            // 4. 새 페이지 표시 + Stack에 추가
            await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            _stack.Push(newPageId);
        }


        [Route]
        private async UniTask On(BackPageCommand command, PublishContext context)
        {
            if (_stack.TryPop(out var id))
            {
                var backPage = await _registry.GetPageAsync(id);
                await _presenter.HidePageAsync(backPage);
            }
            else
            {
                _logger.LogDebug("No page to go back to.");
                return;
            }

            if (_stack.TryPeek(out var nextId))
            {
                var nextPage = await _registry.GetPageAsync(nextId);
                await _presenter.ShowPageAsync(nextPage, context.CancellationToken);
            }
            else
            {
                _logger.LogDebug("No next page to show after going back.");
            }
        }


        [Route]
        private async UniTask On(PushPageCommand command, PublishContext context)
        {
            var newPageId = command.PageId;
            var newPage = await _registry.GetPageAsync(newPageId);

            if (_stack.TryPeek(out var currentId))
            {
                if (currentId == newPageId)
                {
                    _logger.LogDebug("Cannot push the same page again.");
                    return;
                }

                var currentPage = await _registry.GetPageAsync(currentId);
                await _presenter.HidePageAsync(currentPage);
            }

            await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            _stack.Push(newPageId);
        }

        [Route]
        private async UniTask On(ReplacePageCommand command, PublishContext context)
        {
            var newPageId = command.PageId;
            if (_stack.TryPeek(out var currentId) && currentId == newPageId)
            {
                _logger.LogDebug("Cannot replace with the same page.");
                return;
            }

            var newPage = await _registry.GetPageAsync(newPageId);

            if (_stack.TryPop(out var oldId))
            {
                var oldPage = await _registry.GetPageAsync(oldId);
                await _presenter.HidePageAsync(oldPage);
            }
            else
            {
                _logger.LogDebug("No page to replace.");
            }

            await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            _stack.Push(newPageId);
        }
    }
}