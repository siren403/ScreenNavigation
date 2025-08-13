using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ScreenNavigation.Page.Commands;
using UnityEngine;
using VContainer.Unity;
using VitalRouter;

namespace ScreenNavigation.Page
{
    public readonly struct ShowCommand : ICommand
    {

    }

    public readonly struct HideCommand : ICommand
    {

    }

    [Routes]
    [Filter(typeof(RenderingLogging))]
    public partial class PagePresenter
    {
        public class RenderingLogging : ICommandInterceptor
        {
            private readonly PagePresenter _presenter;

            public RenderingLogging(PagePresenter presenter)
            {
                _presenter = presenter;
            }

            public async ValueTask InvokeAsync<T>(T command, PublishContext context, PublishContinuation<T> next)
                where T : ICommand
            {
                await next(command, context);
                Debug.Log($"{_presenter._rendering.Count}");
            }
        }

        private readonly PageRegistry _registry;
        private readonly PageStack _stack;
        private readonly HashSet<string> _rendering = new();

        public PagePresenter(PageRegistry registry, PageStack stack)
        {
            _registry = registry;
            _stack = stack;
        }

        public void Initialize()
        {
            foreach (var page in _registry.CachedPages)
            {
                _ = page.Router;
            }
            _rendering.Clear();
        }

        public void AllHide()
        {
            foreach (var page in _registry.CachedPages)
            {
                page.Router.PublishAsync(new HideCommand());
            }
            _rendering.Clear();
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(ToPageCommand command, PublishContext context)
        {
            if (_stack.TryPeek(out var currentId) && currentId == command.PageId)
            {
                Debug.LogWarning("Cannot replace with the same page.");
                return;
            }

            var newPage = await _registry.GetPageAsync(command.PageId);

            while (_stack.TryPop(out var id))
            {
                var popPage = await _registry.GetPageAsync(id);
                _ = popPage.Router.PublishAsync(new HideCommand());
                _rendering.Remove(id);
            }

            await newPage.Router.PublishAsync(new ShowCommand(), context.CancellationToken);
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(BackPageCommand command, PublishContext context)
        {
            if (_stack.TryPop(out var id))
            {
                var backPage = await _registry.GetPageAsync(id);
                _ = backPage.Router.PublishAsync(new HideCommand());
                _rendering.Remove(id);
            }
            else
            {
                Debug.LogWarning("No page to go back to.");
                return;
            }

            if (_stack.TryPeek(out var nextId))
            {
                var nextPage = await _registry.GetPageAsync(nextId);
                await nextPage.Router.PublishAsync(new ShowCommand(), context.CancellationToken);
                _rendering.Add(nextId);
            }
            else
            {
                Debug.LogWarning("No next page to show after going back.");
            }
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(PushPageCommand command, PublishContext context)
        {
            var newPage = await _registry.GetPageAsync(command.PageId);

            if (_stack.TryPeek(out var currentId))
            {
                if (currentId == command.PageId)
                {
                    Debug.LogWarning("Cannot push the same page again.");
                    return;
                }

                var currentPage = await _registry.GetPageAsync(currentId);
                _ = currentPage.Router.PublishAsync(new HideCommand());
                _rendering.Remove(currentId);
            }

            await newPage.Router.PublishAsync(new ShowCommand(), context.CancellationToken);
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(ReplacePageCommand command, PublishContext context)
        {
            if (_stack.TryPeek(out var currentId) && currentId == command.PageId)
            {
                Debug.LogWarning("Cannot replace with the same page.");
                return;
            }

            var newPage = await _registry.GetPageAsync(command.PageId);

            if (_stack.TryPop(out var oldId))
            {
                var oldPage = await _registry.GetPageAsync(oldId);
                _ = oldPage.Router.PublishAsync(new HideCommand());
                _rendering.Remove(oldId);
            }
            else
            {
                Debug.LogWarning("No page to replace.");
            }

            await newPage.Router.PublishAsync(new ShowCommand(), context.CancellationToken);
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }

    }
}