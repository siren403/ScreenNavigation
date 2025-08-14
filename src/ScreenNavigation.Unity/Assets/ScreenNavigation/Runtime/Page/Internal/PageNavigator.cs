using Cysharp.Threading.Tasks;
using GameKit.Common.Results;
using Microsoft.Extensions.Logging;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Errors;
using VitalRouter;

namespace ScreenNavigation.Page.Internal
{
    [Routes(CommandOrdering.Drop)]
    internal partial class PageNavigator
    {
        private readonly PagePresenter _presenter;
        private readonly PageStack _stack;
        private readonly PageRegistry _registry;
        private readonly Router _router;
        private readonly ILogger<PageNavigator> _logger;

        public PageNavigator(
            PagePresenter presenter,
            PageStack stack,
            PageRegistry registry,
            Router router,
            ILogger<PageNavigator> logger
        )
        {
            _presenter = presenter;
            _stack = stack;
            _registry = registry;
            _router = router;
            _logger = logger;
        }

        public bool IsCurrentPage(string pageId)
        {
            return _presenter.IsRendering(pageId) && AlreadyCurrentPage(pageId);
        }

        private bool AlreadyCurrentPage(string pageId)
        {
            var result = _stack.TryPeek(out var currentPageId) && currentPageId == pageId;

            if (result)
            {
                _logger.LogDebug("Cannot replace with the same page.");
                _ = _router.PublishAsync(new PageErrorCommand(
                    pageId: pageId,
                    operation: PageOperation.None,
                    errorCode: PageErrorCodes.AlreadyCurrent,
                    message: $"Already on page '{pageId}'"
                ));
            }

            return result;
        }

        [Route]
        private async UniTask On(ToPageCommand command, PublishContext context)
        {
            var newPageId = command.PageId;
            // 1. 중복 체크 (Navigator가 Stack 상태 확인)
            if (AlreadyCurrentPage(newPageId))
            {
                return;
            }

            var newPageResult = await _registry.GetPageAsync(newPageId);
            if (newPageResult.IsError)
            {
                _ = _router.PublishAsync(new PageErrorCommand(
                    pageId: newPageId,
                    operation: PageOperation.To,
                    errorCode: PageErrorCodes.NotFound,
                    message: $"Page '{newPageId}' not found: {newPageResult}"
                ));
                return;
            }

            var newPage = newPageResult.Value;

            // 3. 모든 기존 페이지 숨기기 + Stack 조작
            while (_stack.TryPop(out var id))
            {
                var popPageResult = await _registry.GetPageAsync(id);
                if (popPageResult.IsError)
                {
                    continue;
                }

                var popPage = popPageResult.Value;
                _presenter.HidePage(popPage);
            }

            // 4. 새 페이지 표시 + Stack에 추가
            var showResult = await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            if (showResult.IsError)
            {
                return;
            }

            _stack.Push(newPageId);
        }


        [Route]
        private async UniTask On(BackPageCommand command, PublishContext context)
        {
            if (_stack.TryPop(out var id))
            {
                var backPageResult = await _registry.GetPageAsync(id);
                if (backPageResult.IsError)
                {
                    _ = _router.PublishAsync(new PageErrorCommand(
                        pageId: id,
                        operation: PageOperation.Back,
                        errorCode: PageErrorCodes.NotFound,
                        message: $"Back page '{id}' not found: {backPageResult}"
                    ));
                    return;
                }

                var backPage = backPageResult.Value;
                _presenter.HidePage(backPage);
            }
            else
            {
                _logger.LogDebug("No page to go back to.");
                return;
            }

            if (_stack.TryPeek(out var nextId))
            {
                var nextPageResult = await _registry.GetPageAsync(nextId);
                if (nextPageResult.IsError)
                {
                    _ = _router.PublishAsync(new PageErrorCommand(
                        pageId: nextId,
                        operation: PageOperation.Back,
                        errorCode: PageErrorCodes.NotFound,
                        message: $"Next page '{nextId}' not found: {nextPageResult}"
                    ));
                    return;
                }

                var nextPage = nextPageResult.Value;
                var showResult = await _presenter.ShowPageAsync(nextPage, context.CancellationToken);
                if (showResult.IsError)
                {
                    return;
                }
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

            if (AlreadyCurrentPage(newPageId))
            {
                return;
            }

            var newPageResult = await _registry.GetPageAsync(newPageId);
            if (newPageResult.IsError)
            {
                _ = _router.PublishAsync(new PageErrorCommand(
                    pageId: newPageId,
                    operation: PageOperation.Push,
                    errorCode: PageErrorCodes.NotFound,
                    message: $"Page '{newPageId}' not found: {newPageResult}"
                ));
                return;
            }

            var newPage = newPageResult.Value;

            if (_stack.TryPeek(out var currentId))
            {
                if (currentId == newPageId)
                {
                    _logger.LogDebug("Cannot push the same page again.");
                    return;
                }

                var currentPageResult = await _registry.GetPageAsync(currentId);
                if (currentPageResult.IsError)
                {
                    _ = _router.PublishAsync(new PageErrorCommand(
                        pageId: currentId,
                        operation: PageOperation.Push,
                        errorCode: PageErrorCodes.NotFound,
                        message: $"Current page '{currentId}' not found: {currentPageResult}"
                    ));
                    return;
                }

                var currentPage = currentPageResult.Value;
                _presenter.HidePage(currentPage);
            }

            var showResult = await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            if (showResult.IsError)
            {
                return;
            }

            _stack.Push(newPageId);
        }

        [Route]
        private async UniTask On(ReplacePageCommand command, PublishContext context)
        {
            var newPageId = command.PageId;
            if (AlreadyCurrentPage(newPageId))
            {
                return;
            }

            var newPageResult = await _registry.GetPageAsync(newPageId);
            if (newPageResult.IsError)
            {
                _ = _router.PublishAsync(new PageErrorCommand(
                    pageId: newPageId,
                    operation: PageOperation.Replace,
                    errorCode: PageErrorCodes.NotFound,
                    message: $"Page '{newPageId}' not found: {newPageResult}"
                ));
                return;
            }

            var newPage = newPageResult.Value;

            if (_stack.TryPop(out var oldId))
            {
                var oldPageResult = await _registry.GetPageAsync(oldId);
                if (oldPageResult.IsError)
                {
                    _ = _router.PublishAsync(new PageErrorCommand(
                        pageId: oldId,
                        operation: PageOperation.Replace,
                        errorCode: PageErrorCodes.NotFound,
                        message: $"Old page '{oldId}' not found: {oldPageResult}"
                    ));
                    return;
                }

                var oldPage = oldPageResult.Value;
                _presenter.HidePage(oldPage);
            }
            else
            {
                _logger.LogDebug("No page to replace.");
            }

            var showResult = await _presenter.ShowPageAsync(newPage, context.CancellationToken);
            if (showResult.IsError)
            {
                return;
            }

            _stack.Push(newPageId);
        }
    }
}