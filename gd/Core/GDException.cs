using System;

namespace gd.Core
{
    /// <summary>
    /// 傳播於 gd 程式庫的自訂例外，包含選擇性的錯誤代碼與來源例外訊息。
    /// </summary>
    public class GDException : Exception
    {
        /// <summary>
        /// 選擇性的錯誤代碼
        /// </summary>
        public string? ErrorCode { get; }

        public string ? ExceptionMessage { get; }
        

        /// <summary>
        /// 來源例外的訊息快取（如果有提供 inner exception）
        /// </summary>
        public string? SourceExceptionMessage { get; }

        public GDExCode? ExceptionCode { get; }

        public GDException()
        {
        }

        public GDException(string message) : base(message)
        {
        }

        public GDException(string message, Exception innerException) : base(message, innerException)
        {
            SourceExceptionMessage = innerException?.Message;
        }

        public GDException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        public GDException(string message, string errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
            SourceExceptionMessage = innerException?.Message;
        }

        // 支援從 GDExCode 建構（若專案有定義 GDExCode）
        public GDException(GDExCode exCode) : base(exCode?.Message)
        {
            if (exCode is not null)
            {
                ErrorCode = exCode.Code;
                ExceptionMessage = exCode.Message;
            }
        }

        public GDException(GDExCode exCode, Exception innerException) : base(exCode?.Message, innerException)
        {
            if (exCode is not null)
            {
                ErrorCode = exCode.Code;
            }
            SourceExceptionMessage = innerException?.Message;
        }

        public GDException(GDExCode exCode, string sourceMsg) : base(exCode?.Message)
        {
            if (exCode is not null)
            {
                ErrorCode = exCode.Code;
            }
            SourceExceptionMessage = sourceMsg;
        }
    }
}
