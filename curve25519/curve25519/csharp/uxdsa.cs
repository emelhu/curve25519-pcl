﻿/** 
 * Copyright (C) 2016 golf1052
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace org.whispersystems.curve25519.csharp
{
    public class uxdsa
    {
        public static int uxdsa_sign(ISha512 sha512provider,
            byte[] signature_out,
            byte[] curve25519_privkey,
            byte[] msg, int msg_len,
            byte[] random)
        {
            byte[] a = new byte[32];
            byte[] A = new byte[32];

            Ge_p3 Bu = new Ge_p3();
            Ge_p3 ed_pubkey_point = new Ge_p3();
            byte[] sigbuf = new byte[crypto_additions.MAX_MSG_LEN + 160]; /* working buffer */
            byte sign_bit = 0;

            if (msg_len > crypto_additions.MAX_MSG_LEN)
            {
                //memset(signature_out, 0, 96);
                return -1;
            }

            /* Convert the Curve25519 privkey to an Ed25519 public key */
            Ge_scalarmult_base.ge_scalarmult_base(ed_pubkey_point, curve25519_privkey);
            Ge_p3_tobytes.ge_p3_tobytes(A, ed_pubkey_point);

            /* Force Edwards sign bit to zero */
            sign_bit = (byte)(A[31] & 0x80);
            if (sign_bit != 0)
            {
                Sc_neg.sc_neg(a, curve25519_privkey);
                A[31] &= 0x7F;
            }
            else
            {
                Array.Copy(curve25519_privkey, 0, a, 0, 32);
            }

            Elligator.calculate_Bu_and_U(sha512provider, Bu, signature_out, sigbuf, a, msg, msg_len);

            /* Perform an Ed25519 signature with explicit private key */
            usign_modified.crypto_usign_modified(sha512provider, sigbuf, msg, msg_len, a, A, random, Bu, signature_out /*U*/);
            Array.Copy(sigbuf, 0, signature_out, 32, 64);
            return 0;
        }

        public static int uxdsa_verify(ISha512 sha512provider, byte[] signature, byte[] curve25519_pubkey, byte[] msg, int msg_len)
        {
            int[] mont_x = new int[10];
            int[] ed_y = new int[10];

            byte[] ed_pubkey = new byte[32];
            long some_retval = 0;
            byte[] verifybuf = new byte[crypto_additions.MAX_MSG_LEN + 160]; /* working buffer */
            byte[] verifybuf2 = new byte[crypto_additions.MAX_MSG_LEN + 160]; /* working buffer #2 ?? !!! */
            Ge_p3 Bu = new Ge_p3();

            if (msg_len > crypto_additions.MAX_MSG_LEN)
            {
                return -1;
            }

            Elligator.calculate_Bu(sha512provider, Bu, verifybuf, msg, msg_len);

            /* Convert the Curve25519 public key into an Ed25519 public key.
             * 
             * ed_y = (mont_x - 1) / (mont_x + 1)
             * 
             * NOTE: mont_x=-1 is converted to ed_y=0 since fe_invert is mod-exp
             */
            Fe_frombytes.fe_frombytes(mont_x, curve25519_pubkey);
            Fe_montx_to_edy.fe_montx_to_edy(ed_y, mont_x);
            Fe_tobytes.fe_tobytes(ed_pubkey, ed_y);

            Array.Copy(signature, 0, verifybuf, 0, 96);
            Array.Copy(msg, 0, verifybuf, 96, msg_len);

            /* Then perform a signature verification, return 0 on success */
            /* The below call has a strange API: */
            /* verifybuf = U || h || s || message */
            /* verifybuf2 = internal to next call gets a copy of verifybuf, S gets
             * replaced with pubkey for hashing, then the whole thing gets zeroized
             * (if bad sig), or contains a copy of msg (good sig) */
            return uopen_modified.crypto_usign_open_modified(sha512provider, verifybuf2, some_retval, verifybuf, 96 + msg_len, ed_pubkey, Bu);
        }
    }
}