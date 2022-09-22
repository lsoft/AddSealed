using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AddSealed
{
    public static class RoslynHelper
    {
        public static bool AnyTypeIs(
            this INamespaceSymbol @namespace,
            Func<INamedTypeSymbol, bool> predicate
            )
        {
            if (@namespace == null)
            {
                throw new ArgumentNullException(nameof(@namespace));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            foreach (var type in @namespace.GetTypeMembers())
            {
                if (predicate(type))
                {
                    return true;
                }

                foreach (var nestedType in type.GetNestedTypes(predicate))
                {
                    if (predicate(nestedType))
                    {
                        return true;
                    }

                }
            }

            foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
            {
                if (nestedNamespace.AnyTypeIs(predicate))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<INamedTypeSymbol> GetAllTypes(
            this INamespaceSymbol @namespace,
            Func<INamedTypeSymbol, bool> predicate
            )
        {
            if (@namespace == null)
            {
                throw new ArgumentNullException(nameof(@namespace));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            foreach (var type in @namespace.GetTypeMembers())
            {
                foreach (var nestedType in type.GetNestedTypes(predicate))
                {
                    yield return nestedType;
                }
            }

            foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
            {
                foreach (var type in nestedNamespace.GetAllTypes(predicate))
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetNestedTypes(
            this INamedTypeSymbol type,
            Func<INamedTypeSymbol, bool> predicate
            )
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (predicate(type))
            {
                yield return type;
            }

            foreach (var nestedType in type.GetTypeMembers().SelectMany(nestedType => nestedType.GetNestedTypes(predicate)))
            {
                yield return nestedType;
            }
        }
    }
}