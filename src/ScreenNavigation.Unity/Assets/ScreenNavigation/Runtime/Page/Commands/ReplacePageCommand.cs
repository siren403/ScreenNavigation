using VitalRouter;

namespace ScreenNavigation.Page.Commands
{
    public struct ReplacePageCommand : ICommand
    {
        public readonly string PageId;

        public ReplacePageCommand(string pageId)
        {
            PageId = pageId;
        }
    }
}