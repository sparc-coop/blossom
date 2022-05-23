// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "ApiEndpoints101:Endpoint has more than one public method", Justification = "ExecuteAsync is preferred nomenclature for main endpoint function", Scope = "member", Target = "~M:Sparc.Kernel.PublicFeature`2.ExecuteAsync(`0)~System.Threading.Tasks.Task{`1}")]
