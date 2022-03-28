using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HUSRM
{
	public class SequenceDatabaseWithUtility
	{
		private IList<SequenceWithUtility> sequences = new List<SequenceWithUtility>();
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void loadFile(String path, int maximumNumberOfSequences) throws java.io.IOException
		public virtual void loadFile(string path, int maximumNumberOfSequences)
		{
			string thisLine;
			StreamReader myInput = null;
			try
			{
				FileStream fin = new FileStream(path, FileMode.Open, FileAccess.Read);
				myInput = new StreamReader(fin);
				int i = 0;
				while (!string.ReferenceEquals((thisLine = myInput.ReadLine()), null))
				{
					if (thisLine.Length > 0 && thisLine[0] != '#' && thisLine[0] != '%' && thisLine[0] != '@')
					{
						string[] split = thisLine.Split(" ", true);
						addSequence(split);
						i++;
						if (i == maximumNumberOfSequences)
						{
							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
			}
			finally
			{
				if (myInput != null)
				{
					myInput.Close();
				}
			}
		}
		internal virtual void addSequence(string[] tokens)
		{
			ISet<int> alreadySeenItems = new HashSet<int>();
			int profitExtraItemOccurrences = 0;
			SequenceWithUtility sequence = new SequenceWithUtility(sequences.Count);
			IList<int> itemset = new List<int>();
			IList<double> itemsetProfit = new List<double>();
			foreach (string token in tokens)
			{
				if (token.Length == 0)
				{
					continue;
				}
				if (token[0] == 'S')
				{
					string[] strings = token.Split(":", true);
					string exactUtility = strings[1];
					sequence.exactUtility = double.Parse(exactUtility) - profitExtraItemOccurrences;
				}
				else if (token.Equals("-1"))
				{
					sequence.addItemset(itemset);
					sequence.addItemsetProfit(itemsetProfit);
					itemset = new List<int>();
					itemsetProfit = new List<double>();

				}
				else if (token.Equals("-2"))
				{
					sequences.Add(sequence);
				}
				else
				{
					string[] strings = token.Split("\\[", true);
					string item = strings[0];
					int itemInt = int.Parse(item);
					if (alreadySeenItems.Contains(itemInt) == false)
					{
						string profit = strings[1];
						string profitWithoutBrackets = profit.Substring(0, profit.Length - 1);
						itemset.Add(itemInt);
						itemsetProfit.Add(double.Parse(profitWithoutBrackets));
						alreadySeenItems.Add(itemInt);
					}
					else
					{
						string profit = strings[1];
						string profitWithoutBrackets = profit.Substring(0, profit.Length - 1);
						profitExtraItemOccurrences += (int)double.Parse(profitWithoutBrackets);
					}
				}
			}
		}
		public virtual void addSequence(SequenceWithUtility sequence)
		{
			sequences.Add(sequence);
		}
		public virtual void print()
		{
			Console.WriteLine("============  SEQUENCE DATABASE ==========");
			foreach (SequenceWithUtility sequence in sequences)
			{
				Console.Write(sequence.Id + ":  ");
				sequence.print();
				Console.WriteLine("");
			}
		}
		public virtual void printDatabaseStats()
		{
			Console.WriteLine("============  STATS ==========");
			Console.WriteLine("Number of sequences : " + sequences.Count);
			long size = 0;
			foreach (SequenceWithUtility sequence in sequences)
			{
				size += sequence.size();
			}
			double meansize = ((float)size) / ((float)sequences.Count);
			Console.WriteLine("mean size" + meansize);
		}
		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			foreach (SequenceWithUtility sequence in sequences)
			{
				r.Append(sequence.Id);
				r.Append(":  ");
				r.Append(sequence.ToString());
				r.Append('\n');
			}
			return r.ToString();
		}
		public virtual int size()
		{
			return sequences.Count;
		}
		public virtual IList<SequenceWithUtility> Sequences
		{
			get
			{
				return sequences;
			}
		}
		public virtual ISet<int> SequenceIDs
		{
			get
			{
				ISet<int> set = new HashSet<int>();
				foreach (SequenceWithUtility sequence in Sequences)
				{
					set.Add(sequence.Id);
				}
				return set;
			}
		}
	}
}