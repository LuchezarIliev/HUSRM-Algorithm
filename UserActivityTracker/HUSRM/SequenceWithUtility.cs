using System;
using System.Collections.Generic;
using System.Text;

namespace HUSRM
{
	public class SequenceWithUtility
	{
		private readonly IList<IList<int>> itemsets = new List<IList<int>>();
		private readonly IList<IList<double>> profits = new List<IList<double>>();
		private int id;
		public double exactUtility;
		public virtual IList<IList<double>> Utilities
		{
			get
			{
				return profits;
			}
		}
		public SequenceWithUtility(int id)
		{
			this.id = id;
		}
		public virtual void addItemset(IList<int> itemset)
		{
			itemsets.Add(itemset);
		}
		public virtual void addItemsetProfit(IList<double> utilityValues)
		{
			profits.Add(utilityValues);

		}
		public virtual void print()
		{
			Console.Write(ToString());
		}
		public override string ToString()
		{
			StringBuilder r = new StringBuilder("");
			for (int i = 0; i < itemsets.Count; i++)
			{
				IList<int> itemset = itemsets[i];
				r.Append('(');
				for (int j = 0; j < itemset.Count;j++)
				{
					 int item = itemset[j];
					r.Append(item);
					r.Append("[");
					r.Append(profits[i][j]);
					r.Append("]");
					r.Append(' ');
				}
				r.Append(')');
			}
			r.Append("   sequenceUtility: " + exactUtility);
			return r.Append("    ").ToString();
		}
		public virtual int Id
		{
			get
			{
				return id;
			}
		}
		public virtual IList<IList<int>> Itemsets
		{
			get
			{
				return itemsets;
			}
		}
		public virtual IList<int> get(int index)
		{
			return itemsets[index];
		}
		public virtual int size()
		{
			return itemsets.Count;
		}
	}
}