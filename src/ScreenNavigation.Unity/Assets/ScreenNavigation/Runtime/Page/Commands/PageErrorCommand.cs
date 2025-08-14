using System;
using VitalRouter;

namespace ScreenNavigation.Page.Commands
{
    public readonly struct PageErrorCommand : ICommand
    {
        public readonly Exception Exception;

        internal PageErrorCommand(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception), "Exception cannot be null");
        }
    }
}