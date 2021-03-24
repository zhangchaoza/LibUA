﻿// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Cryptography;

namespace LibUA.Security.Cryptography
{
    /// <summary>
    ///     <para>
    ///         The RNGCng class provides a managed wrapper around the CNG random number generator. It
    ///         provides the same interface as the other cryptographic random number generator implementation
    ///         shipped with the .NET Framework, <see cref="RNGCryptoServiceProvider" />.
    ///     </para>
    ///     <para>
    ///         RNGCng uses the BCrypt layer of CNG to do its work, and requires Windows Vista and the .NET
    ///         Framework 3.5.
    ///     </para>
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "RNG", Justification = "This is for consistency with the existing RNGCryptoServiceProvider type")]
    public sealed class RNGCng : RandomNumberGenerator, ICngAlgorithm, IDisposable
    {
        private SafeBCryptAlgorithmHandle m_algorithm;
        private CngProvider m_implementation;

        private static RNGCng s_rngCng = new RNGCng();

        /// <summary>
        ///     Creates a new instance of a random number generator object using the Microsoft Primitive
        ///     Algorithm Provider.
        /// </summary>
        public RNGCng() : this(CngProvider2.MicrosoftPrimitiveAlgorithmProvider)
        {
        }

        /// <summary>
        ///     Creates a new instance of a random number generator object using the specified
        ///     algorithm provider.
        /// </summary>
        /// <param name="algorithmProvider">algorithm provider to use for random number generation</param>
        /// <exception cref="ArgumentNullException">if <paramref name="algorithmProvider"/> is null</exception>
        [SecurityCritical]
        [SecuritySafeCritical]
        public RNGCng(CngProvider algorithmProvider)
        {
            if (algorithmProvider == null)
                throw new ArgumentNullException("algorithmProvider");

            m_algorithm = BCryptNative.OpenAlgorithm(BCryptNative.AlgorithmName.Rng,
                                                     algorithmProvider.Provider);

            m_implementation = algorithmProvider;
        }

        public CngProvider Provider
        {
            get { return m_implementation; }
        }

        /// <summary>
        ///     Static random number generator that can be shared within the AppDomain
        /// </summary>
        internal static RNGCng StaticRng
        {
            get { return s_rngCng; }
        }

        [SecurityCritical]
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Safe use of Dispose")]
        public new void Dispose()
        {
            if (m_algorithm != null)
            {
                m_algorithm.Dispose();
            }
        }

        /// <summary>
        ///     Helper function to generate a random key value using the static RNG
        /// </summary>
        internal static byte[] GenerateKey(int size)
        {
            Debug.Assert(size > 0, "size > 0");

            byte[] key = new byte[size];
            StaticRng.GetBytes(key);
            return key;
        }

        /// <summary>
        ///     <para>
        ///         GetBytes fills the input data array with randomly generated bytes. The input values of the
        ///         array are ignored.
        ///     </para>
        ///     <para>
        ///         This method is thread safe.
        ///     </para>
        /// </summary>
        /// <param name="data">array to fill with randomly generated bytes</param>
        /// <exception cref="ArgumentNullException">if <paramref name="data"/> is null</exception>
        [SecurityCritical]
        [SecuritySafeCritical]
        public override void GetBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            BCryptNative.GenerateRandomBytes(m_algorithm, data);
        }

        /// <summary>
        ///     GetNonZeroBytes is not implemented by the RNGCng class.
        /// </summary>
        /// <exception cref="NotImplementedException">GetNonZeroBytes is not implemented</exception>
        public override void GetNonZeroBytes(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
