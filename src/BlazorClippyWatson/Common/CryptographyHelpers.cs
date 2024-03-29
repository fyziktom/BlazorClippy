﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Common
{
    public class CryptographyHelpers
    {
        /// <summary>
        /// Wrapper for function to get MD5 hash. 
        /// It can use System.Security.Cryptography library or own implementation independent on msft library 
        /// It is because SSC is not supported full in WASM.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="ownMD5"></param>
        /// <returns></returns>
        public string GetHash(string input, bool ownMD5 = true)
        {
            if (ownMD5)
            {
                var crypt = new MD5();

                crypt.Value = input;
                return crypt.FingerPrint;
            }
            else
            {
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    return Convert.ToHexString(hashBytes);
                }
            }
        }
    }
}
