using System;
using System.Threading;

namespace MinecraftClient
{
    internal static class MainThreadExecutionScope
    {
        private sealed class ScopeNode(object owner, ScopeNode? parent) : IDisposable
        {
            public object Owner { get; } = owner;
            public ScopeNode? Parent { get; } = parent;

            public void Dispose()
            {
                if (!ReferenceEquals(s_currentScope.Value, this))
                    throw new InvalidOperationException("Main-thread execution scope disposed out of order.");

                s_currentScope.Value = Parent;
            }
        }

        private static readonly AsyncLocal<ScopeNode?> s_currentScope = new();

        public static IDisposable Enter(object owner)
        {
            ScopeNode scopeNode = new(owner, s_currentScope.Value);
            s_currentScope.Value = scopeNode;
            return scopeNode;
        }

        public static bool IsActive(object owner)
        {
            ScopeNode? scopeNode = s_currentScope.Value;
            while (scopeNode is not null)
            {
                if (ReferenceEquals(scopeNode.Owner, owner))
                    return true;

                scopeNode = scopeNode.Parent;
            }

            return false;
        }
    }
}
