using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AddSealed
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddSealedCodeFixProvider)), Shared]
    public class AddSealedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AddSealedAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<TypeDeclarationSyntax>()
                .First()
                ;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => AddSealedAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> AddSealedAsync(Document document, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
        {
            var sealeds = SyntaxFactory
                .Token(SyntaxKind.SealedKeyword)
                .WithTrailingTrivia(
                    SyntaxFactory.Whitespace(" ")
                    );

            TypeDeclarationSyntax toReplaceNode;
            if (typeDeclaration.Modifiers.Count == 0)
            {
                var newModifiers = typeDeclaration.Modifiers.Insert(0, sealeds);

                toReplaceNode = typeDeclaration
                    .WithoutTrivia()
                    .WithModifiers(newModifiers)
                    .WithLeadingTrivia(typeDeclaration.GetLeadingTrivia())
                    .WithTrailingTrivia(typeDeclaration.GetTrailingTrivia())
                    ;
            }
            else
            {
                var partialIndex = FindIndexFor(typeDeclaration.Modifiers, m => m.Kind() == SyntaxKind.PartialKeyword);

                if (partialIndex < 0)
                {
                    var newModifiers = typeDeclaration.Modifiers.Add(sealeds);

                    toReplaceNode = typeDeclaration
                        .WithoutTrivia()
                        .WithModifiers(newModifiers)
                        .WithLeadingTrivia(typeDeclaration.GetLeadingTrivia())
                        .WithTrailingTrivia(typeDeclaration.GetTrailingTrivia())
                        ;
                }
                else
                {
                    sealeds = sealeds.WithLeadingTrivia(
                        typeDeclaration.Modifiers[partialIndex].LeadingTrivia
                        );

                    var partials = typeDeclaration.Modifiers[partialIndex];
                    var partialWithoutTrivia = partials.WithLeadingTrivia();

                    var modifiers = typeDeclaration.Modifiers.Replace(
                        partials,
                        partialWithoutTrivia
                        );

                    var newModifiers = modifiers.Insert(partialIndex, sealeds);

                    toReplaceNode = typeDeclaration
                        .WithoutTrivia()
                        .WithModifiers(newModifiers)
                        .WithLeadingTrivia(typeDeclaration.GetLeadingTrivia())
                        .WithTrailingTrivia(typeDeclaration.GetTrailingTrivia())
                        ;
                }
            }

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = oldRoot.ReplaceNode(
                typeDeclaration,
                toReplaceNode
                );

            // Return document with transformed tree.
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private static int FindIndexFor(SyntaxTokenList stl, Func<SyntaxToken, bool> predicate)
        {
            for (var i = 0; i < stl.Count; i++)
            {
                var syntax = stl[i];
                if (predicate(syntax))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
