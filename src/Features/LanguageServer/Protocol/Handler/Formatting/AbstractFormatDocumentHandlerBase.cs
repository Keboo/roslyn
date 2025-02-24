﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractFormatDocumentHandlerBase<RequestType, ResponseType> : AbstractStatelessRequestHandler<RequestType, ResponseType>
    {
        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        protected static async Task<LSP.TextEdit[]?> GetTextEditsAsync(
            RequestContext context,
            LSP.FormattingOptions options,
            CancellationToken cancellationToken,
            LSP.Range? range = null)
        {
            var document = context.Document;
            if (document == null)
                return null;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var rangeSpan = (range != null) ? ProtocolConversions.RangeToTextSpan(range, text) : new TextSpan(0, root.FullSpan.Length);
            var formattingSpan = CommonFormattingHelpers.GetFormattingSpan(root, rangeSpan);

            // We should use the options passed in by LSP instead of the document's options.
            var formattingOptions = await ProtocolConversions.GetFormattingOptionsAsync(options, document, cancellationToken).ConfigureAwait(false);

            var services = document.Project.Solution.Workspace.Services;
            var textChanges = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(formattingSpan), services, formattingOptions, rules: null, cancellationToken);

            var edits = new ArrayBuilder<LSP.TextEdit>();
            edits.AddRange(textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, text)));
            return edits.ToArrayAndFree();
        }
    }
}
