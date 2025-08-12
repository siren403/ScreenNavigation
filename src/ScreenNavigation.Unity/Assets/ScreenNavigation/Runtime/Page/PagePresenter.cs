using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ScreenNavigation.Page.Commands;
using UnityEngine;
using VitalRouter;

namespace ScreenNavigation.Page
{
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

        public void AllHide()
        {
            foreach (var page in _registry.CachedPages)
            {
                page.OnHide();

            }
            _rendering.Clear();
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(ToPageCommand command, PublishContext context)
        {
            var newPage = await _registry.GetPageAsync(command.PageId);

            while (_stack.TryPop(out var id))
            {
                var popPage = await _registry.GetPageAsync(id);
                popPage.OnHide();
                _rendering.Remove(id);
            }

            newPage.OnShow();
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(BackPageCommand command, PublishContext context)
        {
            if (_stack.TryPop(out var id))
            {
                var backPage = await _registry.GetPageAsync(id);
                backPage.OnHide();
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
                nextPage.OnShow();
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
                var currentPage = await _registry.GetPageAsync(currentId);
                currentPage.OnHide();
                _rendering.Remove(currentId);
            }

            newPage.OnShow();
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }

        [Route(CommandOrdering.Drop)]
        private async UniTask On(ReplacePageCommand command, PublishContext context)
        {
            var newPage = await _registry.GetPageAsync(command.PageId);

            if (_stack.TryPop(out var oldId))
            {
                var oldPage = await _registry.GetPageAsync(oldId);
                oldPage.OnHide();
                _rendering.Remove(oldId);
            }
            else
            {
                Debug.LogWarning("No page to replace.");
            }

            newPage.OnShow();
            _rendering.Add(command.PageId);
            _stack.Push(command.PageId);
        }
    }
}