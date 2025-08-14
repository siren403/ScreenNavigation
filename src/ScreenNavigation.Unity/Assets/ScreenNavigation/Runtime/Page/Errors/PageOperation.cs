namespace ScreenNavigation.Page.Errors
{
    /// <summary>
    /// 페이지 네비게이션 오퍼레이션 (에러 리포팅용)
    /// </summary>
    public enum PageOperation
    {
        /// <summary>
        /// 특정 오퍼레이션 없음 또는 일반적인 경우
        /// </summary>
        None,
        
        /// <summary>
        /// To 페이지 네비게이션 (모든 페이지 클리어 후 새 페이지 표시)
        /// </summary>
        To,
        
        /// <summary>
        /// Push 페이지 네비게이션 (스택에 페이지 추가)
        /// </summary>
        Push,
        
        /// <summary>
        /// Back 페이지 네비게이션 (이전 페이지로 돌아가기)
        /// </summary>
        Back,
        
        /// <summary>
        /// Replace 페이지 네비게이션 (현재 페이지 교체)
        /// </summary>
        Replace
    }
}