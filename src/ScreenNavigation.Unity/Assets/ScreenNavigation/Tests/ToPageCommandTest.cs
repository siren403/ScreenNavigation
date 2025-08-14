using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ScreenNavigation.Extensions;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Errors;
using ScreenNavigation.Page.Extensions;
using ScreenNavigation.Page.Internal;
using UnityEngine.TestTools;
using VContainer;
using VitalRouter;

namespace ScreenNavigation.Tests
{
    public class ToPageCommandTest
    {
        #region 기본 성공 시나리오

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
            Assert.IsTrue(navigator.IsCurrentPage(toPage.Id));
        });

        [UnityTest]
        public IEnumerator ToPageCommand_빈스택에서페이지이동_성공() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var toPage = new MockPage { Id = MockPage.Login };
            builder.RegisterPages(pages => { pages.InMemory(toPage.Id, toPage); });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();
            var stack = container.Resolve<PageStack>();

            // 스택이 비어있는지 확인
            Assert.IsFalse(stack.TryPeek(out _), "스택은 처음에 비어있어야 함");

            // Act
            await router.ToPageAsync(toPage.Id);

            // Assert
            Assert.IsTrue(toPage.IsVisible);
            Assert.IsTrue(navigator.IsCurrentPage(toPage.Id));
            Assert.IsTrue(stack.TryPeek(out var topPageId), "스택에 페이지가 추가되어야 함");
            Assert.AreEqual(toPage.Id, topPageId, "새 페이지가 스택 최상단에 있어야 함");
        });

        [UnityTest]
        public IEnumerator ToPageCommand_여러페이지스택클리어_성공() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var page1 = new MockPage { Id = MockPage.Login };
            var page2 = new MockPage { Id = MockPage.Home };
            var page3 = new MockPage { Id = MockPage.Settings };
            builder.RegisterPages(pages =>
            {
                pages.InMemory(page1.Id, page1);
                pages.InMemory(page2.Id, page2);
                pages.InMemory(page3.Id, page3);
            });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();
            var stack = container.Resolve<PageStack>();

            // 여러 페이지를 Push로 쌓기
            await router.ToPageAsync(page1.Id);
            Assert.IsTrue(page1.IsVisible, "page1이 먼저 표시됨");
            Assert.IsFalse(page2.IsVisible, "page2는 아직 표시되지 않음");

            await router.PushPageAsync(page2.Id);
            Assert.IsFalse(page1.IsVisible, "Push 후 page1은 숨겨짐 (한번에 하나의 페이지만 표시)");
            Assert.IsTrue(page2.IsVisible, "Push 후 page2만 표시됨");

            // Act - To로 새 페이지 이동 (모든 스택 클리어)
            await router.ToPageAsync(page3.Id);

            // Assert
            Assert.IsFalse(page1.IsVisible, "이전 페이지들 숨겨짐");
            Assert.IsFalse(page2.IsVisible);
            Assert.IsTrue(page3.IsVisible, "새 페이지만 표시");
            Assert.IsTrue(navigator.IsCurrentPage(page3.Id));
            Assert.IsFalse(navigator.IsCurrentPage(page1.Id));
            Assert.IsFalse(navigator.IsCurrentPage(page2.Id));

            // 스택 상태 확인
            Assert.IsTrue(stack.TryPeek(out var topPageId), "스택에 새 페이지가 있어야 함");
            Assert.AreEqual(page3.Id, topPageId, "새 페이지가 스택 최상단에 있어야 함");
        });

        #endregion

        #region 중복 방지 로직

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

            PageErrorCommand? receivedError = null;
            using var subscribe = router.Subscribe<PageErrorCommand>((error, ctx) => { receivedError = error; });

            // 첫 번째 이동
            await router.ToPageAsync(toPage.Id);
            Assert.IsTrue(toPage.IsVisible, "첫 번째 이동 후 페이지가 표시됨");
            Assert.IsTrue(navigator.IsCurrentPage(toPage.Id), "첫 번째 이동 후 현재 페이지가 됨");

            // Act - 동일한 페이지로 다시 이동 시도
            await router.ToPageAsync(toPage.Id);
            await UniTask.Yield(); // 에러 이벤트 처리 대기

            // Assert
            // 중복 이동이 무시되었으므로 페이지는 여전히 표시된 상태여야 함
            Assert.IsTrue(toPage.IsVisible, "중복 이동 무시되어 페이지는 계속 표시됨");
            Assert.IsTrue(navigator.IsCurrentPage(toPage.Id), "여전히 현재 페이지");

            // PageErrorCommand 검증
            Assert.IsNotNull(receivedError);
            Assert.AreEqual(MockPage.Login, receivedError.Value.PageId);
            Assert.AreEqual(PageOperation.None, receivedError.Value.Operation);
            Assert.AreEqual(PageErrorCodes.AlreadyCurrent, receivedError.Value.ErrorCode);
            Assert.That(receivedError.Value.Message, Does.Contain("Already on page"));
        });

        #endregion

        #region 에러 처리

        [UnityTest]
        public IEnumerator ToPageCommand_존재하지않는페이지_PageErrorCommand발행() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            builder.RegisterPages(pages => { }); // 빈 페이지 등록
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();

            PageErrorCommand? receivedError = null;
            using var subscribe = router.Subscribe<PageErrorCommand>((error, ctx) => { receivedError = error; });

            // Act - 존재하지 않는 페이지로 이동 시도
            await router.ToPageAsync("NonExistentPage");
            await UniTask.Yield(); // 에러 이벤트 처리 대기

            // Assert
            Assert.IsFalse(navigator.IsCurrentPage("NonExistentPage"), "페이지가 등록되지 않음");

            // PageErrorCommand 검증
            Assert.IsNotNull(receivedError);
            Assert.AreEqual("NonExistentPage", receivedError.Value.PageId);
            Assert.AreEqual(PageOperation.To, receivedError.Value.Operation);
            Assert.AreEqual(PageErrorCodes.NotFound, receivedError.Value.ErrorCode);
            Assert.That(receivedError.Value.Message, Does.Contain("not found"));
        });

        [UnityTest]
        public IEnumerator ToPageCommand_페이지Show실패_예외발생() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var failingPage = new FailingMockPage
            {
                Id = FailingMockPage.Failing,
                Mode = FailingMockPage.FailureMode.ShowThrows
            };
            builder.RegisterPages(pages => { pages.InMemory(failingPage.Id, failingPage); });
            var container = builder.Build();

            var navigator = container.Resolve<PageNavigator>();
            var router = container.Resolve<Router>();
            var stack = container.Resolve<PageStack>();

            // Act - Show 시 예외가 발생하는 페이지로 이동
            await router.ToPageAsync(failingPage.Id);

            // Assert
            // Show 실패 시 마치 ToPageCommand가 실행되지 않은 것처럼 상태 확인
            Assert.IsFalse(failingPage.IsVisible, "Show 실패로 IsVisible = false");
            Assert.IsFalse(navigator.IsCurrentPage(failingPage.Id), "현재 페이지로 등록되지 않음");

            // 스택 상태 확인 - Show 실패 시 스택에 추가되지 않아야 함
            Assert.IsFalse(stack.TryPeek(out var topPageId), "Show 실패 시 스택에 추가되지 않아야 함");

            // PageErrorCommand가 발생했는지 확인
            Assert.AreEqual(failingPage.Id, failingPage.PageError.PageId);
            Assert.AreEqual(PageOperation.None, failingPage.PageError.Operation);
            Assert.AreEqual(PageErrorCodes.ShowFailed, failingPage.PageError.ErrorCode);
        });

        #endregion

        #region 스택 상태 검증

        [UnityTest]
        public IEnumerator ToPageCommand_스택상태정확히관리됨() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var page1 = new MockPage { Id = MockPage.Login };
            var page2 = new MockPage { Id = MockPage.Home };
            var page3 = new MockPage { Id = MockPage.Settings };
            builder.RegisterPages(pages =>
            {
                pages.InMemory(page1.Id, page1);
                pages.InMemory(page2.Id, page2);
                pages.InMemory(page3.Id, page3);
            });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var stack = container.Resolve<PageStack>();

            // 초기 상태: 빈 스택
            Assert.IsFalse(stack.TryPeek(out _), "초기에는 빈 스택");

            // Act & Assert - 페이지 1 이동
            await router.ToPageAsync(page1.Id);
            Assert.IsTrue(stack.TryPeek(out var current1), "페이지 1이 스택에 추가됨");
            Assert.AreEqual(page1.Id, current1);

            // Act & Assert - 페이지 2로 Push (스택에 2개)
            await router.PushPageAsync(page2.Id);
            Assert.IsTrue(stack.TryPeek(out var current2), "페이지 2가 스택 최상단");
            Assert.AreEqual(page2.Id, current2);

            // Act & Assert - 페이지 3으로 To (스택 클리어 후 1개)
            await router.ToPageAsync(page3.Id);
            Assert.IsTrue(stack.TryPeek(out var current3), "페이지 3만 스택에 남음");
            Assert.AreEqual(page3.Id, current3);

            // 스택 크기 확인 (내부 상태 검증)
            var stackSize = 0;
            while (stack.TryPop(out _)) stackSize++;
            Assert.AreEqual(1, stackSize, "To 후에는 스택에 1개 페이지만 있어야 함");
        });

        #endregion

        #region 중간 단계 실패 시나리오

        [UnityTest]
        public IEnumerator ToPageCommand_기존페이지Hide중실패_새페이지여전히표시됨() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var failingPage = new FailingMockPage
            {
                Id = FailingMockPage.Failing,
                Mode = FailingMockPage.FailureMode.HideThrows
            };
            var normalPage = new MockPage { Id = MockPage.Home };
            builder.RegisterPages(pages =>
            {
                pages.InMemory(failingPage.Id, failingPage);
                pages.InMemory(normalPage.Id, normalPage);
            });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();

            // 첫 번째로 실패하는 페이지 표시
            await router.ToPageAsync(failingPage.Id);
            Assert.IsTrue(failingPage.IsVisible, "첫 번째 페이지 표시됨");

            // Act - 새 페이지로 이동 (기존 페이지 Hide 실패)
            await router.ToPageAsync(normalPage.Id);

            // Assert
            // Hide가 실패해도 새 페이지는 정상 표시되어야 함
            Assert.IsTrue(normalPage.IsVisible, "새 페이지는 정상 표시됨");
            Assert.IsTrue(navigator.IsCurrentPage(normalPage.Id), "새 페이지가 현재 페이지");

            // ⚠️ 실제로는 Hide 실패로 인해 두 페이지가 동시에 보일 수 있지만,
            // 이는 비정상 상황임을 명시적으로 표시
            // Navigator 관점에서는 새 페이지만 현재 페이지로 간주
            Assert.IsFalse(navigator.IsCurrentPage(failingPage.Id), "이전 페이지는 더 이상 현재 페이지가 아님");

            // 참고: failingPage.IsVisible이 여전히 true일 수 있음 (Hide 실패로 인해)
            // 하지만 이는 예외적인 에러 상황으로, 정상적으로는 한번에 하나의 페이지만 표시되어야 함
        });

        #endregion

        #region 동시성 제어

        [UnityTest]
        public IEnumerator ToPageCommand_작업중취소_정상처리됨() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var builder = new ContainerBuilder();
            var slowPage = new MockPage { Id = MockPage.Login };
            builder.RegisterPages(pages => { pages.InMemory(slowPage.Id, slowPage); });
            var container = builder.Build();

            var router = container.Resolve<Router>();
            var navigator = container.Resolve<PageNavigator>();
            var stack = container.Resolve<PageStack>();

            using var cts = new CancellationTokenSource();

            // Act - 작업 시작 후 즉시 취소
            var task = router.ToPageAsync(slowPage.Id, cts.Token);
            await UniTask.DelayFrame(1);
            cts.Cancel();

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 취소 예외는 정상적인 동작
            }

            // Assert
            // 취소된 페이지는 Navigator에서 현재 페이지로 인식되지 않아야 함
            Assert.IsFalse(navigator.IsCurrentPage(slowPage.Id), "취소된 페이지는 현재 페이지가 아니어야 함");
            
            // IsVisible은 true일 수 있음 (ShowCommand에서 IsVisible = true 실행 후 DelayFrame에서 취소됨)
            // 하지만 시스템 관점에서는 취소된 것으로 처리
            
            // 스택 상태 확인 - 취소된 페이지는 스택에 추가되지 않아야 함
            Assert.IsFalse(stack.TryPeek(out _), "취소된 페이지는 스택에 추가되지 않아야 함");
            
            // 시스템이 안정적이어야 함 (예외 없이 동작)
            Assert.DoesNotThrow(() => navigator.IsCurrentPage(slowPage.Id), "시스템이 안정적이어야 함");
        });

        #endregion
    }
}