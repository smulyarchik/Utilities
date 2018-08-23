using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Logger = UI.Test.Common.Utilities.Logger;

namespace UI.Test.Common.Test.Utilities
{
    /// <summary>
    /// Provides functionality for generating an NUnit Test Selection Language(TSL) query for the given execution point.
    /// <para>Should be placed after the general test execution is finished, i.e. inside of methods marked with <see cref="NUnit.Framework.OneTimeTearDownAttribute"/> or <see cref="NUnit.Framework.TearDownAttribute"/>.</para>
    /// </summary>
    public static class TslGenerator
    {
        /// <summary>
        /// Builds a TSL query for the currently failed tests.
        /// </summary>
        /// <returns>NUnit TSL based query.</returns>
        public static string BuildRetryQuery()
        {
            Logger.Exec(Logger.LogLevel.Debug);

            var runResult = TestExecutionContext.CurrentContext.CurrentResult;
            if (runResult.ResultState.Status == TestStatus.Passed) return string.Empty;

            var failedFixturesResults = GetFailedResults(runResult);
            return BuildQuery(failedFixturesResults);
        }

        /// <summary>
        /// Builds a TSL query for the currently failed tests and writes it to a file using the provided location.
        /// </summary>
        /// <param name="fileName">Absolute path to the file what the built query should be written to.</param>
        public static void WriteRetryQueryToFile(string fileName)
        {
            Logger.Exec(Logger.LogLevel.Debug, fileName);

            var query = BuildRetryQuery();
            if (string.IsNullOrEmpty(query)) return;
            WriteTslToFile(query, fileName);
        }

        private static string BuildQuery(IEnumerable<ITestResult> resultList)
        {
            var results = resultList.ToArray();
            Logger.Exec(Logger.LogLevel.Debug, results.Select(e => e.FullName));

            const char pipe = '|';
            var writeArg = new Func<ITestResult, string>(res =>
            {
                var name = res.FullName;
                if (name.Contains("("))
                    // Enclose the name in quote in order to run parameterized fixtures/tests.
                    name = $"\\\"{name}\\\"";
                return $"test=={name} {pipe} ";
            });

            return results.Aggregate(string.Empty, (query, fixture) =>
            {
                return fixture.Children.Where(child => child.ResultState.Status == TestStatus.Failed)
                    .Select(child =>
                        {
                            // If dealing with a parameterized test, get its failed child results.
                            return child.HasChildren
                                ? child.Children.Where(c => c.ResultState.Status == TestStatus.Failed)
                                    .Aggregate(string.Empty, (line, c) => line + writeArg(c))
                                : writeArg(child);
                        })
                    // Remove duplicates. (if test cases are used)
                    .Distinct()
                    .Aggregate(query, (tot, cur) => tot + cur);
            }).Trim(pipe, ' ');
        }

        private static IEnumerable<ITestResult> GetFailedResults(ITestResult node)
        {
            // If the node is a suite that has failed test cases.
            if (node.Test.IsSuite && node.FailCount > 0)
            {
                // And the first child either is parameterized or simple test method. (i.e. not a suite)
                var firstChild = node.Children.First();
                if (firstChild.Test.TestType.Equals("ParameterizedMethod") || !firstChild.Test.IsSuite)
                    // Add the node to the collection.
                    return new[] {node};
            }

            return node.Children.SelectMany(GetFailedResults);
        }

        private static void WriteTslToFile(string query, string fileName)
        {
            Logger.Exec(Logger.LogLevel.Debug, query, fileName);
            File.WriteAllText(fileName, query);
        }

    }
}
