using System.Collections.Generic;

namespace ScreenNavigation.Page.Internal
{
    internal class PageStack
    {
        private readonly Stack<string> _stack = new();

        public bool Any => _stack.Count > 0;

        public void Push(string pageId)
        {
            _stack.Push(pageId);
        }

        public string Pop()
        {
            if (_stack.Count == 0)
            {
                throw new System.InvalidOperationException("Cannot pop from an empty stack.");
            }
            return _stack.Pop();
        }

        public bool TryPop(out string pageId)
        {
            if (Any)
            {
                pageId = Pop();
                return true;
            }

            pageId = null;
            return false;
        }

        public bool TryPeek(out string pageId)
        {
            if (Any)
            {
                pageId = _stack.Peek();
                return true;
            }

            pageId = null;
            return false;
        }
    }
}