﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

namespace System.Windows.Media
{
    #region ImageMetadata

    /// <summary>
    /// Metadata for ImageSource.
    /// </summary>
    public abstract partial class ImageMetadata : Freezable
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        internal ImageMetadata()
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        ///     Shadows inherited Copy() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new ImageMetadata Clone()
        {
            return (ImageMetadata)base.Clone();
        }

        #endregion
    }

    #endregion
}
