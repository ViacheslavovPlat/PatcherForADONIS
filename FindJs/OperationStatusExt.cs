using Program;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindJs
{
    public class OperationStatusExt
    {
        public enum operationStatus
        {
            SUCCESS,
            ERROR,
            WARNING,
            PENDING
        }
        private string toSymbol(operationStatus status) 
        {
            return status switch
            {
                operationStatus.SUCCESS => "✅",
                operationStatus.ERROR => "❌",
                operationStatus.WARNING => "⚠️",
                operationStatus.PENDING => "⏳",
                _ => "❔"
            };
        }
        public void printOperationStatus(operationStatus status, string description)
        {
            switch(status)
            {
                case operationStatus.SUCCESS:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{toSymbol(status)} {status}\n\t\t|{description}|");
                    Console.ResetColor();
                    break;
                case operationStatus.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{toSymbol(status)} {status}\n\t\t|{description}|");
                    Console.ResetColor();
                    break;
                case operationStatus.PENDING:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{toSymbol(status)} {status}\n\t\t|{description}|");
                    Console.ResetColor();
                    break;
                case operationStatus.WARNING:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{toSymbol(status)} {status}\n\t\t|{description}|");
                    Console.ResetColor();
                    break;
                default:
                    Console.ResetColor();
                    break;
            }
            
        }
    }
}
