using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FindJs
{
    public class JsPatcher
    {
        private readonly string jsFilePath;
        private readonly OperationStatusExt opStatus;
        private const string PATCH_MARKER = "// Patched for iframe support";

        private const string NEW_GETHASH_BODY = @"
        var loc = window == window.top ? window.top.location : window.location;
        var href = loc.href, i = href.indexOf('#');
        return i >= 0 ? href.substr(i + 1) : null;\n";

        private const string NEW_TOKEN_BODY = @"
        token = newtoken;
        handleStateChange(token);
        var loc = window == window.top ? window.top.location : window.location;
        loc.hash = token;
        hash = token;
        doSave();\n";

        private const string NEW_EXT_BODY = @"
            var loc = window == window.top ? window.top.location : window.location;
            loc.hash = token;
            return true;\n";

        private string[] getHashPattern = { @"function\s+getHash\s*\([^)]*\)\s*\{([\s\S]*?)\}" };

        private string[] setTokenPatterns = {@"function\s+checkIFrame\s*\([^)]*\)\s*\{([\s\S]*?)\}",
                @"setInterval\s*\(\s*function\s*\([^)]*\)\s*\{\s*([\s\S]*?)\s*\}\s*,\s*\d+\s*\)\s*;",
                @"if\s*\(newtoken\s*!==\s*token\)\s*\{([\s\S]*?)\}"};

        private string[] extPatterns = {@"return\s*\{\s*([\s\S]*?)\}",
                @"add\s*:\s*function\s*\([^)]*\)\s*\{\s*([\s\S]*?)\s*\}\s*,",
                @"else\s*\{([\s\S]*?)\}" };

        public JsPatcher(string jsFilePath) 
        {
            this.jsFilePath = jsFilePath;
            this.opStatus = new OperationStatusExt();
        }

        public (bool, string) applyPatch()
        {
            string content;

            if (!File.Exists(jsFilePath))
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                     $"File not found: {jsFilePath}");
                return (false, "");
            }

            try
            {
                content = File.ReadAllText(jsFilePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                    $"Cannot read file: {ex.Message}");
                return (false, "");
            }

            if (content.Contains(PATCH_MARKER))
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                    "File is already patched skipping");
                return (true, "");
            }

            try
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                    "Applying patch to JS file...");

                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                    "Patching getHash function...");
                content = filterString(content, getHashPattern, _ => NEW_GETHASH_BODY);

                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                    "Patching token setter...");
                content = filterString(content, setTokenPatterns, _ => NEW_TOKEN_BODY);

                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                    "Patching extension method...");
                content = filterString(content, extPatterns, _ => NEW_EXT_BODY);

                backupAndSave(content);
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                    "Patching completed successfully.");
                return (true, content);
            }
            catch (Exception ex)
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                    $"Error during patching: {ex.Message}");
                return (false, "");
            }
        }

        private string filterString(string input, IEnumerable<string> patterns, Func<string, string> filteredString)
        {
            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Filtering string by nested patterns...");

            string lastPattern = patterns.Last();
            Match? lastMatch = null;
            string searchArea = input;

            foreach (var pattern in patterns)
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                    $"Searching pattern: {pattern}");

                var match = Regex.Match(searchArea, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                if (!match.Success)
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        $"No pattern was found: {pattern}");
                    return input;
                }

                searchArea = match.Groups[1].Value;
                lastMatch = match;
            }

            if (lastMatch == null)
                return input;

            string innerContent = lastMatch.Groups[1].Value;
            string newInner = filteredString(innerContent);

            string newBlock = lastMatch.Value.Replace(innerContent, newInner);
            string result = input.Replace(lastMatch.Value, newBlock);

            return result;
        }


        private void backupAndSave(string content) 
        { 
            string backupPath = jsFilePath + ".bak";
            File.Copy(jsFilePath, backupPath, true);
            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                $"Backup created at: {backupPath}");

            File.WriteAllText(jsFilePath,PATCH_MARKER + Environment.NewLine +  content, Encoding.UTF8);
        }

    }
}
