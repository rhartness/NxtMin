/*
 *
 * Richard Hartness (RLH), (c) 2014
 * What code I've written can be copied, modified and distributed
 * as desired.  I just request that if this is used, please provide credit, 
 * where credit is due.
 * 
 * If you found this useful, please send donations to:
 * 
 * Nxt:  1102622531
 * BTC:  1Mhk5aKnE6jN7yafQXCdDDm8T9Qoy2sTqS
 * LTC:  LKTF6AjzFj2CG81rQravs164VsoJJnEPmm
 * DOGE: DGea4Qev7eJGmohWq2iKSeDkrTsPeYXQAC
 * 
 */
 
//Comment out this line to not update the hash rate.
#define BENCHMARK

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace NxtMin
{
	class Program
	{

        static string pfx = "";
        static ulong min = ulong.MaxValue;

 		static void Main(string[] args)
		{
            SetArgs(args);
            if (pfx == "") pfx = CreateRandomString(50);
            if (min == ulong.MaxValue)
            {
                Console.Write("Enter the a minimal account number (0 = Max ulong): ");
                string strLim = Console.ReadLine();
                if (strLim != "0" && strLim != "") min = Convert.ToUInt64(strLim);
                else min = ulong.MaxValue;
            }

			Process currentProcess = Process.GetCurrentProcess();
			ProcessPriorityClass oldPriority = currentProcess.PriorityClass;
			try
			{
				Console.Clear();
				currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
	
				//Write output
				Object lockMin = new Object();
 #if BENCHMARK
                DateTime lastBenchmark = DateTime.Now;
                int iterations = 0;
                Object itObject = new Object();
                Console.CursorTop = 0;
                Console.Write("Calculating address creation rate...");
                Console.CursorLeft = 0;
#endif

                Parallel.For(1, Int32.MaxValue, (i, loopState) => 
				{
				
					string secret = pfx + i.ToString();
					ulong addr = CreateAddress(secret);
					if (addr < min)
					{
						lock (lockMin)
						{
							StreamWriter sw = new StreamWriter("nxt.txt", true);
							min = addr;
                            Console.CursorTop = 1;
                            Console.Write("Found!: {0}", addr.ToString().PadRight(30));
                            Console.CursorLeft = 0;
							sw.WriteLine("{0}: PK: {1}, Addr: {2}", DateTime.Now.ToString(), secret, addr);
							sw.Flush();
							sw.Close();
						}
					}
#if BENCHMARK
                    lock (itObject)
                    {
                        iterations++;
                        if (lastBenchmark.AddSeconds(5) < DateTime.Now)
                        {
                            Console.CursorTop = 0;
                            Console.Write("Executing {0:F3} kh/s!".PadRight(50), ((float)iterations) / 5 / 1024);
                            Console.CursorLeft = 0;
                            iterations = 0;
                            lastBenchmark = DateTime.Now;
                        }                        
                    }
#endif
				});
			}
			finally
			{
				//Bring the priority back up to the original level.
				currentProcess.PriorityClass = oldPriority;
			}
		}

        public static void SetArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-"))
                {
                    Console.WriteLine("There was a problem with the your input.  Please run the application with the --help parameter for further assistance.");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
                switch (args[i])
                {
                    case "--prefix":
                    case "-p": pfx = args[++i]; break;
                    case "--min":
                    case "-m":
                        {
                            try
                            {
                                min = Convert.ToUInt64(args[++i]);
                            }
                            catch
                            {
                                Console.WriteLine("There was a problem with the specified minimum account number. Defaulting to Max ulong value.");
                                Console.ReadKey();
                                min = ulong.MaxValue - 1; //The -1 is a cheat.  We do not want to prompt the user again.  Well, I don't.
                            };
                        }
                        break;
                    case "--help":
                    case "-?":
                        Console.Clear();
                        Console.Write(
@"NxtMin : RLHs Nxt Address Generator
This account generator, generates private keys with small Nxt addresses.
Initially the application starts with a minimal account number.  Once a 
smaller account number has been found, the new account and private key 
is exported to a file and the application will begin searching strictly 
for addresses that are smaller than the last one discovered.

Once an address has been found, the application will create a file 
named nxt.txt in the root folder of the application, outputting the 
results.  If a nxt.txt file already exists, the application will append 
new results.

Thank you!

USAGE:

--prefix, -p: 
Choose your own prefix for the private key.  If not provided, NxtMin 
will randomly generate a 50 character private key.

--min, -m:
Set the starting, minimal account value.  If not set, the minimal 
value is the max value of an unsigned long integer.

--help, -?:
Print this help document.

If you've found this application helpful, please send tips to 
1102622531"
                        );
                        Console.ReadKey();
                        Environment.Exit(0);
                        break;
                }
            }
        }
        public static ulong CreateAddress(string privateKey)
        {
            SHA256 sha = SHA256.Create();
            byte[] sha1 = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(privateKey));

            //This needs to be done due to the Curve25519 implementation that's being used.
            sha1[0] &= 248; sha1[31] &= 127; sha1[31] |= 64;

            byte[] pubKey = Elliptic.Curve25519.GetPublicKey(sha1);
            byte[] sha2 = sha.ComputeHash(pubKey);
            return BitConverter.ToUInt64(new byte[] { sha2[0], sha2[1], sha2[2], sha2[3], sha2[4], sha2[5], sha2[6], sha2[7] }, 0);
        }
        private static string CreateRandomString(int length)
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/";
            string result = "";
            Random r = new Random();
            while (result.Length < length) result += chars[r.Next(chars.Length - 1)];
            return result;
        }
	}
}
