using System;
using System.Collections.Generic;
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
        private const string patchMarker = "// Patched for iframe support";

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
        
        public JsPatcher(string jsFilePath) 
        {
            this.jsFilePath = jsFilePath;
            this.opStatus = new OperationStatusExt();
        }

        public bool applyPatch() 
        {
            string content;

            if (!File.Exists(jsFilePath)) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                     $"File not found: {jsFilePath}");
                return false;
            }

            try
            {
                content = File.ReadAllText(jsFilePath, Encoding.UTF8);
            }
            catch (Exception ex) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.ERROR,
                    $"Cannot read file: {ex.Message}");
                return false;
            }

            if (content.Contains(patchMarker)) 
            {
                opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING, 
                    "File is already patched skipping");
                return true;
            }

            
        }

        private string filterString(string input, IEnumerable<string> patterns, Func<string, string> filteredString)
        {
            string current = input;
            opStatus.printOperationStatus(OperationStatusExt.operationStatus.PENDING,
                "Filtering string by nested patterns...");

            string lastPattern = patterns.Last();
            string? matchedFullBlock = null;

            foreach (var pattern in patterns)
            {
                // Многострочный режим + DOTALL
                var match = Regex.Match(current, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
                if (!match.Success)
                {
                    opStatus.printOperationStatus(OperationStatusExt.operationStatus.WARNING,
                        $"No pattern was found: {pattern}");
                    return input; // Возвращаем оригинальный текст, без изменений
                }

                // Тело блока (внутреннее содержимое {...})
                current = match.Groups[1].Value;

                // Сохраняем последний полный матч (включая скобки) — чтобы потом вставить
                if (pattern == lastPattern)
                    matchedFullBlock = match.Value;
            }

            if (matchedFullBlock == null)
                return input;

            // Вызываем пользовательскую замену (вставляем новый код)
            string filteredBody = filteredString(current);

            // Заменяем старый блок на новый
            string result = input.Replace(current, filteredBody);

            return result;
        }

    }
}
