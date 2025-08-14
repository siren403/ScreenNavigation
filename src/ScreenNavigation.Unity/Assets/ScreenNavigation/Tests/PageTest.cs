using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ScreenNavigation.Extensions;
using ScreenNavigation.Page;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Extensions;
using ScreenNavigation.Page.Internal;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using VitalRouter;

namespace ScreenNavigation.Tests
{
    [Routes]
    public partial class MockPage : IPage
    {
        public const string Login = "LoginPage";
        public const string Home = "HomePage";
        public const string Settings = "SettingsPage";

        public string Id { get; set; }
        public bool IsVisible { get; set; }

        [Route]
        private async UniTask On(ShowCommand command)
        {
            IsVisible = true;
            Debug.Log($"Showing page: {Id}");
            await UniTask.Yield(); // Simulate async operation
        }

        [Route]
        private async UniTask On(HideCommand command)
        {
            IsVisible = false;
            Debug.Log($"Hiding page: {Id}");
            await UniTask.Yield(); // Simulate async operation
        }
    }

    public class PageTest
    {
        [UnityTest]
        public IEnumerator ToPageCommand_새페이지로이동_성공() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var toPage = new MockPage { Id = MockPage.Login };
            builder.RegisterPages(pages => { pages.InMemory(toPage.Id, toPage); });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();

            // Act
            await router.ToPageAsync(toPage.Id);

            // Assert
            Assert.IsTrue(toPage.IsVisible);
            Assert.IsTrue(navigator.IsTopPage(toPage.Id));
        });

        [UnityTest]
        public IEnumerator ToPageCommand_동일페이지중복이동_무시됨() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var toPage = new MockPage { Id = MockPage.Login };
            builder.RegisterPages(pages => { pages.InMemory(toPage.Id, toPage); });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();

            // 첫 번째 이동
            await router.ToPageAsync(toPage.Id);
            
            // 상태 초기화 (IsVisible을 false로 설정하여 두 번째 Show 호출 확인)
            toPage.IsVisible = false;

            // Act - 동일한 페이지로 다시 이동 시도
            await router.ToPageAsync(toPage.Id);

            // Assert
            Assert.IsFalse(toPage.IsVisible); // 두 번째 Show가 호출되지 않았으므로 여전히 false
            Assert.IsTrue(navigator.IsTopPage(toPage.Id)); // 하지만 여전히 최상위 페이지
        });
    }
}