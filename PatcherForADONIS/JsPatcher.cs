using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PatcherForADONIS
{
    public class JsPatcher
    {
        private readonly string jsFilePath;
        private readonly OperationStatusExt opStatus;
        private const string PATCH_MARKER = "// Patched for iframe support";

        private const string NEW_GETHASH_BODY = "\n\t  var loc = window == window.top ? window.top.location : window.location;\n" +
        "\t  var href = loc.href, i = href.indexOf('#');\n" +
        "\t  return i >= 0 ? href.substr(i + 1) : null;\n  ";

        private const string NEW_TOKEN_BODY = "\t\t\ttoken = newtoken;\n" +
            "\t\t\t  handleStateChange(token);\n" +
            "\t\t\t  var loc = window == window.top ? window.top.location : window.location;\n" +
            "\t\t\t  loc.hash = token;\n" +
            "\t\t\t  hash = token;\n"+
            "\t\t\t  doSave();\n\t\t  ";

        private const string NEW_EXT_BODY = "\n\t\t\t  var loc = window == window.top ? window.top.location : window.location;\n" +
            "\t\t\t  loc.hash = token;\n" +
            "\t\t\t  return true;\n\t\t  ";

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

            if(Regex.IsMatch(content, @"^\s*//\s*Patched\s+for\s+iframe\s+support", RegexOptions.Multiline))
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
                int closeGet = findBlockEndIndex(content, openGet);
                if (closeGet > openGet)
                {
                    content = content.Substring(0, openGet + 1)
                            + NEW_GETHASH_BODY
                            + content.Substring(closeGet);

                    patchedGetHash = true;
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                            "getHash() patched successfully.");
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
                int closeCheck = findBlockEndIndex(content, openCheck);
                if (closeCheck > openCheck)
                {
                    string checkFunc = content.Substring(openCheck, closeCheck - openCheck + 1);

                    int ifPos = checkFunc.IndexOf("if (newtoken !== token)", StringComparison.OrdinalIgnoreCase);
                    if (ifPos >= 0)
                    {
                        int ifOpen = checkFunc.IndexOf('{', ifPos);
                        int ifClose = findBlockEndIndex(checkFunc, ifOpen);
                        if (ifClose > ifOpen)
                        {
                            checkFunc = checkFunc.Substring(0, ifOpen + 1)
                                       + NEW_TOKEN_BODY
                                       + checkFunc.Substring(ifClose);

                            content = content.Substring(0, openCheck)
                                     + checkFunc
                                     + content.Substring(closeCheck + 1);

                            patchedToken = true;
                            opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                                    "Token setter patched successfully.");
                        }
                        else
                        {
                            opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                    "Could not locate if-block boundaries in checkIFrame().");
                        }
                    }
                    else
                    {
                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                "Could not locate if (newtoken !== token) block in checkIFrame().");
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
            int returnPos = content.IndexOf("return {", StringComparison.Ordinal);
            if (returnPos >= 0)
            {
                int addPos = content.IndexOf("add: function", returnPos, StringComparison.OrdinalIgnoreCase);
                if (addPos >= 0)
                {
                    int addOpen = content.IndexOf('{', addPos);
                    int addClose = findBlockEndIndex(content, addOpen);
                    if (addClose > addOpen)
                    {
                        string addBlock = content.Substring(addOpen, addClose - addOpen + 1);
                        int ifExPos = addBlock.IndexOf("if (Ext.isIE)", StringComparison.OrdinalIgnoreCase);
                        if (ifExPos >= 0)
                        {
                            int ifExOpen = addBlock.IndexOf('{', ifExPos);
                            int ifExClose = findBlockEndIndex(addBlock, ifExOpen);
                            if (ifExClose > ifExOpen)
                            {
                                int elsePos = addBlock.IndexOf("else", ifExClose, StringComparison.OrdinalIgnoreCase);
                                if (elsePos >= 0)
                                {
                                    int elseOpen = addBlock.IndexOf('{', elsePos);
                                    int elseClose = findBlockEndIndex(addBlock, elseOpen);
                                    if (elseClose > elseOpen)
                                    {
                                        addBlock = addBlock.Substring(0, elseOpen + 1)
                                                 + NEW_EXT_BODY
                                                 + addBlock.Substring(elseClose);

                                        content = content.Substring(0, addOpen)
                                                 + addBlock
                                                 + content.Substring(addClose + 1);

                                        patchedExt = true;
                                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.SUCCESS,
                                                "Extension method patched successfully.");
                                    }
                                    else
                                    {
                                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                                "Could not locate else-block boundaries in add() method.");
                                    }
                                }
                                else
                                {
                                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                            "Could not locate else-block in add() method.");
                                }
                            }
                            else
                            {
                                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                        "Could not locate Ext.isIE if-block boundaries in add() method.");
                            }
                        }
                        else
                        {
                            opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                    "Could not locate Ext.isIE check in add() method.");
                        }
                    }
                    else
                    {
                        opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                                "Could not locate add() method body boundaries.");
                    }
                }
                else
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "add: function not found inside return { ... } block.");
                }
            }
            else
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        "return { … } block not found.");
            }

            bool anyPatched = patchedGetHash || patchedToken || patchedExt;

            if (anyPatched)
            {
                content = PATCH_MARKER + Environment.NewLine + content;
                backupAndSave(content);

                if (!patchedGetHash)
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "getHash() function was not patched.");
                if (!patchedToken)
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "Token setter was not patched.");
                if (!patchedExt)
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                            "Extension method was not patched.");
            }
            else
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                        "No patchable sections were found. File remains unchanged.");
            }


            return (true, content);
        }

        private static int findBlockEndIndex(string text, int startIndex)
        {
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != '{')
                throw new ArgumentException("startIndex have to be on a '{' sign");

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

            File.WriteAllText(jsFilePath, content,new UTF8Encoding(false));
        }
    }
}
