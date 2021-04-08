/*
 * ParseEpsteinLolitaExpressFlightLogs.cs
 * 
 * Per a GAB post, I downloaded this PDF, and saved it as a text file:
 * 
 * https://documentcloud.adobe.com/link/track?uri=urn:aaid:scds:US:fefeabf5-fd48-48e0-9919-ad0142158218
 * 
 * 
 * It looks like newlines are more of a distraction than an help:
 * 
ID Date Year Aircraft Model Aircraft Tail # Aircraft Type # of Seats DEP: Code ARR: Code DEP ARR Flight_No. Pass # Unique ID First Name Last Name Last, First First Last Comment Initials Known Data Source
4276
11/17/1995 1995 G-1159B N908JE Jet 22 CMH PBI Columbus, OH, United States West Palm Beach, FL, United States 779 Pass 1 35020-G-1159B-N908JE-CMH-PBI-779-Pass 1 Jeff
Epstein Epstein, Jeff
Jeff Epstein JE Yes Flight Log
4277
 * 
 * So split the whole de-CRLF'ed body upon (ID Date and year strings).
 * 
 * 
 * There is also some occasional (Pg number, FF) therein, this last one:
 * 

116


 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ParseEpsteinLolitaExpressFlightLogs
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			
			try
			{
				doit();
			}
			catch(Exception e)
			{
				Console.Error.WriteLine(e.ToString());
			}
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		

		static Regex reRidCRLFs = new Regex(" *[\r\n]+ *", RegexOptions.Compiled);

		static Regex reRidPgNumFF = new Regex(" *[0-9]+ *\f+ *", RegexOptions.Compiled);

		static Regex reRidXsSpcs = new Regex("  +", RegexOptions.Compiled);

		static Regex reRidHeaders = new Regex("ID Date Year Aircraft Model Aircraft Tail # Aircraft Type # of Seats DEP: Code ARR: Code DEP ARR Flight_No. Pass # Unique ID First Name Last Name Last, First First Last Comment Initials Known Data Source ", RegexOptions.Compiled);

		// I had to drop the absolute requirement of an ID field, some ID fileds are missing.
		static Regex reFirst3FieldsSplit = new Regex("([0-9]* [0-9][0-9]?/[0-9][0-9]?/[0-9][0-9][0-9][0-9] [0-9][0-9][0-9][0-9] )", RegexOptions.Compiled);

		// gimme named capture groups: oops, a few slips twixt...
		static Regex reFirst3FieldsAgain = new Regex("(?<id>[0-9]*) (?<date>[0-9][0-9]?/[0-9][0-9]?/[0-9][0-9][0-9][0-9]) (?<year>[0-9][0-9][0-9][0-9]) ", RegexOptions.Compiled);


		// Unique ID has 8 hyphens, maybe (always?) one space near end.
		// Allow lowercases in final "No Records" or "Pass #"
		// Or better, ensure it is one of those two things.
		// Allow the 7th group may be missing, empty, hence *.
		// make (...) into (?:...) non-capture groups
		static Regex reUniqueId = new Regex(" (?:[0-9A-Z]+-){6}[0-9A-Z]*-(?:No Records|Pass [0-9]+) ", RegexOptions.Compiled);

		// work on the right side of that split, with the names.
		// rid the tail end: initials, yes/no, source:
		static Regex reRidInitialsBoolSrc = new Regex("[?A-Z]{2} (Yes|No) (Flight Log|FOIA)", RegexOptions.Compiled);

		// namish - E.g., "Sarah Kellen Kellen, Sarah Sarah Kellen"
		// grab the easiest mostest first: 2spcs, commaspace, 2spcs
		// allow hyphens, and allow QM in place of a name.
		// static Regex reEasyNamish = new Regex("[A-Za-z]+ [A-Za-z]+ [A-Za-z]+, [A-Za-z]+ [A-Za-z]+ [A-Za-z]+", RegexOptions.Compiled);
		static Regex reEasyNamish = new Regex("(?<fn1>[-A-Za-z]+|\\?) (?<ln1>[-A-Za-z]+|\\?) (?<ln2>[-A-Za-z]+|\\?), (?<fn2>[-A-Za-z]+|\\?) (?<fn3>[-A-Za-z]+|\\?) (?<ln3>[-A-Za-z]+|\\?)", RegexOptions.Compiled);

		// tabulate "FirstName LastName" counts:
		static Dictionary<string,int> NameQty = new Dictionary<string, int>();
		
		static void doit()
		{
			DateTime runAt = DateTime.Now;
			string runAtStr = runAt.ToString("yyyy-MM-ddTHHmmss.fff");
			string topDirectory = @"C:\A\C#2021\ParseEpsteinLolitaExpressFlightLogs";
			string inputFilePath = Path.Combine(topDirectory, "flight_logs.txt");
			string outputFilePath = Path.Combine(topDirectory, runAtStr + ".txt");

			// string [] lines = File.ReadAllLines(inputFilePath);
			// File.WriteAllLines(outputFilePath, lines);

			// First, some general massages of the whole body
			
			string body1 = File.ReadAllText(inputFilePath);
			Console.WriteLine("" + body1.Length);
			string body2 = reRidCRLFs.Replace(body1, " "); // rid 34000 chars
			Console.WriteLine("" + body2.Length);
			string body3 = reRidPgNumFF.Replace(body2, " "); // rid 588 chars
			Console.WriteLine("" + body3.Length);
			string body4 = reRidXsSpcs.Replace(body3, " "); // rid 0 chars
			Console.WriteLine("" + body4.Length);
			string body5 = reRidHeaders.Replace(body4, " "); // rid 24548 chars
			Console.WriteLine("" + body5.Length);

			// now on to splitting apart the records

			// hmmm. length of 1 means my bad regex.
			// baby steps:
			// regex "/" created 10004 length.
			// regex "(/)" created 20007 length.
			// regex "(/[0-9][0-9]?/)" created 10003 length.
			// So, there are 5001 records in this document.
			// Go on to flesh out the first 3 fields again.
			string[] parts = reFirst3FieldsSplit.Split(body5);
			//Console.WriteLine("" + parts.Length);
			//Console.WriteLine("" + parts[0].Length); // 1 - huh, who cares?
			//Console.WriteLine("" + parts[1].Length); // 21
			//Console.WriteLine("" + parts[2].Length); // 202
			////...
			//Console.WriteLine("" + parts[parts.Length-2].Length); // 21
			//Console.WriteLine("" + parts[parts.Length-1].Length); // 181

			// Just anal self-checking of first 3.
			// Overwrought due to regex Again typo.
			int idEmpty = 0;
			int idLow = int.MaxValue;
			int idHigh = int.MinValue;
			for(int i = 1; i < parts.Length; i+=2)
			{
				Match m = reFirst3FieldsAgain.Match(parts[i]);
				string idStr = m.Groups["id"].Value;
				if(string.IsNullOrEmpty(idStr))
					idEmpty ++;
				else
				{
					// Huh, still throws? Duh, bad Again regex!
					int id = -1;
					if(int.TryParse(idStr, out id) == false)
					{
						Console.WriteLine("Bad ID = [" + idStr + "]");
					}
					else
					{
						if(idLow > id)
							idLow = id;
						if(idHigh < id)
							idHigh = id;
					}
				}
			}
			Console.WriteLine("idEmpty = " + idEmpty); // 399
			Console.WriteLine("idLow = " + idLow); // 1
			Console.WriteLine("idHigh = " + idHigh); //5001


			// let's process the richer info in parts[evens]:
			
			int nGoodNames = 0;
			
			for(int i = 2; i < parts.Length; i+=2)
			{
				string rico = parts[i];

				// Let's split on Unique ID
				// Unique ID has 8 hyphens, maybe one space near end.

				Match mu = reUniqueId.Match(rico);
				if(mu.Success == false)
				{
					// Console.WriteLine(rico);
					// Just 4 non-matching oddballs out of 5001 records:
					//G-1159B N908JE Jet 22 TEb PBI Teterboro, NJ, United States West Palm Beach, FL, United States 788 Pass 1 35068-G-1159B-N908JE-TEb-PBI-788-Pass 1 Jeff Epstein Epstein, Jeff Jeff Epstein JE Yes Flight Log
					//G-1159B N908JE Jet 22 TEb PBI Teterboro, NJ, United States West Palm Beach, FL, United States 788 Pass 2 35068-G-1159B-N908JE-TEb-PBI-788-Pass 2 Sophie Biddle Biddle, Sophie Sophie Biddle SB Yes Flight Log
					//EC120B 121TH Helicopter 5 PBI PBI West Palm Beach, FL, United States West Palm Beach, FL, United States Pass 1 36579-EC120B-121TH-PBI-PBI--Pass 1 Marx Mcafee Mcafee, Marx Marx Mcafee MM Yes Flight Log
					//C-421B N908GM Fixed Wing 8 PBI AVO West Palm Beach, FL, United States Avon Park, FL, United States Pass 1 36961-C-421B-N908GM-PBI-AVO --Pass 1 Chris Wagner Wagner, Chris Chris Wagner (Pilot) CW Yes Flight Log
					// Two lowercase, one extra space, one short a hyphen group
					// I could adjust regex. Close enough for jazz.
				}
				else
				{
					// Let's split on Unique ID
					string[] leftRight = reUniqueId.Split(rico);
					if(leftRight.Length != 2)
					{
						Console.WriteLine("" + leftRight.Length + ": " + rico);
					}
					else
					{
						// continue with the 4997 good records...
						// work on the name in right side
						string right = leftRight[1];

						Match mi = reRidInitialsBoolSrc.Match(right);
						if(mi.Success == false)
						{
							// Console.WriteLine(right);
							// 178 records dropped out; None interesting:
							// 48 Female (1) Female (1) Female (1) ? No Flight Log
							// 22 Male (1) Male (1) Male (1) ? No Flight Log
							// 20 Female (2) Female (2) Female (2) ? No Flight Log
							// 16 Reposition Reposition Reposition ? No Flight Log
							// 14 Nanny (1) Nanny (1) Nanny (1) ? No Flight Log
							// 14 Illegible Illegible Illegible ? No Flight Log
							// 10 Passenger(0) Passenger (0) Passenger (0) ? No Flight Log
							// 6 Passenger(1) Passenger (1) Passenger (1) ? No Flight Log
							// 4 Aunts (2) Aunts (2) Aunts (2) ? No Flight Log
							// 3 Passenger(4) Passenger (4) Passenger (4) ? No Flight Log
							// 3 Passenger(3) Passenger (3) Passenger (3) ? No Flight Log
							// 3 Passenger(0) Passenger (0) Passenger (0) Test Flight ? No Flight Log
							// 2 Nanny (2) Nanny (2) Nanny (2) ? No Flight Log
							// 2 Male (2) Male (2) Male (2) (ITALIAN ? No Flight Log
							// 2 Inspection Inspection Inspection ? No Flight Log
							// 1 WIFE? WIFE? WIFE? ? No Flight Log
							// 1 Passenger(2) Passenger (2) Passenger (2) ? No Flight Log
							// 1 Nadia Nadia Nadia ? No Flight Log
							// 1 Male (3) Male (3) Male (3) ? No Flight Log
							// 1 Kids (2) Kids (2) Kids (2) ? No Flight Log
							// 1 Female (3) Female (3) Female (3) ? No Flight Log
							// 1 Female (2) Female (2) Female (2) (ROXEBY ? No Flight Log
							// 1 Female (1) Female (1) Female (1) (MARHAM ? No Flight Log
							// 1 Baby Baby Baby ? No Flight Log
						}
						else
						{
							// continue with the ~4819 good records...
							string namish = reRidInitialsBoolSrc.Replace(right, "").Trim();
							// they all should have a comma between similar name fields.
							// E.g., "Sarah Kellen Kellen, Sarah Sarah Kellen"
							// grab the easiest mostest first: 2spcs, commaspace, 2spcs
							
							// But first, fix any hyphen space to just hyphen,
							// allow hyphens, and allow QM in place of a name.
							string namish2 = namish.Replace("- ", "-");
							// Three more fixes
							string namish3 = namish2.Replace("Marcinkov a", "Marcinkova").Replace("Cazaudum ec", "Cazaudumec").Replace("Passenger s", "Passengers");
							Match me = reEasyNamish.Match(namish3);
							if(me.Success == false)
							{
								// Console.WriteLine(namish2);
								// I will let these 8 more records fall out with the bathwater:
								//Rhonda Sherer's Sherer's, Rhonda Rhonda Sherer's Husband
								//MR. BROWN BROWN, MR. MR. BROWN
								//E JUTHLE? JUTHLE?, E E JUTHLE?
								//Mglindallns E? E?, Mglindallns Mglindallns E?
								//Geor Tintay? Tintay?, Geor Geor Tintay?
								//CZI FRIK? FRIK?, CZI CZI FRIK?
								//Mr. Mucinska Mucinska, Mr. Mr. Mucinska
								//Mrs. Mucinska Mucinska, Mrs. Mrs. Mucinska
							}
							else
							{
								// I am ready to extract all easy first and last names!
								// Anal.anal check equalities:
								string fn1 = me.Groups["fn1"].Value;
								string fn2 = me.Groups["fn2"].Value;
								string fn3 = me.Groups["fn3"].Value;
								string ln1 = me.Groups["ln1"].Value;
								string ln2 = me.Groups["ln2"].Value;
								string ln3 = me.Groups["ln3"].Value;
								if(fn1 != fn2
								   || fn2 != fn3
								   || ln1 != ln2
								   || ln2 != ln3)
								{
									// Console.WriteLine(namish2);
									// I now have 3 out of 4 cases, which I will fix above:
									// 54 Nadia Marcinkov a Marcinkova, Nadia Nadia Marcinkova
									// 32 Didier Cazaudum ec Cazaudumec, Didier Didier Cazaudumec
									// 8 No Passenger s Passengers, No No Passengers
									// 1 MORGAN RRY ? ?, MORGAN RRY MORGAN RRY ?
								}
								else
								{
									// tabulate "FirstName LastName" counts:
									
									nGoodNames++;
									
									string firstLast = fn1 + " " + ln1;
									if(NameQty.ContainsKey(firstLast))
										NameQty[firstLast]++;
									else
										NameQty.Add(firstLast, 1);
								}
							}
						}
					}
				}
			}
		
			// output the counted names ahead of any further reduction/analysis:			

			List<string>pq = new List<string>();
			foreach(KeyValuePair<string,int>kvp in NameQty)
			{
				pq.Add(kvp.Value.ToString().PadLeft(4) + " " + kvp.Key);
			}
			pq.Sort();
			pq.Reverse();
			File.WriteAllLines(outputFilePath, pq);
			Console.WriteLine("pq.Count = " + pq.Count);

			Console.WriteLine("nGoodNames = " + nGoodNames);

		}
	}
}