﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
    {
        private class IntroduceVariableAllOccurrenceCodeAction : AbstractIntroduceVariableCodeAction
        {
            internal IntroduceVariableAllOccurrenceCodeAction(
                TService service,
                SemanticDocument document,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool isConstant,
                bool isLocal,
                bool isQueryLocal)
                : base(service, document, expression, allOccurrences, isConstant, isLocal, isQueryLocal)
            {
            }

            protected override async Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
            {
                var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
                var simplifierOptions = await SimplifierOptions.FromDocumentAsync(document, fallbackOptions: null, cancellationToken).ConfigureAwait(false);

                document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, simplifierOptions, cancellationToken).ConfigureAwait(false);
                document = await Formatter.FormatAsync(document, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
                document = await CaseCorrector.CaseCorrectAsync(document, CaseCorrector.Annotation, cancellationToken).ConfigureAwait(false);
                return document;
            }
        }
    }
}
