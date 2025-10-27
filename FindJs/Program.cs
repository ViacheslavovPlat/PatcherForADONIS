using System.IO;
using System.Runtime.CompilerServices;
using FindJs;

namespace Program 
{
    class Program
    {

        static int Main(string[] args) 
        {
            var opStatus = new OperationStatusExt();
            FileStream fr, fw;
            string exportPath;

            Console.WriteLine("\t=== ADONIS Export Patcher ===");

            if (args.Length == 0)
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, "Missing argument: export folder path.");
                return 1;
            }

            exportPath = args[0];

            try
            {
                if (!Directory.Exists(exportPath))
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, $"Folder not Found:{exportPath}");
                    return 2;
                }
                var validator = new Validator(exportPath);

                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING, $"Checking folder: {exportPath}");
                if (!validator.validateExportFolder()) 
                {
                    return 3;
                }
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS, $"Folder exists. Proceeding to next step...");
                return 0;
            }
            catch (Exception ex) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, $"{ex.Message}");
                return -1;
            }
        }

        
    }
}