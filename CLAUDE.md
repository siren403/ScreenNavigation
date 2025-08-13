# Unity UI Navigation 시스템 설계 문서

## 프로젝트 개요

### 목표
Unity에서 Page, Modal(Dialog/Popup/Notification) 등 뷰 단위의 네비게이션을 관리하는 라이브러리 개발

### 핵심 원칙
- **도메인별 분리**: Page와 Modal(Dialog/Popup/Notification) 각각 독립적 구현
- **이벤트 기반**: VitalRouter를 활용한 Command/Event 패턴
- **가상뷰 패턴**: Unity 의존성 분리를 위한 인터페이스 기반 설계
- **점진적 구현**: To 기능부터 시작해서 History, 상호작용 순차 추가

## 아키텍처 구조

### 도메인 계층
```
Page (기본 화면)
Modal (사용자 집중 필요한 오버레이)
├── Dialog (응답 필수, 페이지 컨텍스트 의존)
├── Popup (정보 표시, 독립적 컨텍스트)
└── Notification (시스템 알림, 자동 처리)
```


### 도메인별 구성요소
각 도메인은 다음 3요소로 구성:
- **Presenter**: 실제 표시/숨김 로직 (Unity 독립적)
- **History**: 히스토리 관리 (도메인별 특화)
- **Navigator**: VitalRouter 연동 + Presenter/History 조합

### Modal 하위 도메인 특성
- **Dialog**: 단순 Stack History, 페이지별 컨텍스트 저장
- **Popup**: 선택적 History (설정으로 on/off)
- **Notification**: Queue 기반 (History 대신 우선순위 큐)

## 1단계: 기본 To 기능 구현

### Command 정의
```csharp
// Page Commands (실제 구현)
public readonly struct ToPageCommand : ICommand
{
    public readonly string PageId;
    
    public ToPageCommand(string pageId)
    {
        PageId = pageId;
    }
}

public readonly struct PushPageCommand : ICommand
{
    public readonly string PageId;
    
    public PushPageCommand(string pageId)
    {
        PageId = pageId;
    }
}

public readonly struct BackPageCommand : ICommand
{
}

public readonly struct ReplacePageCommand : ICommand
{
    public readonly string PageId;
    
    public ReplacePageCommand(string pageId)
    {
        PageId = pageId;
    }
}
```

### Event 정의
```csharp
// 현재 구현에는 Event 시스템이 없음
// 향후 구현 예정 구조:

// Page Events
public readonly struct PageNavigatedEvent
{
    public readonly string FromPageId;
    public readonly string ToPageId;
    public readonly DateTime Timestamp;
}

// Popup Events  
public readonly struct PopupShownEvent
{
    public readonly string FromPopupId;
    public readonly string ToPopupId;
    public readonly DateTime Timestamp;
}
```

### 페이지 인터페이스 (실제 구현)
```csharp
// 현재 구현된 인터페이스
public interface IPage
{
    void OnShow();
    void OnHide();
}

// 부모 제공자 인터페이스
public interface IParentProvider
{
    Transform Parent { get; }
}
```

### 현재 구현된 핵심 컴포넌트

**PagePresenter**: VitalRouter 기반 Command 처리기
- `ToPageCommand`: 모든 페이지 클리어 후 새 페이지 이동 (중복 방지)
- `PushPageCommand`: 현재 페이지 위에 새 페이지 추가 (중복 방지)
- `BackPageCommand`: 이전 페이지로 돌아가기
- `ReplacePageCommand`: 현재 페이지를 새 페이지로 교체 (중복 방지)
- 페이지별 Router를 통한 `ShowCommand`/`HideCommand` 발행

**PageRegistry**: 페이지 등록 및 인스턴스 관리
- Addressable 시스템을 통한 동적 로딩
- 캐싱을 통한 성능 최적화

**PageStack**: 페이지 히스토리 관리
- Stack 기반 페이지 순서 추적

**IPage**: 페이지 라이프사이클 인터페이스
- 각 페이지가 자체 `Router` 소유 및 노출
- `ShowCommand`/`HideCommand`를 통한 비동기 애니메이션 처리

**CanvasPage**: IPage의 기본 구현체
- LitMotion 기반 Show/Hide 애니메이션 지원
- Canvas enabled/disabled를 통한 화면 제어
- VitalRouter 기반 Command 처리 (`[Route]` 어트리뷰트)

### 현재 아키텍처 vs 설계 문서 차이점

**실제 구현**:
- PagePresenter가 페이지 네비게이션 전담 (Command 수신, 라우팅)
- 각 페이지가 자체 Router 소유하여 Show/Hide Command 처리
- Parameters 지원 없음 (pageId만 사용)
- **비동기 방식 페이지 제어** (ShowCommand/HideCommand with UniTask)
- **애니메이션 지원** (LitMotion 통합)
- **중복 방지 로직** (동일 페이지 연속 실행 방지)

**설계 문서**:
- Controller/Navigator 분리 구조
- Event 기반 시스템
- Parameters 지원

**✅ 구현 완료된 기능**:
- VitalRouter 기반 Command 패턴
- PageStack을 통한 히스토리 관리
- Addressable을 통한 동적 로딩
- **페이지별 독립 Router 시스템**
- **비동기 Show/Hide 애니메이션**
- **중복 네비게이션 방지**

## 2단계: History 기능 추가

### PageHistory 특화 기능
- Deep Navigation (여러 단계 깊이)
- 브랜치 히스토리 (분기 네비게이션)
- 상태 저장 (스크롤 위치, 선택된 탭)
- Root Navigation (특정 페이지로 바로 돌아가기)

