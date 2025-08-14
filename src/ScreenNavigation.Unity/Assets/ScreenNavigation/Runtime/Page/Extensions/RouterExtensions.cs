using System;
using System.Threading;
using System.Threading.Tasks;
using ScreenNavigation.Page.Commands;
using VitalRouter;

namespace ScreenNavigation.Page.Extensions
{
    public static class RouterExtensions
    {
        public static ValueTask ToPageAsync(this Router router, string pageId, CancellationToken ct = default)
        {
            return router.PublishAsync(new ToPageCommand(pageId), ct);
        }

        public static ValueTask PushPageAsync(this Router router, string pageId, CancellationToken ct = default)
        {
            return router.PublishAsync(new PushPageCommand(pageId), ct);
        }

        public static ValueTask ReplacePageAsync(this Router router, string pageId, CancellationToken ct = default)
        {
            return router.PublishAsync(new ReplacePageCommand(pageId), ct);
        }

        public static ValueTask BackPageAsync(this Router router, CancellationToken ct = default)
        {
            return router.PublishAsync(new BackPageCommand(), ct);
        }
    }
}