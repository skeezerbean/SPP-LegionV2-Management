using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

// Pulled from https://stackoverflow.com/a/35473966/11673183
namespace SPP_LegionV2_Management.Extensions
{
	/// <summary>
	/// Provides unsafe temporary operations on secured strings.
	/// </summary>
	[SuppressUnmanagedCodeSecurity]
	public static class SecureStringExtensions
	{
		/// <summary>
		/// Converts a secured string to an unsecured string.
		/// </summary>
		public static string ToUnsecuredString(this SecureString secureString)
		{
			// copy&paste from the internal System.Net.UnsafeNclNativeMethods
			IntPtr bstrPtr = IntPtr.Zero;
			if (secureString != null)
			{
				if (secureString.Length != 0)
				{
					try
					{
						bstrPtr = Marshal.SecureStringToBSTR(secureString);
						return Marshal.PtrToStringBSTR(bstrPtr);
					}
					finally
					{
						if (bstrPtr != IntPtr.Zero)
							Marshal.ZeroFreeBSTR(bstrPtr);
					}
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Copies the existing instance of a secure string into the destination, clearing the destination beforehand.
		/// </summary>
		public static void CopyInto(this SecureString source, SecureString destination)
		{
			destination.Clear();
			foreach (var chr in source.ToUnsecuredString())
			{
				destination.AppendChar(chr);
			}
		}

		/// <summary>
		/// Converts an unsecured string to a secured string.
		/// </summary>
		public static SecureString ToSecuredString(this string plainString)
		{
			if (string.IsNullOrEmpty(plainString))
			{
				return new SecureString();
			}

			SecureString secure = new SecureString();
			foreach (char c in plainString)
			{
				secure.AppendChar(c);
			}
			return secure;
		}

		// This is used to compute a new SHA256 password hash for the bnet account, and
		// is REQUIRED if the bnet account name changed, as the 2 are tied together
		public static string sha256_hash(string randomString, bool reverse = false)
		{
			var crypt = new SHA256Managed();
			byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));

			// clear our potential password as quickly as possible
			randomString = string.Empty;

			var hash = new System.Text.StringBuilder();

			foreach (byte theByte in crypto)
				hash.Append(theByte.ToString("x2"));

			// the 2nd time hashing the password needs the string reversed
			if (reverse)
			{
				string a = hash.ToString();
				char[] ca = a.ToCharArray();
				StringBuilder sb = new StringBuilder(a.Length);
				for (int i = 0; i < a.Length; i += 2)
					sb.Insert(0, ca, i, 2);

				return sb.ToString();
			}

			return hash.ToString();
		}
	}
}