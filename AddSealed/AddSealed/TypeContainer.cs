using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;

namespace AddSealed
{
    public class TypeContainer : IEnumerable<INamedTypeSymbol>
    {
        private HashSet<INamedTypeSymbol> _set = new (SymbolEqualityComparer.Default);

        public TypeContainer()
        {
        }

        public bool Add(INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType)
            {
                return _set.Add(symbol.ConstructUnboundGenericType());
            }
            else
            {
                return _set.Add(symbol);
            }
        }

        public bool Contains(INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType)
            {
                return _set.Contains(symbol.ConstructUnboundGenericType());
            }
            else
            {
                return _set.Contains(symbol);
            }
        }

        public IEnumerator<INamedTypeSymbol> GetEnumerator()
        {
            return ((IEnumerable<INamedTypeSymbol>)_set).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_set).GetEnumerator();
        }
    }
}
