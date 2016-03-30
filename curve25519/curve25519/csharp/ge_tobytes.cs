namespace org.whispersystems.curve25519.csharp
{

    public class Ge_tobytes
    {

        //CONVERT #include "ge.h"

        public static void ge_tobytes(byte[] s, Ge_p2 h)
        {
            int[] recip = new int[10];
            int[] x = new int[10];
            int[] y = new int[10];

            Fe_invert.fe_invert(recip, h.Z);
            Fe_mul.fe_mul(x, h.X, recip);
            Fe_mul.fe_mul(y, h.Y, recip);
            Fe_tobytes.fe_tobytes(s, y);
            s[31] = (byte)(s[31] ^ Fe_isnegative.fe_isnegative(x) << 7);
        }
    }
}