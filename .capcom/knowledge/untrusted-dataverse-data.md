---
title: Treat Dataverse names, XML and git text as untrusted input
labels: [security, invariant]
refs: [Bulk Solution Exporter/Helpers/FileAndFolderHelper.cs#internal static string ToSafeFileNamePart(, Bulk Solution Exporter/Helpers/FileAndFolderHelper.cs#internal static bool IsValidSolutionFilePath(, Bulk Solution Exporter/Helpers/GitHelper.cs#internal static string QuoteArgument(, Bulk Solution Exporter/Helpers/MarkdownParser.cs]
---

The realistic attacker is the data, not a network peer: a hostile or compromised Dataverse environment returning crafted solution names, manifest XML or import-error XML, plus git text (branch names, repo contents).

- Solution unique/friendly names never go into a path unsanitized: use `FileAndFolderHelper.ToSafeFileNamePart` and `IsValidSolutionFilePath` (rejects traversal, drive prefixes, paths outside the chosen folder). Validate on load, not only in the file wizard.
- Git is never fed a hand-built command line: arguments go through `GitHelper.QuoteArgument`, the commit message goes via stdin. stdout/stderr are drained without deadlocking.
- Log input is length-capped and the markdown renderer's recursion is bounded (a `StackOverflowException` would kill the XrmToolBox host). Escaping of untrusted text is still open: issue `escape-log-markdown`.
