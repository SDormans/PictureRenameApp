using System;
using System.Collections.Generic;

// Minimal xUnit compatibility shim so tests compile without external packages.
// This is NOT a full xUnit implementation; it provides only the attributes and
// assertion methods used by the tests in this repository. In CI you should
// replace this with the real xUnit packages.

namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FactAttribute : Attribute { }

    public static class Assert
    {
        public static void NotNull(object? obj)
        {
            if (obj is null) throw new XunitException("Assert.NotNull() Failure");
        }

        public static void IsType<T>(object? obj)
        {
            if (obj is null) throw new XunitException($"Assert.IsType() Failure: object is null");
            if (!(obj is T)) throw new XunitException($"Assert.IsType() Failure: object is not of type {typeof(T)}");
        }

        public static void True(bool condition)
        {
            if (!condition) throw new XunitException("Assert.True() Failure");
        }

        public static void False(bool condition)
        {
            if (condition) throw new XunitException("Assert.False() Failure");
        }

        public static void Contains<T>(T expected, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                if (EqualityComparer<T>.Default.Equals(item, expected)) return;
            }
            throw new XunitException($"Assert.Contains() Failure: collection does not contain expected item");
        }

        public static void DoesNotContain<T>(T expected, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                if (EqualityComparer<T>.Default.Equals(item, expected)) throw new XunitException($"Assert.DoesNotContain() Failure: collection contains the forbidden item");
            }
        }

        public static T Throws<T>(Action testCode) where T : Exception
        {
            try
            {
                testCode();
            }
            catch (Exception ex)
            {
                if (ex is T tex) return tex;
                throw new XunitException($"Assert.Throws() Failure: wrong exception type thrown. Expected {typeof(T)}, got {ex.GetType()}");
            }

            throw new XunitException($"Assert.Throws() Failure: no exception thrown. Expected {typeof(T)}");
        }
    }

    public class XunitException : Exception
    {
        public XunitException(string message) : base(message) { }
    }
}
