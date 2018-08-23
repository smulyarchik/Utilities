using System;
using System.Collections.Generic;
using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace UI.Test.Common.Test.Utilities
{
    /// <summary>
    /// Provides an improved version of WebDriverWait. Allows:
    /// <para>Re-use wait with different parameters.</para>
    /// <para>Ignore timeout exception in case negative result is expected.</para>
    /// <para>Use improved version of Until() method that takes current instance as an argument. Therefore, allows to perform more granular waits.</para>
    /// </summary>
    public class ExtendedWebDriverWait : WebDriverWait
    {
        private readonly IList<Type> _ignoredExceptions;
        private readonly IWebDriver _driver;

        public ExtendedWebDriverWait(IWebDriver driver, TimeSpan timeout)
            : base(driver, timeout)
        {
            _driver = driver;
            var type = GetType().BaseType; // WebDriverWait.
            type = type.BaseType; // DefaultWait.
            _ignoredExceptions = (IList<Type>)type.GetField("ignoredExceptions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
        }

        /// <summary>
        /// Retrieve a new instance of the wait class with specified parameters.
        /// </summary>
        /// <param name="timeout">New timeout.</param>
        /// <param name="pollingInverval">New polling interval.</param>
        /// <param name="delayBy">Interval to wait before the first poll.</param>
        /// <param name="message">New error message.</param>
        /// <param name="ignoreException">Additional exception to ignore. If it's necessary to ignore more than one exception type, use IgnoreExceptionTypes() on the passed instance.</param>
        /// <returns></returns>
        public ExtendedWebDriverWait UseOnce(TimeSpan? timeout = null, TimeSpan? pollingInverval = null, TimeSpan? delayBy = null, string message = null, Type ignoreException = null)
        {
            var instance = new ExtendedWebDriverWait(_driver, timeout ?? Timeout);
            foreach (var ex in _ignoredExceptions)
            {
                if (!instance._ignoredExceptions.Contains(ex))
                    instance._ignoredExceptions.Add(ex);
            }
            if (pollingInverval.HasValue)
                instance.PollingInterval = pollingInverval.Value;
            if (message != null)
                instance.Message = message;
            if (ignoreException != null)
                instance.IgnoreExceptionTypes(ignoreException);
            if (delayBy.HasValue)
                instance.UseOnce(delayBy.Value, ignoreException: typeof(WebDriverTimeoutException)).Until(() => false);
            return instance;
        }

        /// <summary>
        /// Overload of the base method without in T argument.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="condition"></param>
        /// <returns></returns>
        public TResult Until<TResult>(Func<TResult> condition)
        {
            var func = new Func<IWebDriver, TResult>(browser => condition());
            return Until(func);
        }

        /// <summary>
        /// Base version of Until() method improved by passing current instance as an argument, allowing to perform waits inside waits. But be careful with timeouts! Make sure outer wait's timeout if greater than the inner.
        /// </summary>
        /// <typeparam name="TResult">Type of expected result.</typeparam>
        /// <param name="condition">Expected condition.</param>
        /// <returns>Instance of the expected type.</returns>
        public TResult Until<TResult>(Func<ExtendedWebDriverWait, TResult> condition)
        {
            var func = new Func<IWebDriver, TResult>(browser => condition(this));
            return Until(func);
        }

        /// <summary>
        /// Base version of Until() method improved by ability to ignore wait timeout if need be.
        /// </summary>
        /// <typeparam name="T">Type of wait base, typically - IWebDriver.</typeparam>
        /// <typeparam name="TResult">Type of expected result.</typeparam>
        /// <param name="condition">Expected condition.</param>
        /// <returns>Instance of the expected type.</returns>
        public new TResult Until<TResult>(Func<IWebDriver, TResult> condition)
        {
            try
            {
                return base.Until(browser =>
                {
                    try
                    {
                        return condition(_driver);
                    }
                    // TODO: Remove IE Bug when resolved: https://github.com/seleniumhq/selenium-google-code-issue-archive/issues/7524
                    catch (Exception e) when (e.Message.Contains(
                        "Error determining if element is displayed"))
                    {
                        return default(TResult);
                    }
                });
            }
            catch (WebDriverTimeoutException e)
            {
                if (!_ignoredExceptions.Contains(e.GetType()))
                    throw;
                return default(TResult);
            }
        }
    }
}
