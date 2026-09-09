using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace Core.Common.Editor
{
    /// <summary>baseType을 상속하는 새 서브클래스 .cs를 만드는 순수 저작 로직. UI(EditorWindow)·배정(SessionState/리로드)과 분리 — 여기는 "무슨 소스를, 어디에, 유효하게" 쓰나만 안다. (Unity ScriptTemplates .txt 대신 타입 정보로 소스를 조립해 상속 대상을 코드로 보장.)</summary>
    internal static class SubclassScriptTemplate
    {
        // 기본 클래스명 제안: "New" + baseType 이름.
        public static string SuggestClassName(Type baseType)
        {
            return "New" + baseType.Name;
        }

        // 생성 가능 여부. 문제가 있으면 사용자용 메시지, 없으면 null.
        public static string Validate(string folder, string className, Type baseType)
        {
            if (string.IsNullOrEmpty(className))
            {
                return "Enter a class name.";
            }

            if (!IsValidIdentifier(className))
            {
                return "Not a valid C# class name.";
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                return "Folder does not exist under the project.";
            }

            if (File.Exists(ScriptPath(folder, className)))
            {
                return "A script with this name already exists in the folder.";
            }

            if (EditorTypeUtility.GetConcreteSubclasses(baseType).Any(type => type.Name == className))
            {
                return $"A {baseType.Name} named '{className}' already exists.";
            }

            return null;
        }

        // .cs를 folder에 만들고 경로를 반환한다. 호출 전 Validate 통과를 가정한다(파일 쓰기만 — 임포트/컴파일은 호출부).
        public static string CreateFile(string folder, string className, Type baseType)
        {
            string path = ScriptPath(folder, className);
            File.WriteAllText(path, BuildSource(DeriveNamespace(folder), className, baseType));
            return path;
        }

        private static string ScriptPath(string folder, string className)
        {
            return $"{folder}/{className}.cs";
        }

        // 폴더 경로에서 네임스페이스를 유도한다: "Assets/Scripts/Character" → "Character",
        // "Assets/Scripts/Core/AbilitySystem/Attribute" → "Core.AbilitySystem.Attribute" (기존 컨벤션과 일치).
        // 선행 "Assets"·"Scripts"는 루트 취급해 떼어낸다. 남는 게 없으면 전역 네임스페이스.
        private static string DeriveNamespace(string folder)
        {
            var parts = folder.Replace('\\', '/').Split('/').Where(part => part.Length > 0).ToList();

            if (parts.Count > 0 && parts[0] == "Assets")
            {
                parts.RemoveAt(0);
            }

            if (parts.Count > 0 && parts[0] == "Scripts")
            {
                parts.RemoveAt(0);
            }

            return string.Join(".", parts);
        }

        private static string BuildSource(string ns, string className, Type baseType)
        {
            var sb = new StringBuilder();

            bool needsUsing = !string.IsNullOrEmpty(baseType.Namespace) && baseType.Namespace != ns;
            if (needsUsing)
            {
                sb.AppendLine($"using {baseType.Namespace};");
                sb.AppendLine();
            }

            if (string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"public class {className} : {baseType.Name}");
                sb.AppendLine("{");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {className} : {baseType.Name}");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static bool IsValidIdentifier(string name)
        {
            if (!(char.IsLetter(name[0]) || name[0] == '_'))
            {
                return false;
            }

            for (int i = 1; i < name.Length; i++)
            {
                if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
                {
                    return false;
                }
            }

            return !_keywords.Contains(name);
        }

        private static readonly HashSet<string> _keywords = new()
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };
    }
}
