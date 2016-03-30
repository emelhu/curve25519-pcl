using System;

namespace org.whispersystems.curve25519.csharp
{

    public class Open
    {

        //CONVERT #include <string.h>
        //CONVERT #include "crypto_sign.h"
        //CONVERT #include "crypto_hash_sha512.h"
        //CONVERT #include "crypto_verify_32.h"
        //CONVERT #include "ge.h"
        //CONVERT #include "sc.h"

        public static int crypto_sign_open(
          Sha512 sha512provider,
          byte[] m, long mlen,
          byte[] sm, long smlen,
          byte[] pk
        )
        {
            byte[] pkcopy = new byte[32];
            byte[] rcopy = new byte[32];
            byte[] scopy = new byte[32];
            byte[] h = new byte[64];
            byte[] rcheck = new byte[32];
            Ge_p3 A = new Ge_p3();
            Ge_p2 R = new Ge_p2();

            if (smlen < 64) return -1;
            if ((sm[63] & 224) != 0) return -1;
            if (Ge_frombytes.ge_frombytes_negate_vartime(A, pk) != 0) return -1;

            byte[] pubkeyhash = new byte[64];
            sha512provider.calculateDigest(out pubkeyhash, pk, 32);

            Array.Copy(pk, 0, pkcopy, 0, 32);
            Array.Copy(sm, 0, rcopy, 0, 32);
            Array.Copy(sm, 32, scopy, 0, 32);

            Array.Copy(sm, 0, m, 0, (int)smlen);
            Array.Copy(pkcopy, 0, m, 32, 32);
            sha512provider.calculateDigest(out h, m, smlen);
            Sc_reduce.sc_reduce(h);

            Ge_double_scalarmult.ge_double_scalarmult_vartime(R, h, A, scopy);
            Ge_tobytes.ge_tobytes(rcheck, R);
            if (Crypto_verify_32.crypto_verify_32(rcheck, rcopy) == 0)
            {
                Array.Copy(m, 64, m, 0, (int)(smlen - 64));
                //memset(m + smlen - 64,0,64);
                return 0;
            }

            //badsig:
            //memset(m,0,smlen);
            return -1;
        }


    }
}