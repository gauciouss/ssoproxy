using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gd.Core
{
    public class GDExCode
    {
        
        public string Code { get; set; }

        public string Message { get; set; }

        public GDExCode(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public static readonly GDExCode Success = new("00-000", "成功");
        public static readonly GDExCode UnknownError = new("99-999", "未知錯誤");
        public static readonly GDExCode InvalidInput = new("99-001", "輸入資料不合法");
        public static readonly GDExCode NotFound = new("99-002", "找不到資料");
        public static readonly GDExCode Unauthorized = new("99-003", "未授權的存取");
        public static readonly GDExCode Forbidden = new("99-004", "禁止存取");
        public static readonly GDExCode InternalServerError = new("99-005", "伺服器內部錯誤");
        public static readonly GDExCode ServiceUnavailable = new("99-006", "服務暫時無法使用");
        public static readonly GDExCode Timeout = new("99-007", "請求逾時");
        public static readonly GDExCode Conflict = new("99-008", "資料衝突");
        public static readonly GDExCode BadRequest = new("99-009", "錯誤的請求");
        public static readonly GDExCode NotImplemented =    new("99-010", "尚未實作的功能");
    }
}