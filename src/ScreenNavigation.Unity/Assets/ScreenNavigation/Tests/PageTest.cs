using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ScreenNavigation.Extensions;
using ScreenNavigation.Page.Commands;
using ScreenNavigation.Page.Extensions;
using ScreenNavigation.Page.Internal;
using UnityEngine.TestTools;
using VContainer;
using VitalRouter;

namespace ScreenNavigation.Tests
{
    /// <summary>
    /// Push, Back, Replace Command 테스트
    /// (ToPageCommand 테스트는 ToPageCommandTest.cs 참조)
    /// </summary>
    public class PageTest
    {
        // TODO: PushPageCommand, BackPageCommand, ReplacePageCommand 테스트 구현 예정
        
        [Test]
        public void PageTest_구조확인_성공()
        {
            // 현재는 ToPageCommand 테스트를 ToPageCommandTest.cs로 분리함
            // 향후 이 클래스에는 다른 Command 테스트들이 추가될 예정
            Assert.Pass("PageTest 구조 확인됨");
        }
    }
}