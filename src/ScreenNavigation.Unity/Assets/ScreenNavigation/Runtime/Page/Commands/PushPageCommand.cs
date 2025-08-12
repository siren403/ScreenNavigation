using VitalRouter;

namespace ScreenNavigation.Page.Commands
{
    public struct PushPageCommand : ICommand
    {
        public readonly string PageId;

        public PushPageCommand(string pageId)
        {
            PageId = pageId;
        }
    }
}