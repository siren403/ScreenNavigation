using VitalRouter;

namespace ScreenNavigation.Page.Commands
{
    public readonly struct ToPageCommand : ICommand
    {
        public readonly string PageId;

        public ToPageCommand(string pageId)
        {
            PageId = pageId;
        }
    }

}