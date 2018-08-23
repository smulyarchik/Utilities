using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace UI.Test.Common.Test.Utilities
{
    /// <summary>
    /// Provides a functionality to extract a test tree from an assembly with the possibility of exporting its information into a tab delimited CSV file.
    /// </summary>
    public class TestExplorer
    {
        private const char Delimiter = '\t';

        /// <summary>
        /// Extract NUnit test tree.
        /// </summary>
        /// <param name="assembly">Assembly containing NUnit tests.</param>
        /// <returns>Complete NUnit test tree.</returns>
        public static ITest ExtractTestTree(Assembly assembly)
        {
            var tree = new DefaultTestAssemblyBuilder().Build(assembly, new Dictionary<string, object>());
            return tree;
        }

        /// <summary>
        /// Extact NUnit test tree and export its information into a tab delimited CSV file.
        /// </summary>
        /// <param name="assembly">Assembly containing NUnit tests.</param>
        /// <param name="fileName">Path to CSV file to export tests into.</param>
        /// <param name="propertyNames">List of NUnit properties to extract information from.</param>
        /// <remarks>Each property information is placed into a separate column.</remarks>
        public static void ExportTestTreeIntoFile(Assembly assembly, string fileName = "ExportedTests.csv", params string[] propertyNames)
        {
            var sb = new StringBuilder();
            var assemblyFixture = ExtractTestTree(assembly);
            Console.WriteLine("Assembly: '{0}' is built.", assemblyFixture.FullName);

            var headerLine = propertyNames
                .Aggregate(nameof(assemblyFixture.FullName) + Delimiter + nameof(assemblyFixture.TestType) + Delimiter,
                    (tot, cur) => tot + cur + Delimiter).Trim(Delimiter);
            sb.AppendLine(headerLine);
            Console.WriteLine("Added headers: '{0}'.", headerLine);

            var fixtures = GetTestFixtures(assemblyFixture).ToList();
            Console.WriteLine("Number of discovered fixtures: '{0}'.", fixtures.Count);

            var props = new PropertyBag();
            foreach (var name in propertyNames)
                props.Add(name, null);
            foreach (var f in fixtures)
            {
                RecursivelyAppendTestInfo(f, props, sb);
                Console.WriteLine("Fixture processed: '{0}'.", f.FullName);
            }
            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            Console.WriteLine("Saved at: '{0}.", fileName);
        }

        private static void RecursivelyAppendTestInfo(ITest test, IPropertyBag parentBag, StringBuilder sb)
        {
            if (!test.HasChildren)
            {
                AppendTestInfo(test, parentBag, sb);
                return;
            }

            var props = MergePropertyBags(parentBag, test.Properties);
            test.Tests.ToList().ForEach(t => RecursivelyAppendTestInfo(t, props, sb));
        }

        private static void AppendTestInfo(ITest test, IPropertyBag parentBag, StringBuilder sb)
        {
            var fullName = test.FullName;

            var mergedBag = MergePropertyBags(parentBag, test.Properties);
            if (mergedBag.ContainsKey(PropertyNames.Category))
            {
                const char newLine = '\r';
                mergedBag.Set(PropertyNames.Category, mergedBag[PropertyNames.Category].Cast<string>()
                    .Aggregate(string.Empty, (tot, cur) => tot +  cur + newLine)
                    .Trim(newLine));
            }

            var testType = test.TestType;
            var line = fullName + Delimiter + testType + Delimiter;
            foreach (var key in mergedBag.Keys)
            {
                var propList = mergedBag[key];
                var propStr = propList.Cast<object>().Aggregate(string.Empty, (tot, cur) => tot + cur);
                // ReSharper disable once UseStringInterpolation
                line += string.Format("\"{0}\"{1}", propStr, Delimiter);
            }

            sb.AppendLine(line.Trim(Delimiter));
        }

        private static PropertyBag MergePropertyBags(IPropertyBag bagA, IPropertyBag bagB)
        {
            var mergedBag = new PropertyBag();
            foreach (var key in bagA.Keys)
                mergedBag[key] = bagA[key].Cast<object>().Union(bagB[key].Cast<object>()).ToList();
            return mergedBag;
        }

        private static IEnumerable<ITest> GetTestFixtures(ITest node)
        {
            Console.WriteLine("Getting fixtures for: '{0}'", node.FullName);
            Console.WriteLine("Node has {0}' children.", node.Tests.Count);
            if (node.HasChildren)
            {
                var firstChild = node.Tests.First();
                Console.WriteLine("First child name: '{0}'", firstChild.FullName);
                if (firstChild.TestType.Equals("TestMethod") || firstChild.TestType.Equals("ParameterizedMethod"))
                    return new[] {node};
            }
            return node.Tests.SelectMany(GetTestFixtures);
        }
    }
}
