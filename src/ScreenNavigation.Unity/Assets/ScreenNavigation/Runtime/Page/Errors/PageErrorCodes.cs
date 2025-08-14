namespace ScreenNavigation.Page.Errors
{
    /// <summary>
    /// Page navigation error codes
    /// </summary>
    public static class PageErrorCodes
    {
        /// <summary>
        /// Page not found in registry
        /// </summary>
        public const string NotFound = "Page.NotFound";
        
        /// <summary>
        /// Failed to load page from Addressables
        /// </summary>
        public const string LoadFailed = "Page.LoadFailed";
        
        /// <summary>
        /// Network error during page loading
        /// </summary>
        public const string NetworkError = "Page.NetworkError";
        
        /// <summary>
        /// Page registry operation failed
        /// </summary>
        public const string RegistryError = "Page.RegistryError";
        
        /// <summary>
        /// Empty stack navigation attempted
        /// </summary>
        public const string EmptyStack = "Page.EmptyStack";
        
        /// <summary>
        /// Invalid page state or configuration
        /// </summary>
        public const string InvalidState = "Page.InvalidState";
        
        /// <summary>
        /// Timeout during page operation
        /// </summary>
        public const string Timeout = "Page.Timeout";
        
        /// <summary>
        /// Permission denied for page access
        /// </summary>
        public const string PermissionDenied = "Page.PermissionDenied";
        
        /// <summary>
        /// Already on the requested page
        /// </summary>
        public const string AlreadyCurrent = "Page.AlreadyCurrent";
        
        /// <summary>
        /// 페이지 Show 실행 실패
        /// </summary>
        public const string ShowFailed = "Page.ShowFailed";
        
        /// <summary>
        /// 페이지 Hide 실행 실패
        /// </summary>
        public const string HideFailed = "Page.HideFailed";
    }
}