using System;
using NUnit.Framework;

namespace UI.Test.Common.Test.Utilities
{
    public static class AssertionExtensions
    {
        public static void MustBeEqualTo(this object actual, object expected, string errorMessage) =>
            Assert.That(actual, Is.EqualTo(expected), errorMessage);

        public static void MustBeTrue(this bool actual, string errorMessage) => Assert.That(actual, Is.True, errorMessage);

        public static void MustBeFalse(this bool actual, string errorMessage) => Assert.That(actual, Is.False, errorMessage);

        public static void MustContain(this string actual, string expected, string errorMessage) =>
            Assert.That(actual, Does.Contain(expected), errorMessage);

        public static void MustThrow<TType>(this TType t, TestDelegate d, string errorMessage)
            where TType : class => Assert.That(d, Throws.Exception);
    }
}
