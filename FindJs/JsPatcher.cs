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

            if (!File.Exists(jsFilePath))
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                    $"File not found: {jsFilePath}");
                return (false, "");
            }

            string content;
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

            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Applying patch to JS file...");

            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Patching getHash function...");
            bool patchedGetHash = false;
            int posGet = content.IndexOf("function getHash", StringComparison.Ordinal);
            if (posGet >= 0)
            {
                int openGet = content.IndexOf('{', posGet);
                int closeGet = FindBlockEndIndex(content, openGet);
                if (openGet > posGet && closeGet > openGet)
                {
                    string header = content.Substring(posGet, openGet - posGet + 1);
                    string tail = content.Substring(closeGet); 
                    content = content.Substring(0, posGet) + header + NEW_GETHASH_BODY + tail.Substring(1);
                    patchedGetHash = true;
                }
                else
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        "Could not locate getHash() body boundaries.");
                }
            }
            else
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                    "Function getHash() not found.");
            }

            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Patching token setter...");
            bool patchedToken = false;
            int posCheck = content.IndexOf("function checkIFrame", StringComparison.Ordinal);
            if (posCheck >= 0)
            {
                int openCheck = content.IndexOf('{', posCheck);
                int closeCheck = FindBlockEndIndex(content, openCheck);
                if (openCheck > posCheck && closeCheck > openCheck)
                {
                    string func = content.Substring(posCheck, closeCheck - posCheck + 1);
                    int ifPos = func.IndexOf("if (newtoken !== token)", StringComparison.Ordinal);
                    if (ifPos >= 0)
                    {
                        int ifOpen = func.IndexOf('{', ifPos);
                        int ifClose = FindBlockEndIndex(func, ifOpen);
                        if (ifOpen > ifPos && ifClose > ifOpen)
                        {
                            string before = func.Substring(0, ifOpen + 1);
                            string after = func.Substring(ifClose);
                            func = before + NEW_TOKEN_BODY + after;
                            content = content.Substring(0, posCheck) + func + content.Substring(closeCheck + 1);
                            patchedToken = true;
                        }
                        else
                        {
                            opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                "Could not locate token setter block boundaries.");
                        }
                    }
                    else
                    {
                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "Token setter if-block not found.");
                    }
                }
                else
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        "Could not locate checkIFrame() body boundaries.");
                }
            }
            else
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                    "Function checkIFrame() not found.");
            }

            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Patching extension method...");
            bool patchedExt = false;
            int posReturn = content.IndexOf("return", StringComparison.Ordinal);
            if (posReturn >= 0)
            {
                int openReturn = content.IndexOf('{', posReturn);
                int closeReturn = FindBlockEndIndex(content, openReturn);
                if (openReturn > posReturn && closeReturn > openReturn)
                {
                    string returnBody = content.Substring(openReturn + 1, closeReturn - openReturn - 1);
                    int addPos = returnBody.IndexOf("add:", StringComparison.Ordinal);
                    if (addPos >= 0)
                    {
                        int funcOpen = returnBody.IndexOf('{', addPos);
                        int funcClose = FindBlockEndIndex(returnBody, funcOpen);
                        if (funcOpen > addPos && funcClose > funcOpen)
                        {
                            string addFunc = returnBody.Substring(addPos, funcClose - addPos + 1);
                            int elsePos = addFunc.IndexOf("else", StringComparison.Ordinal);
                            if (elsePos >= 0)
                            {
                                int elseOpen = addFunc.IndexOf('{', elsePos);
                                int elseClose = FindBlockEndIndex(addFunc, elseOpen);
                                if (elseOpen > elsePos && elseClose > elseOpen)
                                {
                                    string before = addFunc.Substring(0, elseOpen + 1);
                                    string after = addFunc.Substring(elseClose);
                                    addFunc = before + NEW_EXT_BODY + after;
                                    returnBody = returnBody.Substring(0, addPos) + addFunc + returnBody.Substring(funcClose + 1);
                                    content = content.Substring(0, openReturn + 1) + returnBody + content.Substring(closeReturn);
                                    patchedExt = true;
                                }
                                else
                                {
                                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                        "Could not locate else-block boundaries in add().");
                                }
                            }
                            else
                            {
                                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                    "else-block inside add() not found.");
                            }
                        }
                        else
                        {
                            opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                "Could not locate add() body boundaries.");
                        }
                    }
                    else
                    {
                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "Method add() not found inside return block.");
                    }
                }
                else
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        "Could not locate return-block boundaries.");
                }
            }
            else
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                    "return block not found.");
            }

            backupAndSave(content);
            opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                "Patching completed successfully.");

            return (true, content);
        }



        /*private string filterString(string input, IEnumerable<string> patterns, Func<string, string> filteredString)
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
        }*/

        private static int FindBlockEndIndex(string text, int startIndex)
        {
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != '{')
                throw new ArgumentException("startIndex должен указывать на символ '{' в строке.");

            int depth = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
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
