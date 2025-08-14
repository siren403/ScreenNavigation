using System.Collections.Generic;
using GameKit.Common.Results;

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

        public FastResult<string> Pop()
        {
            if (_stack.Count == 0)
            {
                return FastResult<string>.Fail("Page.EmptyStack");
            }

            return FastResult<string>.Ok(_stack.Pop());
        }

        public bool TryPop(out string pageId)
        {
            if (Any)
            {
                var popResult = Pop();
                if (!popResult.IsError)
                {
                    pageId = popResult.Value;
                    return true;
                }
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