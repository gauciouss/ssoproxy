using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gd.Core;

namespace ssoproxy.Models
{
    public class ResponseEntity
    {
        public Object? Body { get; set; }

        public Header Header { get; set; }

        public ResponseEntity()
        {
            Body = null;
            Header = new Header(string.Empty, string.Empty, 0);
        }

        public ResponseEntity(Object? body, Header header)
        {
            Body = body;
            Header = header;
        }

        public ResponseEntity(Object? body, string code, string message, Double execTime)
        {
            Body = body;
            Header = new Header(code, message, execTime);
        }

        public ResponseEntity(Object? body, GDExCode code, Double execTime, string source)
        {
            Body = body;
            Header = new Header(code.Code, code.Message, execTime);
            // You can add a Source property to the ResponseEntity class if needed
        }   
    }

    public class Header
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public Double ExecTime { get; set; }

        public Header(string code, string message, Double execTime)
        {
            Code = code;
            Message = message;
            ExecTime = execTime;
        }
    }
}