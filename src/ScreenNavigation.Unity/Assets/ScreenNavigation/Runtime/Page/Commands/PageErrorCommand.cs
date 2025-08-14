using System;
using ScreenNavigation.Page.Errors;
using VitalRouter;

namespace ScreenNavigation.Page.Commands
{
    /// <summary>
    /// 페이지 네비게이션 에러 커맨드
    /// </summary>
    public readonly struct PageErrorCommand : ICommand
    {
        /// <summary>
        /// 에러가 발생한 페이지 ID
        /// </summary>
        public readonly string PageId;
        
        /// <summary>
        /// 실패한 오퍼레이션 (To, Push, Back, Replace 등)
        /// </summary>
        public readonly PageOperation Operation;
        
        /// <summary>
        /// 구조화된 에러 코드 (예: "Page.NotFound")
        /// </summary>
        public readonly string ErrorCode;
        
        /// <summary>
        /// 사용자용 에러 메시지
        /// </summary>
        public readonly string Message;
        
        /// <summary>
        /// 원본 예외 (nullable)
        /// </summary>
        public readonly Exception Exception;

        public PageErrorCommand(string pageId, PageOperation operation, string errorCode, string message, Exception exception = null)
        {
            PageId = pageId;
            Operation = operation;
            ErrorCode = errorCode;
            Message = message;
            Exception = exception;
        }
    }
}