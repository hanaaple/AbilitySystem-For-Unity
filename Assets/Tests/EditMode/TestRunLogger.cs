using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// 테스트 실행 결과를 <c>Logs/TestRun.log</c>에 한 줄씩 남긴다. Test Runner 창을 보지 않고(에이전트·CLI 없이 에디터가 열린 상태) 결과를 읽기 위한 것.
    /// 도메인 리로드가 중간에 끼어도(EnterPlayMode 테스트) InitializeOnLoad로 다시 등록돼 남은 결과가 이어 적힌다.
    /// </summary>
    [InitializeOnLoad]
    public static class TestRunLogger
    {
        public const string LogPath = "Logs/TestRun.log";

        static TestRunLogger()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
        }

        private static void Append(string line)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }

        private sealed class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Append($"[RunStarted] {DateTime.Now:yyyy-MM-dd HH:mm:ss} mode={testsToRun.TestMode} count={testsToRun.TestCaseCount}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Append($"[RunFinished] {DateTime.Now:HH:mm:ss} status={result.TestStatus} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount} duration={result.Duration:F2}s");
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test.IsSuite)
                {
                    return;
                }

                Append($"[{result.TestStatus}] {result.Test.FullName} ({result.Duration * 1000.0:F1} ms)");
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Append("  message: " + result.Message.Replace("\n", "\n  "));
                }

                if (!string.IsNullOrEmpty(result.Output))
                {
                    Append("  output: " + result.Output.TrimEnd().Replace("\n", "\n  "));
                }
            }
        }
    }
}
