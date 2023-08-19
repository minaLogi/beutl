﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Runtime.Loader
{
    internal partial struct LibraryNameVariation
    {
        private static readonly string LibraryNamePrefix = "lib";
        //#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
        //        private const string LibraryNameSuffix = ".dylib";
        //#else
        //        private const string LibraryNameSuffix = ".so";
        //#endif
        private static readonly string LibraryNameSuffix;

        static LibraryNameVariation()
        {
            if(OperatingSystem.IsMacOS()
                || OperatingSystem.IsMacCatalyst()
                || OperatingSystem.IsIOS()
                || OperatingSystem.IsTvOS())
            {
                LibraryNameSuffix = ".dylib";
            }
            else if(OperatingSystem.IsWindows())
            {
                LibraryNameSuffix = ".dll";
                LibraryNamePrefix = "";
            }
            else
            {
                LibraryNameSuffix = ".so";
            }
        }

        internal static IEnumerable<LibraryNameVariation> DetermineLibraryNameVariations(string libName, bool isRelativePath)
        {
            // This is a copy of the logic in DetermineLibNameVariations in dllimport.cpp in CoreCLR

            if (!isRelativePath)
            {
                yield return new LibraryNameVariation(string.Empty, string.Empty);
            }
            else
            {
                bool containsSuffix = false;
                int indexOfSuffix = libName.IndexOf(LibraryNameSuffix, StringComparison.OrdinalIgnoreCase);
                if (indexOfSuffix >= 0)
                {
                    indexOfSuffix += LibraryNameSuffix.Length;
                    containsSuffix = indexOfSuffix == libName.Length || libName[indexOfSuffix] == '.';
                }

                bool containsDelim = libName.Contains(Path.DirectorySeparatorChar);

                if (containsSuffix)
                {
                    yield return new LibraryNameVariation(string.Empty, string.Empty);
                    if (!containsDelim)
                    {
                        yield return new LibraryNameVariation(LibraryNamePrefix, string.Empty);
                    }
                    yield return new LibraryNameVariation(string.Empty, LibraryNameSuffix);
                    if (!containsDelim)
                    {
                        yield return new LibraryNameVariation(LibraryNamePrefix, LibraryNameSuffix);
                    }
                }
                else
                {
                    yield return new LibraryNameVariation(string.Empty, LibraryNameSuffix);
                    if (!containsDelim)
                    {
                        yield return new LibraryNameVariation(LibraryNamePrefix, LibraryNameSuffix);
                    }
                    yield return new LibraryNameVariation(string.Empty, string.Empty);
                    if (!containsDelim)
                    {
                        yield return new LibraryNameVariation(LibraryNamePrefix, string.Empty);
                    }
                }
            }
        }
    }
}
