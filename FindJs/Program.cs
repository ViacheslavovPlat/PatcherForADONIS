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
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, $"Folder validation failed. Aborting.");
                    return 3;
                }
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS, $"Folder exists. Proceeding to next step...");
            }
            catch (Exception ex) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, $"{ex.Message}");
                return -1;
            }

            var jsFilePath = Path.Combine(exportPath, "client", "scripts", "ext_overrides.js");
            var patcher = new JsPatcher(jsFilePath);
            var (success, newContent) = patcher.applyPatch();

            if (success)
            {
                File.WriteAllText(jsFilePath, newContent);
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS, "Patching completed successfully.");
                return 0;
            }
            else 
            { 
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR, "Patching failed. No changes were made to the file.");
                return 4;
            }
        }

        
    }
}