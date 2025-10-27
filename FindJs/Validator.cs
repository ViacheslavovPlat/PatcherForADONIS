using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FindJs
{
    public class Validator
    {
        private string clientPath;
        private string indexHtmlPath;
        private readonly OperationStatusExt opStatus;

        public Validator(string expPath)
        {
            clientPath = Path.Combine(expPath, "client", "scripts", "ext_overrides.js");
            indexHtmlPath = Path.Combine(expPath, "index.html");
            this.opStatus = new OperationStatusExt();
        }

        public bool validateExportFolder() 
        {
            if(!File.Exists(clientPath)) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR
                    , "Missing required file: client\\scripts\\ext_overrides.js");
                return false;
            }

            if (!File.Exists(indexHtmlPath))
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                    "index.html not found — structure may be incomplete.");
            }
            else
            {
                try
                {
                    string indexContent = File.ReadAllText(indexHtmlPath);
                    if (!indexContent.Contains("ADONIS", StringComparison.OrdinalIgnoreCase) &&
                        !indexContent.Contains("BOC", StringComparison.OrdinalIgnoreCase))
                    {
                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "index.html does not seem to belong to ADONIS export.");
                    }
                }
                catch (Exception ex) 
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        $"Could not read index.html ({ex.Message}).");
                }
            }

            opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                 "Export folder structure looks valid.");

            return true;
        }

    }
}
