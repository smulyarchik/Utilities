using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

namespace UI.Test.Common.Test.Utilities
{
    public static class TestScopeUtility
    {
        private static readonly object Locker = new object();
        private const string ParameterizedMethod = "ParameterizedMethod";

        public static bool IsParallel(this ITest test)
        {
            // Parameterized methods are treated as fixtures. Ignore for the sake of validation.
            var fixture = test.Parent;
            var fixtureMethodsAreParallel = ContextMethodsAreParallel(fixture);
            if (test.Parent.TestType.Equals(ParameterizedMethod))
                fixture = fixture.Parent;
            fixtureMethodsAreParallel |= ContextMethodsAreParallel(fixture);
            return fixtureMethodsAreParallel || HasParallelScope(test);
        }

        private static bool HasParallelScope(ITest test)
        {
            var parallelScope = test.Properties[PropertyNames.ParallelScope];
            var hasParallelScope = parallelScope.Count > 0;
            // If a test has parallel scope different from None, it is parallel.
            return hasParallelScope && !parallelScope[0].Equals(ParallelScope.None);
        }

        private static IEnumerable<CompositeWorkItem> GetAllChildren(CompositeWorkItem item)
        {
            var firstChild = item.Children.FirstOrDefault();
            var isTestMethod = firstChild != null &&
                               (!firstChild.Test.IsSuite || firstChild.Test.TestType.Equals(ParameterizedMethod));
            return isTestMethod
                ? new[] {item}
                : item.Children.Cast<CompositeWorkItem>().SelectMany(GetAllChildren);
        }

        private static bool ContextMethodsAreParallel(ITest fixture)
        {
            // If a fixture has parallel scope set to either ALl or Children, all its tests are parallel.
            IList parallelScope;
            lock (Locker)
            {
                // NUnit internal implementation adds an item to PropertyBag if it doesn't exist. 
                // Race conditions may apply when one thread adds an item while another thread performs Contains check.
                parallelScope = fixture.Properties[PropertyNames.ParallelScope]; 
            }
            if (HasParallelScope(fixture) && parallelScope[0].Equals(ParallelScope.All | ParallelScope.Children))
                return true;
            // Or started subset of tests all have parallel scope.
            var dispatcher = TestExecutionContext.CurrentContext.Dispatcher;
            var topLevelWorkItem = dispatcher?.GetType()
                .GetField("_topLevelWorkItem", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(dispatcher) as CompositeWorkItem;
            if (topLevelWorkItem == null)
                return false;
            var currentWorkItem = GetAllChildren(topLevelWorkItem).FirstOrDefault(t => t.Test.Equals(fixture) || t.Children.Any(c => c.Test.Equals(fixture)));
            return currentWorkItem != null && currentWorkItem.Children.Select(item => item.Test).All(HasParallelScope);
        }
    }
}