### PopupHistory 특화 기능
- 컨텍스트 스택 (어떤 페이지에서 열린 팝업인지)
- 레벨별 관리 (System > Important > Normal)
- 일시적 팝업 (토스트, 툴팁은 히스토리 제외)
- 페이지 연동 (페이지 변경 시 관련 팝업 정리)

### NotificationHistory 특화 기능
- 큐 관리 (FIFO 방식 대기열)
- 우선순위 큐 (Urgent > High > Normal > Low)
- 중복 제거 (동일 알림 중복 방지)
- 그룹핑 (유사 알림 묶어서 표시)

## 3단계: 도메인 간 상호작용

### ScreenCoordinator
도메인 간 상호작용 규칙을 중앙에서 관리:

1. **팝업 활성 시 페이지 전환 제한**
2. **페이지 변경 시 관련 팝업 정리**
3. **우선순위 기반 표시 제어** (Notification > Popup > Page)
4. **리소스 경합 관리**
5. **전역 상태 변화 대응** (네트워크 끊김, 백그라운드 진입)

### 구현 예시
```csharp
[Routes]
public partial class ScreenCoordinator : MonoBehaviour
{
    [Route]
    async UniTask On(NavigateToPageCommand command)
    {
        // 팝업 활성 상태 체크
        if (popupNav.HasActivePopup)
        {
            await Router.Default.PublishAsync(new PageNavigationBlockedEvent(...));
            return;
        }
        
        // 문제없으면 페이지 전환 허용
        await ForwardToPageNavigator(command);
    }
    
    [Route]
    async UniTask On(PageNavigatedEvent evt)
    {
        // 이전 페이지 관련 팝업들 정리
        await Router.Default.PublishAsync(new ClosePageRelatedPopupsCommand(evt.FromPageId));
    }
}
```

## 4단계: 라우터 시스템 (향후)

### 통합 라우팅
```csharp
// 문자열 기반 라우팅
Router.Navigate("page/settings");
Router.Navigate("popup/confirm");
Router.Navigate("notification/error");

// 조건부 라우팅
Router.Navigate("back"); // 현재 상황에 맞는 Back 실행
```

## 도메인별 특화 기능 (향후 확장)

### Page 도메인
- **Presenter**: 전체 화면 관리, 데이터 로딩, 리소스 관리, 전환 효과
- **History**: Deep Navigation, 브랜치 히스토리, 상태 저장
- **Navigator**: 글로벌 상태 관리, 권한 검증, 딥링크 처리

### Popup 도메인
- **Presenter**: 모달 관리, 위치 계산, 크기 조정, 애니메이션
- **History**: 컨텍스트 스택, 레벨별 관리, 일시적 팝업
- **Navigator**: 페이지 상호작용, 외부 이벤트 처리, 다중 팝업 관리

### Notification 도메인
- **Presenter**: 다중 표시, 자동 타이밍, 사용자 인터렉션, 위치 관리
- **History**: 큐 관리, 우선순위 큐, 중복 제거, 그룹핑
- **Navigator**: 시스템 통합, 조건부 표시, 배치 처리

## 이벤트 로깅 & 디버깅

### NavigationEventLogger
모든 네비게이션 이벤트를 기록하여 디버깅과 재생 가능한 시스템 구축:

```csharp
[Routes]
public partial class NavigationEventLogger : MonoBehaviour
{
    private readonly List<object> eventHistory = new();
    
    [Route] void On(PageNavigatedEvent evt) { ... }
    [Route] void On(PopupShownEvent evt) { ... }
    [Route] void On(PageNavigationBlockedEvent evt) { ... }
    
    public async UniTask ReplayEvents() { ... }
    public void ExportEventLog() { ... }
}
```

## 개발 순서 및 현재 상태

### ✅ 완료된 기능
1. **Page 도메인 기본 구현**: To, Push, Back, Replace Command 처리 (중복 방지 포함)
2. **PageStack 히스토리 관리**: Stack 기반 페이지 순서 추적
3. **PageRegistry**: Addressable 기반 동적 로딩 및 캐싱
4. **VitalRouter 통합**: Command 패턴 기반 아키텍처
5. **페이지별 독립 Router**: 각 페이지가 자체 Router 소유 및 생명주기 관리
6. **비동기 애니메이션 시스템**: ShowCommand/HideCommand를 통한 LitMotion 애니메이션
7. **Canvas 기반 화면 제어**: CanvasPage를 통한 기본적인 UI 표시/숨김

### 🚧 현재 진행 중
- Page 도메인 고도화 (Parameters, Event 시스템)

### 📋 향후 계획
1. **Event 시스템 추가**: 네비게이션 이벤트 발행
2. **Parameters 지원**: Command에 데이터 전달 기능
3. **Modal 도메인**: Dialog, Popup, Notification 구현  
4. **ScreenCoordinator**: 도메인 간 상호작용 관리
5. **통합 라우터**: 문자열 기반 라우팅 시스템

## 기술 스택

- **메시징**: VitalRouter (Command/Event 패턴)
- **비동기**: UniTask
- **DI**: VContainer (선택사항)
- **UI 프레임워크**: Unity UI (UGUI) 또는 UI Toolkit

## 패키지 이름

**UINavigation** 또는 **ScreenNavigation**

UI 컴포넌트 렌더링이 아닌 뷰 단위 네비게이션에 특화된 의미로 명명