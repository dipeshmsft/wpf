// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MS.Internal
{
    /// <summary>
    /// A typed exception to allow parse errors on AssemblyVersions to flow to
    /// the MarkupCompile task execution.
    /// </summary>
    internal class AssemblyVersionParseException : Exception
    {
        public AssemblyVersionParseException(string message) : base(message) { }
    }
}

