using VitalRouter;

namespace ScreenNavigation.Page
{
    public interface IPage
    {
        bool IsVisible { set; get; }
        Subscription MapTo(ICommandSubscribable subscribable);
    }
}