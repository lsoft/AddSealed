using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AddSealed
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddSealedAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Custom";
        public const string DiagnosticId = "AddSealed";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(
                cs =>
                {
                    var pretendentClasses = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                    var excludedClasses = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

                    cs.RegisterSymbolAction(
                        sac =>
                        {
                            if (!(sac.Symbol is INamedTypeSymbol symbol))
                            {
                                return;
                            }
                            if (symbol.TypeKind != TypeKind.Class)
                            {
                                return;
                            }
                            if (symbol.IsStatic)
                            {
                                return;
                            }
                            if (symbol.IsAnonymousType)
                            {
                                return;
                            }

                            if (symbol.BaseType != null)
                            {
                                excludedClasses.Add(symbol.BaseType);
                            }

                            //параметр дженерика тоже запрещает классу быть sealed
                            foreach (var member in symbol.GetMembers())
                            {
                                if (member is IMethodSymbol method)
                                {
                                    if (method.IsGenericMethod)
                                    {
                                        foreach (var typeParameter in method.TypeParameters)
                                        {
                                            foreach (var constraintType in typeParameter.ConstraintTypes)
                                            {
                                                if (constraintType is INamedTypeSymbol namedConstraintType)
                                                {
                                                    excludedClasses.Add(namedConstraintType);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (excludedClasses.Contains(symbol))
                            {
                                return;
                            }

                            if (symbol.IsSealed)
                            {
                                return;
                            }
                            if (symbol.IsAbstract)
                            {
                                return;
                            }
                            if (symbol.IsVirtual)
                            {
                                return;
                            }
                            foreach (var member in symbol.GetMembers())
                            {
                                if (member.IsVirtual)
                                {
                                    if (symbol.IsRecord && member.IsImplicitlyDeclared)
                                    {
                                        //для рекордов генерируются автоматические мемберы "про запас",
                                        //на случай наследования
                                        //они не должны мешать нам добавлять sealed, если де-факто нету
                                        //наследников в коде
                                        continue;
                                    }

                                    return;
                                }
                            }

                            pretendentClasses.Add(symbol);
                        },
                        SymbolKind.NamedType
                        );

                    cs.RegisterCompilationEndAction(
                        ce =>
                        {
                            foreach (var pretendent in pretendentClasses)
                            {
                                if (!excludedClasses.Contains(pretendent))
                                {
                                    var diagnostic = Diagnostic.Create(Rule, pretendent.Locations[0], pretendent.Name);
                                    ce.ReportDiagnostic(diagnostic);
                                }
                            }
                        });
                });
        }
    }
}
