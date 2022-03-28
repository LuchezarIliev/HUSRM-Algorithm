using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HUSRM
{
    /* This file is copyright (c) 2008-2013 Philippe Fournier-Viger
    * 
    * This file is part of the SPMF DATA MINING SOFTWARE
    * (http://www.philippe-fournier-viger.com/spmf).
    * 
    * SPMF is free software: you can redistribute it and/or modify it under the
    * terms of the GNU General Public License as published by the Free Software
    * Foundation, either version 3 of the License, or (at your option) any later
    * version.
    * 
    * SPMF is distributed in the hope that it will be useful, but WITHOUT ANY
    * WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR
    * A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    * You should have received a copy of the GNU General Public License along with
    * SPMF. If not, see <http://www.gnu.org/licenses/>.
    */

    //using MemoryLogger = ca.pfv.spmf.tools.MemoryLogger;

    public class MemoryLogger
    {

        // the only instance  of this class (this is the "singleton" design pattern)
        private static MemoryLogger instance = new MemoryLogger();

        // variable to store the maximum memory usage
        private double maxMemory = 0;

        /**
         * Method to obtain the only instance of this class
         * @return instance of MemoryLogger
         */
        public static MemoryLogger getInstance()
        {
            return instance;
        }

        /**
         * To get the maximum amount of memory used until now
         * @return a double value indicating memory as megabytes
         */
        public double getMaxMemory()
        {
            return maxMemory;
        }

        /**
         * Reset the maximum amount of memory recorded.
         */
        public void reset()
        {
            maxMemory = 0;
        }

        /**
         * Check the current memory usage and record it if it is higher
         * than the amount of memory previously recorded.
         */
        public void checkMemory()
        {
            //double currentMemory = (Runtime.getRuntime().totalMemory() - Runtime.getRuntime().freeMemory()) / 1024d / 1024d;
            double currentMemory = GC.GetTotalMemory(true) / 1024d / 1024d;
            if (currentMemory > maxMemory)
            {
                maxMemory = currentMemory;
            }
        }
    }

    /// <summary>
    /// This is the implementation of the HUSRM algorithm that we have submited at MLDM 2015.
    /// <br/><br/>
    /// Zida, S., Fournier-Viger, P., Wu, C.-W., Lin, J. C. W., Tseng, V.S., (2015). Efficient 
    /// Mining of High Utility Sequential Rules. Proc. 11th International Conference on Machine
    ///  Learning and Data Mining (MLDM 2015). Springer, LNAI, 15 pages (to appear).
    /// <br/>
    /// </summary>
    /// <seealso cref= SequenceWithUtility </seealso>
    /// <seealso cref= SequenceDatabaseWithUtility
    /// @author Souleymane Zida and Philippe Fournier-Viger, 2015 </seealso>
    public class AlgoHUSRM
	{
		// for statistics //
		/// <summary>
		/// start time of latest execution </summary>
		internal long timeStart = 0;
		/// <summary>
		/// end time of latest execution </summary>
		internal long timeEnd = 0;
		/// <summary>
		///  number of rules generated </summary>
		internal int ruleCount;

		// parameters ***/
		/// <summary>
		/// minimum confidence * </summary>
		internal double minConfidence;

		/// <summary>
		/// minimum support </summary>
		internal double minutil;

		/// <summary>
		/// this is the sequence database </summary>
		internal SequenceDatabaseWithUtility database;

		/// <summary>
		/// this buffered writer is used to write the output file </summary>
		internal StreamWriter writer = null;

		/// <summary>
		/// this is a map where the KEY is an item and the VALUE is the list of sequences
		/// /* containing the item. 
		/// </summary>
		private IDictionary<int, ListSequenceIDs> mapItemSequences;

		/// <summary>
		/// this variable is used to activate the debug mode.  When this mode is activated
		/// /* some additional information about the algorithm will be shown in the console for
		/// /* debugging *
		/// </summary>
		internal readonly bool DEBUG = false;

		/// <summary>
		/// this is a contrainst on the maximum number of item that the left side of a rule should
		/// /* contain 
		/// </summary>
		private int maxSizeAntecedent;

		/// <summary>
		/// this is a contrainst on the maximum number of item that the right side of a rule should
		/// /* contain 
		/// </summary>
		private int maxSizeConsequent;

		////// ================ STRATEGIES ===============================
		// Various strategies have been used to improve the performance of HUSRM.
		// The following boolean values are used to deactivate these strategies.

		/// <summary>
		/// Strategy 1: remove items with a sequence estimated utility < minutil </summary>
		private bool deactivateStrategy1 = false;

		/// <summary>
		/// Strategy 2: remove rules contains two items a--> b with a sequence estimated utility < minutil </summary>
		private bool deactivateStrategy2 = false;

		/// <summary>
		/// Strategy 3 use bitvectors instead of array list for quickly calculating the support of
		/// /*  rule antecedent 
		/// </summary>
		private bool deactivateStrategy3 = false;

		/// <summary>
		/// Strategy 4 :  utilize the sum of the utility of lutil, lrutil and rutil
		/// /* If deactivated, we use the same utility tables, but the upper bound will be calculated as
		/// /*  lutil + lrutil + rutil instead of the better upper bounds described in the paper 
		/// </summary>
		private bool deactivateStrategy4 = false;



		/// <summary>
		/// Default constructor
		/// </summary>
		public AlgoHUSRM()
		{
		}

		/// <summary>
		/// This is a structure to store some estimated utility and a list of sequence ids.
		/// It will be use in the code for storing the estimated utility of a rule and the list
		/// of sequence ids where the rule appears.
		/// </summary>
		public class EstimatedUtilityAndSequences
		{
			private readonly AlgoHUSRM outerInstance;

			public EstimatedUtilityAndSequences(AlgoHUSRM outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			// an estimated profit value
			internal double? utility = 0d;
			// a list of sequence ids
			internal IList<int> sequenceIds = new List<int>();
		}



		/// <summary>
		/// The main method to run the algorithm
		/// </summary>
		/// <param name="input"> an input file </param>
		/// <param name="output"> an output file </param>
		/// <param name="minConfidence"> the minimum confidence threshold </param>
		/// <param name="minutil"> the minimum utility threshold </param>
		/// <param name="maxConsequentSize"> a constraint on the maximum number of items that the right side of a rule should contain </param>
		/// <param name="maxAntecedentSize"> a constraint on the maximum number of items that the left side of a rule should contain </param>
		/// <param name="maximumNumberOfSequences"> the maximum number of sequences to be used </param>
		/// <exception cref="IOException"> if error reading/writing files </exception>
		//@SuppressWarnings("unused")
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void runAlgorithm(String input, String output, double minConfidence, double minutil, int maxAntecedentSize, int maxConsequentSize, int maximumNumberOfSequences) throws java.io.IOException
		public virtual void runAlgorithm(string input, string output, double minConfidence, double minutil, int maxAntecedentSize, int maxConsequentSize, int maximumNumberOfSequences)
		{

			// save the minimum confidence parameter
			this.minConfidence = minConfidence;

			// save the constraints on the maximum size of left/right side of the rules
			this.maxSizeAntecedent = maxAntecedentSize;
			this.maxSizeConsequent = maxConsequentSize;

			// reinitialize the number of rules found
			ruleCount = 0;
			this.minutil = minutil;

			// if the database was not loaded, then load it.
			if (database == null)
			{
				try
				{
					database = new SequenceDatabaseWithUtility();
					database.loadFile(input, maximumNumberOfSequences);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
				}
			}

			// if in debug mode, we print the database to the console
			if (DEBUG)
			{
				database.print();
			}

			// We reset the tool for calculating the maximum memory usage
			//MemoryLogger.Instance.reset();
			MemoryLogger.getInstance().reset();

			// we prepare the object for writing the output file
			writer = new StreamWriter(output);

			// if minutil is 0, set it to 1 to avoid generating
			// all rules 
			this.minutil = minutil;
			if (this.minutil == 0)
			{
				this.minutil = 0.001;
			}

			// save the start time
			timeStart = DateTimeHelper.CurrentUnixTimeMillis(); // for stats

			// FIRST STEP: We will calculate the estimated profit of each single item

			// if this strategy has not been deactivated
			if (deactivateStrategy1 == false)
			{
				// This map will store pairs of (key: item   value: estimated profit of the item)
				IDictionary<int, double> mapItemEstimatedUtility = new Dictionary<int, double>();

				// We read the database.
				// For each sequence 
				foreach (SequenceWithUtility sequence in database.Sequences)
				{

					// for each itemset in that sequence
					foreach (IList<int> itemset in sequence.Itemsets)
					{

						// for each item
						foreach (int item in itemset)
						{

							// get the current sequence estimated utility of that item
							//double? estimatedUtility = mapItemEstimatedUtility[item];
							double? estimatedUtility = mapItemEstimatedUtility.ContainsKey(item) ? (double?)mapItemEstimatedUtility[item] : null;

							// if we did not see that item yet
							if (estimatedUtility == null)
							{
								// then its estimated utility of that item until now is the 
								// utility of that sequence
								estimatedUtility = sequence.exactUtility;

							}
							else
							{
								// otherwise, it is not the first time that we saw that item
								// so we add the utility of that sequence to the sequence
								// estimated utility f that item
								estimatedUtility = estimatedUtility.Value + sequence.exactUtility;
							}

							// update the estimated utility of that item in the map
							mapItemEstimatedUtility[item] = estimatedUtility.Value;

						}
					}
				}

				// if we are in debug mode, we will print the calculated estimated utility
				// of all items to the console for easy debugging
				if (DEBUG)
				{
					Console.WriteLine("==============================================================================");
					Console.WriteLine("--------------------ESTIMATED UTILITY OF ITEMS -----------------------------------");
					Console.WriteLine("==============================================================================");
					Console.WriteLine(" ");

					// for each entry in the map
					foreach (KeyValuePair<int, double> entreeMap in mapItemEstimatedUtility.SetOfKeyValuePairs())
					{
						// we print the item and its estimated utility
						Console.WriteLine("item : " + entreeMap.Key + " profit estime: " + entreeMap.Value);
					}




					// NEXT STEP: WE WILL REMOVE THE UNPROMISING ITEMS

					Console.WriteLine("==============================================================================");
					Console.WriteLine("-------------------ESTIMATED UTILITY OF PROMISING ITEMS      ----------------");
					Console.WriteLine("==============================================================================");
				}


//				// we create an iterator to loop over all items
//				IEnumerator<KeyValuePair<int, double>> iterator = mapItemEstimatedUtility.SetOfKeyValuePairs().GetEnumerator();
//				// for each item
//				while (iterator.MoveNext())
//				{

//					// we obtain the entry in the map
//					KeyValuePair<int, double> entryMapItemEstimatedUtility = (KeyValuePair<int, double>) iterator.Current;
//					double? estimatedUtility = entryMapItemEstimatedUtility.Value;

//					// if the estimated utility of the current item is less than minutil
//					if (estimatedUtility.Value < minutil)
//					{

//						// we remove the item from the map
////JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
//						iterator.remove();
//					}
//				}

                foreach (KeyValuePair<int, double> item in mapItemEstimatedUtility.SetOfKeyValuePairs())
                {
                    if (item.Value < minutil)
                        mapItemEstimatedUtility.SetOfKeyValuePairs().Remove(item);
                }


				// if the debug mode is activated
				if (DEBUG)
				{
					// we will print all the promising items

					// we loop over the entries of the map
					foreach (KeyValuePair<int, double> entreeMap in mapItemEstimatedUtility.SetOfKeyValuePairs())
					{
						// we print the item and its estimated utility
						Console.WriteLine("item : " + entreeMap.Key + " profit estime: " + entreeMap.Value);
					}

					Console.WriteLine("==============================================================================");
					Console.WriteLine("-------------- DATABASE WITH ONLY ITEMS HAVING ESTIMATED UTILITY >= miinutil-------------");
					Console.WriteLine("==============================================================================");

				}

                // NEXT STEP: WE REMOVE UNPROMISING ITEMS FROM THE SEQUENCES
                // (PREVIOUSLY WE HAD ONLY REMOVED THEM FROM THE MAP).


                //// So we scan the database again.
                //// For each sequence
                //IEnumerator<SequenceWithUtility> iteratorSequence = database.Sequences.GetEnumerator();
                //while (iteratorSequence.MoveNext())
                //{
                //    SequenceWithUtility sequence = iteratorSequence.Current;

                //    //For each itemset
                //    IEnumerator<IList<int>> iteratorItemset = sequence.Itemsets.GetEnumerator();
                //    IEnumerator<IList<double>> iteratorItemsetUtilities = sequence.Utilities.GetEnumerator();
                //    while (iteratorItemset.MoveNext())
                //    {
                //        // the items in that itemset
                //        IList<int> itemset = iteratorItemset.Current;
                //        // the utility values in that itemset
                //        //JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                //        IList<double> itemsetUtilities = iteratorItemsetUtilities.next();

                //        // Create an iterator over each item in that itemset
                //        IEnumerator<int> iteratorItem = itemset.GetEnumerator();
                //        // Create an iterator over utility values in that itemset
                //        IEnumerator<double> iteratorItemUtility = itemsetUtilities.GetEnumerator();

                //        // For each item
                //        while (iteratorItem.MoveNext())
                //        {
                //            // get the item 
                //            int item = iteratorItem.Current;
                //            // get its utility value
                //            //JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                //            double? utility = iteratorItemUtility.next();

                //            // if the item is unpromising
                //            if (mapItemEstimatedUtility[item] == null)
                //            {

                //                // remove the item
                //                //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                //                iteratorItem.remove();
                //                // remove its utility value
                //                //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                //                iteratorItemUtility.remove();
                //                // subtract the item utility value from the sequence utility.
                //                sequence.exactUtility -= utility.Value;
                //            }
                //        }

                //        // If the itemset has become empty, we remove it from the sequence
                //        if (itemset.Count == 0)
                //        {
                //            //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                //            iteratorItemset.remove();
                //            //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                //            iteratorItemsetUtilities.remove();
                //        }
                //    }

                //    // If the sequence has become empty, we remove the sequences from the database
                //    if (sequence.size() == 0)
                //    {
                //        //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                //        iteratorSequence.remove();
                //    }
                //}

                // So we scan the database again.
                // For each sequence
                IEnumerator<SequenceWithUtility> iteratorSequence = database.Sequences.GetEnumerator();
                while (iteratorSequence.MoveNext())
                {
                    SequenceWithUtility sequence = iteratorSequence.Current;

                    //For each itemset
                    IEnumerator<IList<int>> iteratorItemset = sequence.Itemsets.GetEnumerator();
                    IEnumerator<IList<double>> iteratorItemsetUtilities = sequence.Utilities.GetEnumerator();
                    while (iteratorItemset.MoveNext())
                    {
                        // the items in that itemset
                        IList<int> itemset = iteratorItemset.Current;
                        // the utility values in that itemset
                        //JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                        iteratorItemsetUtilities.MoveNext();
                        IList<double> itemsetUtilities = iteratorItemsetUtilities.Current;

                        // Create an iterator over each item in that itemset
                        IEnumerator<int> iteratorItem = itemset.GetEnumerator();
                        // Create an iterator over utility values in that itemset
                        IEnumerator<double> iteratorItemUtility = itemsetUtilities.GetEnumerator();

                        // For each item
                        while (iteratorItem.MoveNext())
                        {
                            // get the item 
                            int item = iteratorItem.Current;
                            // get its utility value
                            //JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
                            iteratorItemUtility.MoveNext();
                            double? utility = iteratorItemUtility.Current;

                            // if the item is unpromising
                            if (mapItemEstimatedUtility[item] == null)
                            {
                                // remove the item
                                itemset.Remove(item);
                                iteratorItem = itemset.GetEnumerator();
                                // remove its utility value
                                itemsetUtilities.Remove(utility.Value);
                                iteratorItemUtility = itemsetUtilities.GetEnumerator();
                                // subtract the item utility value from the sequence utility.
                                sequence.exactUtility -= utility.Value;
                            }
                        }

                        // If the itemset has become empty, we remove it from the sequence
                        if (itemset.Count == 0)
                        {
                            sequence.Itemsets.Remove(itemset);
                            iteratorItemset = sequence.Itemsets.GetEnumerator();

                            sequence.Utilities.Remove(itemsetUtilities);
                            iteratorItemsetUtilities = sequence.Utilities.GetEnumerator();
                        }
                    }

                    // If the sequence has become empty, we remove the sequences from the database
                    if (sequence.size() == 0)
                    {
                        database.Sequences.Remove(sequence);
                    }
                }

                //foreach (var sequence in database.Sequences)
                //{
                //    IEnumerator<IList<double>> iteratorItemsetUtilities = sequence.Utilities.GetEnumerator();
                //    foreach (var itemset in sequence.Itemsets)
                //    {
                //        iteratorItemsetUtilities.MoveNext();
                //        IList<double> itemsetUtilities = iteratorItemsetUtilities.Current;
                //        IEnumerator<double> iteratorItemUtility = itemsetUtilities.GetEnumerator();

                //        foreach (var item in itemset)
                //        {
                //            iteratorItemUtility.MoveNext();
                //            double? utility = iteratorItemUtility.Current;

                //            // if the item is unpromising
                //            if (mapItemEstimatedUtility[item] == null)
                //            {

                //                // remove the item
                //                itemset.Remove(item);
                //                // remove its utility value
                //                itemsetUtilities.Remove(utility.Value);
                //                // subtract the item utility value from the sequence utility.
                //                sequence.exactUtility -= utility.Value;
                //            }
                //        }

                //        // If the itemset has become empty, we remove it from the sequence
                //        if (itemset.Count == 0)
                //        {
                //            sequence.Itemsets.Remove(itemset);
                //            sequence.Utilities.Remove(itemsetUtilities);
                //        }
                //    }

                //    // If the sequence has become empty, we remove the sequences from the database
                //    if (sequence.size() == 0)
                //    {
                //        database.Sequences.Remove(sequence);
                //    }
                //}
			}

			// if we are in debug mode
			if (DEBUG)
			{
				// print the database without the unpromising items
				database.print();

				Console.WriteLine("==============================================================================");
				Console.WriteLine("----- FOR EACH ITEM, REMEMBER THE IDS OF SEQUENCES CONTAINING THE ITEM  -------");
				Console.WriteLine("==============================================================================");

			}

			// We create a map to store for each item, the list of sequences containing the item
			// Key: an item   Value:  the list of sequences containing the item
			mapItemSequences = new Dictionary<int, ListSequenceIDs>();

			// For each sequence
			for (int i = 0; i < database.Sequences.Count; i++)
			{
				SequenceWithUtility sequence = database.Sequences[i];

				// For each itemset
				foreach (IList<int> itemset in sequence.Itemsets)
				{

					// For each item
					foreach (int item in itemset)
					{
						// Get the list of identifiers of sequence containing that item
						//ListSequenceIDs numerosSequenceItem = mapItemSequences[item];
						ListSequenceIDs numerosSequenceItem = mapItemSequences.ContainsKey(item) ? mapItemSequences[item] : null;

						// If the list does not exist, we will create it
						if (numerosSequenceItem == null)
						{
							// if the user desactivated strategy 3, we will use an arraylist implementation
							if (deactivateStrategy3)
							{
								numerosSequenceItem = new ListSequenceIDsArrayList(this);
							}
							else
							{
								// otherwise we use a bitvector implementation, which is more efficient
								numerosSequenceItem = new ListSequenceIDsBitVector(this);
							}
							// we add the list in the map for that item
							mapItemSequences[item] = numerosSequenceItem;
						}
						// finally we add the current sequence ids to the list of sequences ids of the current
						// item
						numerosSequenceItem.addSequenceID(i);
					}
				}
			}

			// if we are in debug mode
			if (DEBUG)
			{
				// We will print the map which will show the list of sequence identifiers
				// for each item.
				foreach (KeyValuePair<int, ListSequenceIDs> entree in mapItemSequences.SetOfKeyValuePairs())
				{
					Console.WriteLine("Item : " + entree.Key + " Sequences : " + entree.Value);
				}

				Console.WriteLine("==============================================================================");
				Console.WriteLine("----- CALCULATE SEQUENCE ESTIMATED UTILITY OF EACH RULE OF SIZE 2 -------------");
				Console.WriteLine("==============================================================================");
			}

			// We create a map of map to store the estimated utility and list of sequences ids for
			// each rule of two items (e.g. a -> b  ).
			// The key of the first map: the item "a" in the left side of the rule
			// The key of the second map:  the item "b" in the right side of the rule
			// The value in the second map:  the estimated utility of the rule and sequence ids for that rule
			IDictionary<int, IDictionary<int, EstimatedUtilityAndSequences>> mapItemItemEstimatedUtility = new Dictionary<int, IDictionary<int, EstimatedUtilityAndSequences>>();

			// For each sequence
			for (int z = 0; z < database.Sequences.Count; z++)
			{
				SequenceWithUtility sequence = database.Sequences[z];

				// For each itemset I 
				for (int i = 0; i < sequence.Itemsets.Count; i++)
				{

					// get the itemset
					IList<int> itemset = sequence.Itemsets[i];

					// For each item  X 
					for (int j = 0; j < itemset.Count; j++)
					{
						int itemX = itemset[j];
						// SI X N'A PAS DEJA ETE VU

						// For each item Y occuring after X,
						// that is in the itemsets I+1, I+2 .... 
						for (int k = i + 1; k < sequence.Itemsets.Count; k++)
						{
							//  for a given itemset K
							IList<int> itemsetK = sequence.Itemsets[k];
							// for an item Y
							foreach (int itemY in itemsetK)
							{

								// We will update the estimated profit of the rule X --> Y 
								// by adding the sequence utility of that sequence to the 
								// sequence estimated utility of that rule

								// Get the map for item X
								//IDictionary<int, EstimatedUtilityAndSequences> mapXItemUtility = mapItemItemEstimatedUtility[itemX];
								IDictionary<int, EstimatedUtilityAndSequences> mapXItemUtility = mapItemItemEstimatedUtility.ContainsKey(itemX) ? mapItemItemEstimatedUtility[itemX] : null;

								// If we never saw X before
								if (mapXItemUtility == null)
								{
									// we create a map for X
									mapXItemUtility = new Dictionary<int, EstimatedUtilityAndSequences>();
									mapItemItemEstimatedUtility[itemX] = mapXItemUtility;

									// Then we create a structure for storing the estimated utility of X ->Y
									EstimatedUtilityAndSequences structure = new EstimatedUtilityAndSequences(this);
									structure.utility = sequence.exactUtility; // the current sequence utility
									structure.sequenceIds.Add(z); // the sequence id
									// add it in the map for X -> Y
									mapXItemUtility[itemY] = structure;
								}
								else
								{
									// in the case were we saw X before.
									// We get its structure for storing the estimated utility of X -> Y
									//EstimatedUtilityAndSequences structure = mapXItemUtility[itemY];
									EstimatedUtilityAndSequences structure = mapXItemUtility.ContainsKey(itemY) ? mapXItemUtility[itemY] : null;
									// if we never saw X ->Y
									if (structure == null)
									{

										// Then we create a structure for storing the estimated utility of X ->Y
										 structure = new EstimatedUtilityAndSequences(this);
										structure.utility = sequence.exactUtility; // the current sequence utility
										structure.sequenceIds.Add(z); // the sequence id

										// add it in the map for X -> Y
										mapXItemUtility[itemY] = structure;
									}
									else
									{
										// if we saw X -> Y before
										// We add the sequence utility to the utility of that rule
										structure.utility = structure.utility.Value + sequence.exactUtility;
										// We add the sequence ids to the list of sequence ids of that rule.
										structure.sequenceIds.Add(z);
									}
								}

							}
						}
					}
				}
			}

			// if in debuging mode
			if (DEBUG)
			{
				// we will print the estimated utility and list of sequences ids of all rules containing two items
				// e.g.   "a" -> "b"

				// for each item X
				foreach (KeyValuePair<int, IDictionary<int, EstimatedUtilityAndSequences>> entreeX in mapItemItemEstimatedUtility.SetOfKeyValuePairs())
				{
					int itemX = entreeX.Key;

					// for each item Y 
					//foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entreeYProfit in entreeX.Value.entrySet())
					foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entreeYProfit in entreeX.Value)
					{
						int itemY = entreeYProfit.Key;
						EstimatedUtilityAndSequences structureXY = entreeYProfit.Value;

						// Print the rule X ->Y  with its estimated utility and list of sequence ids.
						Console.WriteLine("  RULE: " + itemX + " --> " + itemY + "   estimated utility " + structureXY.utility.Value + "   sequences " + structureXY.sequenceIds);
					}
				}


				Console.WriteLine("==============================================================================");
				Console.WriteLine("-------------- RULES OF SIZE 2 WITH ESTIMATED UTILITY >= minutil -------------");
				Console.WriteLine("==============================================================================");
			}

			// For each entry in the map
			foreach (KeyValuePair<int, IDictionary<int, EstimatedUtilityAndSequences>> mapI in mapItemItemEstimatedUtility.SetOfKeyValuePairs())
			{

                // An entry represents an item "i" (the key) and some maps (value)
                // We will loop over the entries of the secondary map of "i" (value)
                // to remove all rules of the form i -> j where the estimated utility
                // is lower than minutil

                //Create an iterator
                //IEnumerator<KeyValuePair<int, EstimatedUtilityAndSequences>> iterEntry = mapI.Value.entrySet().GetEnumerator();
                IEnumerator<KeyValuePair<int, EstimatedUtilityAndSequences>> iterEntry = mapI.Value.GetEnumerator();

                // loop over the map
                while (iterEntry.MoveNext())
                {
                    // We consider each entry j and the estimated utility of i-> j
                    KeyValuePair<int, EstimatedUtilityAndSequences> entry = (KeyValuePair<int, EstimatedUtilityAndSequences>)iterEntry.Current;
                    // if the estimated profit of i -> j is lower than minutil
                    // we remove that rule because no larger rule containing that rule
                    // can have a estimated utility higher or equal to minutil.
                    if (entry.Value.utility < minutil)
                    {
                        // we only do that if the user did not deactivate strategy 2
                        if (deactivateStrategy2 == false)
                        {
                            //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                            //iterEntry.remove();
                            mapI.Value.Remove(entry);
                            iterEntry = mapI.Value.GetEnumerator();
                        }
                    }

                }

                //foreach (KeyValuePair<int, EstimatedUtilityAndSequences> item in mapI.Value)
                //{
                //    if (item.Value.utility < minutil)
                //    {
                //        if (deactivateStrategy2 == false)
                //        {
                //            mapI.Value.Remove(item);
                //        }
                //    }
                //}
            }

			// If in debug mode
			if (DEBUG)
			{
				// We will print the remaining rules

				// we will print the estimated utility and list of sequences ids of all rules containing two items
				// e.g.   "a" -> "b"

				// for each item X
				foreach (KeyValuePair<int, IDictionary<int, EstimatedUtilityAndSequences>> entreeX in mapItemItemEstimatedUtility.SetOfKeyValuePairs())
				{
					int itemX = entreeX.Key;

					// for each item Y
					//foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entreeYProfit in entreeX.Value.entrySet())
					foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entreeYProfit in entreeX.Value)
					{
						int itemY = entreeYProfit.Key;

						// Print the rule X ->Y  with its estimated utility and list of sequence ids.
						EstimatedUtilityAndSequences structureXY = entreeYProfit.Value;
						Console.WriteLine("  REGLE: " + itemX + " --> " + itemY + "   profit estime " + structureXY.utility.Value + "   sequences " + structureXY.sequenceIds);
					}
				}

				Console.WriteLine("==============================================================================");
				Console.WriteLine("-------------- RULES OF SIZE 2 WITH UTILITY >= minutil -------------");
				Console.WriteLine("==============================================================================");
			}

		// For each rule X --> Y
		foreach (KeyValuePair<int, IDictionary<int, EstimatedUtilityAndSequences>> entryX in mapItemItemEstimatedUtility.SetOfKeyValuePairs())
		{
			// Get the item X
			int itemX = entryX.Key;

			// Get the list of sequence ids containing the item X
			ListSequenceIDs sequenceIDsX = mapItemSequences[itemX];
			// Get the support of item X
			double supportX = sequenceIDsX.Size;

			// For each Y
			//foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entryYUtility in entryX.Value.entrySet())
			foreach (KeyValuePair<int, EstimatedUtilityAndSequences> entryYUtility in entryX.Value)
			{
				int itemY = entryYUtility.Key;

				// Get the estimated utility and list of sequences ids for the rule X -> Y
				EstimatedUtilityAndSequences structure = entryYUtility.Value;
				IList<int> sequencesIDsXY = structure.sequenceIds;

				// Get the support of the rule X ->Y
				double supportXY = sequencesIDsXY.Count;

				// We create the utility table of rule X -�> Y
				UtilityTable table = new UtilityTable(this);

				// We will scan each sequence to fill the utility table 
				// and update the other variable to calculate the confidence of the rule.

				// for each sequence containing X -> Y
				foreach (int numeroSequence in sequencesIDsXY)
				{
					// Get the sequence
					SequenceWithUtility sequence = database.Sequences[numeroSequence];

					// Create a new element in the table
					ElementOfTable element = new ElementOfTable(this, numeroSequence);

					// we reset position alpha and beta
					int positionAlphaItem = -1;
					int positionBetaItem = -1;

					// (1) We will scan the sequence from left to right to find X
					// and stop at the first position ALPHA where X has been seen completely.
					// At the same time, we will add the utility of items in X.

					// For each itemset I 
		for (int i = 0; i < sequence.Itemsets.Count; i++)
		{
						// get the itemset I
						//IList<int> itemset = sequence.Itemsets[i];

						// For each item J
						for (int j = 0; j < sequence.Itemsets[i].Count; j++)
						{
							int? itemIJ = sequence.Itemsets[i][j];

							// if we found the item X
							if (itemX.Equals(itemIJ))
							{
								// we get its utility
								double utilityXPositionIJ = sequence.Utilities[i][j];
								// we add it to the exact utility in the current utility table element
								element.utility += utilityXPositionIJ;

								// Stop and remember that position
								element.positionAlphaItemset = i;
								// remember the position ALPHA (which in this case means where the item in 
								// the right side
								// of a rule was found)
								positionAlphaItem = j;

								// since we found j, we don't need to continue this loop since we assume
								// that an item do not occur more than once per sequence
								goto loop1Break;
							}
							else if (itemIJ > itemX)
							{
								// If the item is greater than the item X,
								// we add the profit of this item to the "lutil" value of its element.
								double profitXPositionIJ = sequence.Utilities[i][j];
								element.utilityLeft += profitXPositionIJ;
							}
						}
			loop1Continue:;
		}
		loop1Break:

					// If X does not appear, we don't do the following steps
					if (element.positionAlphaItemset == -1)
					{
						continue;
					}

					// (2) Now we will scan the sequence from right to left to find
					//  Y and stop if we find it. That position where we find it will be called beta.
					// At the same time as we scan the sequence, we will add the utility of items in Y

					// for each itemset starting from the last one until itemset alpha+1
		for (int i = sequence.Itemsets.Count - 1; i > element.positionAlphaItemset ; i--)
		{
						// get the current itemset
						//IList<int> itemset = sequence.Itemsets[i];

						// for each item J in that itemset
						for (int j = sequence.Itemsets[i].Count - 1; j >= 0; j--)
						{
							// get the item J
							int? itemIJ = sequence.Itemsets[i][j];

							// if that item is Y
							if (itemY.Equals(itemIJ))
							{
								// we add Y's profit to the exact utility of the current element
								double profitYPositionIJ = sequence.Utilities[i][j];
								element.utility += profitYPositionIJ;

								// we stop and remember that we stopped at the i-th itemset
								// we will call this position "beta".
								element.positionBetaItemset = i;
								positionBetaItem = j;

								goto loop2Break;
							}
							 else if (itemIJ > itemY)
							 {
								// If the item is greater than the item Y,
									// we add the profit of this item to the "rutil" value of its element.
								double profitXPositionIJ = sequence.Utilities[i][j];
								element.utilityRight += profitXPositionIJ;
							 }
						}
			loop2Continue:;
		}
		loop2Break:
					/// If Y does not appear, we don't do the following steps
					 if (element.positionBetaItemset == -1)
					 {
						 continue;
					 }

					 // (3) THIRD STEP:  WE WILL SCAN THE SEQUENCE BETWEEN THE ALPHA
					 // AND BETA POSITIONS WHERE WE HAVE STOPPED TO CALCUlATE THE "LRUTIL" VALUE
					 // FOR X ->Y in that SEQUENCE

					 // (A) WE SCAN THE ALPHA ITEMSET
						IList<int> itemsetAlpha = sequence.Itemsets[element.positionAlphaItemset];
						// FOR EACH ITEM J IN THE ALPHA ITEMSET
						for (int j = positionAlphaItem + 1; j < itemsetAlpha.Count; j++)
						{

							// we add the utility of the item to the "LUTIL" value of the current element.
							double profitPositionIJ = sequence.Utilities[element.positionAlphaItemset][j];
							element.utilityLeft += profitPositionIJ;
						}


					// (B) Scan the other itemsets after the alpha itemset but before the beta itemset
					for (int i = element.positionAlphaItemset + 1; i < element.positionBetaItemset; i++)
					{
							// get the itemset
							//IList<int> itemset = sequence.Itemsets[i];

							// For each item J
							for (int j = 0; j < sequence.Itemsets[i].Count; j++)
							{
								int? itemIJ = sequence.Itemsets[i][j];

								// if the item is greater than X and Y
								if (itemIJ > itemX && itemIJ > itemY)
								{
									// it means that this item could be used to extend the left or right side
									// of the rule
									// We add its utility to "LRUTIL"
									double utilityPositionIJ = sequence.Utilities[i][j];
									element.utilityLeftRight += utilityPositionIJ;
								}
								else if (itemIJ > itemX)
								{
									// if the item is only greater than X
									// We add its utility to "RUTIL"
									double utilityPositionIJ = sequence.Utilities[i][j];
									element.utilityLeft += utilityPositionIJ;
								}
								else if (itemIJ > itemY)
								{
									// if the item is only greater than Y
									// We add its utility to "RUTIL"
									double utilityPositionIJ = sequence.Utilities[i][j];
									element.utilityRight += utilityPositionIJ;
								}
							}
					}

					// (c) Scan item in the itemset BETA after the item beta (i.e. the item Y)
					IList<int> itemset = sequence.Itemsets[element.positionBetaItemset];

					// For each item J after the beta item (i.e. the item Y)
					for (int j = 0; j < positionBetaItem - 1; j++)
					{
						int? itemIJ = itemset[j];

						// if the item is greater than Y
						if (itemIJ > itemY)
						{
							// We add its utility to "RUTIL"
							double profitPositionIJ = sequence.Utilities[element.positionBetaItemset][j];
							element.utilityRight += profitPositionIJ;
						}
					}

					// Finally, we add the element of this sequence to the utility table of X->Y
					table.addElement(element);

				}

				// We calculate the confidence of X -> Y
				double confidence = (supportXY / supportX);

				double conditionExpandLeft;
				double conditionExpandRight;

				// if strategy 4 is deactivated
				// we use a worse upper bound
				if (deactivateStrategy4)
				{
					conditionExpandLeft = table.totalUtility + table.totalUtilityLeft + table.totalUtilityLeftRight + table.totalUtilityRight;
					 conditionExpandRight = conditionExpandLeft;
				}
				else
				{
					// otherwise we use a better upper bound
					conditionExpandLeft = table.totalUtility + table.totalUtilityLeft + table.totalUtilityLeftRight;
					 conditionExpandRight = table.totalUtility + table.totalUtilityRight + table.totalUtilityLeftRight + table.totalUtilityLeft;
				}


				// if in debug mode
				if (DEBUG)
				{
					//We will print the rule and its profit and whether it is a high utility rule or not
					string isInteresting = (table.totalUtility >= minutil) ? " *** HIGH UTILITY RULE! ***" : " ";
					Console.WriteLine("\n  RULE: " + itemX + " --> " + itemY + "   utility " + table.totalUtility + " frequence : " + supportXY + " confiance : " + confidence + isInteresting);

					// we will print the utility table of the rule
					foreach (ElementOfTable element in table.elements)
					{
						Console.WriteLine("      SEQ:" + element.numeroSequence + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft + " \t lrutil: " + element.utilityLeftRight + " \t rutil: " + element.utilityRight + " alpha : " + element.positionAlphaItemset + " beta : " + element.positionBetaItemset);
					}

					Console.WriteLine("      TOTAL: " + " \t utility: " + table.totalUtility + " \t lutil: " + table.totalUtilityLeft + " \t lrutil: " + table.totalUtilityLeftRight + " \t rutil: " + table.totalUtilityRight);
								Console.WriteLine("      Should we explore larger rules by left expansions ? " + (conditionExpandLeft >= minutil) + " (" + conditionExpandLeft + " )");
					Console.WriteLine("       Should we explore larger rules by right expansions ? " + (conditionExpandRight >= minutil) + " (" + conditionExpandRight + " )");
				}

				// create the rule antecedent and consequence
				int[] antecedent = new int[]{itemX};
				int[] consequent = new int[]{itemY};

				// if high utility with ENOUGH  confidence
				if ((table.totalUtility >= minutil) && confidence >= minConfidence)
				{
					// we output the rule
					saveRule(antecedent, consequent, table.totalUtility, supportXY, confidence);
				}

				// if the right side size is less than the maximum size, we will try to expand the rule
				if (conditionExpandRight >= minutil && maxConsequentSize > 1)
				{
					expandRight(table, antecedent, consequent, sequenceIDsX);
				}

				// if the left side size is less than the maximum size, we will try to expand the rule
				if (conditionExpandLeft >= minutil && maxAntecedentSize > 1)
				{
					expandFirstLeft(table, antecedent, consequent, sequenceIDsX);
				}
			}
		}


			//We will check the current memory usage
			//MemoryLogger.Instance.checkMemory();
			MemoryLogger.getInstance().checkMemory();

			// save end time
			timeEnd = DateTimeHelper.CurrentUnixTimeMillis();

			// close the file
			writer.Close();

			// after the algorithm ends, we don't need a reference to the database
			// anymore.
			database = null;
		}

		/// <summary>
		/// This method save a rule to the output file </summary>
		/// <param name="antecedent"> the left side of the rule </param>
		/// <param name="consequent"> the right side of the rule </param>
		/// <param name="utility"> the rule utility </param>
		/// <param name="support"> the rule support </param>
		/// <param name="confidence"> the rule confidence </param>
		/// <exception cref="IOException"> if an error occurs when writing to file </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private void saveRule(int[] antecedent, int[] consequent, double utility, double support, double confidence) throws java.io.IOException
		private void saveRule(int[] antecedent, int[] consequent, double utility, double support, double confidence)
		{

			// increase the number of rule found
			ruleCount++;

			// create a string buffer
			StringBuilder buffer = new StringBuilder();

			// write the left side of the rule (the antecedent)
			for (int i = 0; i < antecedent.Length; i++)
			{
				buffer.Append(antecedent[i]);
				if (i != antecedent.Length - 1)
				{
					buffer.Append(",");
				}
			}

			// write separator
			buffer.Append("	==> ");

			// write the right side of the rule (the consequent)
			for (int i = 0; i < consequent.Length; i++)
			{
				buffer.Append(consequent[i]);
				if (i != consequent.Length - 1)
				{
					buffer.Append(",");
				}
			}
			// write support
			buffer.Append("\t#SUP: ");
			buffer.Append(support);
			// write confidence
			buffer.Append("\t#CONF: ");
			buffer.Append(confidence);
			buffer.Append("\t#UTIL: ");
			buffer.Append(utility);
			writer.Write(buffer.ToString());
			writer.WriteLine();

			//if we are in debug mode, we will automatically check that the utility, confidence and support
			// are correct to ensure that there is no bug.
			if (DEBUG)
			{
				//We will check if the rule utility support and confidence is ok
				checkMeasuresForARule(antecedent,consequent, utility, support, confidence);

			}
		}

		/// <summary>
		/// This method is used for debugging. It scan a database to check if the measures
		/// (confidence, utility, support) of a given rule have been correctly calculated. </summary>
		/// <param name="antecedent"> the left isde </param>
		/// <param name="antecedent"> the left side of the rule </param>
		/// <param name="consequent"> the right side of the rule </param>
		/// <param name="utility"> the rule utility </param>
		/// <param name="support"> the rule support </param>
		/// <param name="confidence"> the rule confidence </param>
		private void checkMeasuresForARule(int[] antecedent, int[] consequent, double utility, double support, double confidence)
		{

			// We will calculate again the utility, support and confidence by 
			// scanning the database.
			double supportOfAntecedent = 0;
			double supportOfTheRule = 0;
			double utilityOfTheRule = 0;

			// for each sequence
			foreach (SequenceWithUtility sequence in database.Sequences)
			{

				// Count the number of items already seen from the antecedent in that sequence
				int numberOfAntecedentItemsAlreadySeen = 0;

				double ruleUtilityInSequence = 0;

				//=========================================
				// For each itemset in that sequence
				int i = 0;
	for (; i < sequence.Itemsets.Count; i++)
	{
					IList<int> itemset = sequence.Itemsets[i];

					// For each item
					for (int j = 0; j < itemset.Count; j++)
					{
						int? item = itemset[j];

						// if the item appear in the left side of a rule
						//if (Arrays.binarySearch(antecedent, item) >= 0)
						if (Array.BinarySearch(antecedent, item) >= 0)
						{
							// add the profit of that item to the rule utility
							double utilityItem = sequence.Utilities[i][j];
							ruleUtilityInSequence += utilityItem;

							// increase the number of items from the antecedent that we have seen 
							numberOfAntecedentItemsAlreadySeen++;

							// if we have completely found the antecedent X
							if (numberOfAntecedentItemsAlreadySeen == antecedent.Length)
							{
								// increase the support of the antecedent
								supportOfAntecedent++;
								// and stop searching for items in the antecedent
								goto loop1Break;
							}

						}
					}
		loop1Continue:;
	}
	loop1Break:

				//=========================================
				// Now we will search for the consequent of the rule
				// starting from the next itemset in that sequence
				i++;

				// This variable will count the number of items of the consequent
				// that we have already seen
				int numberOfConsequentItemsAlreadySeen = 0;


				// for each itemset after the antecedent
		for (; i < sequence.Itemsets.Count; i++)
		{
					IList<int> itemset = sequence.Itemsets[i];

					// for each item
					for (int j = 0; j < itemset.Count; j++)
					{
						int? item = itemset[j];

						// if the item appear in the consequent of the rule
						//if (Arrays.binarySearch(consequent, item) >= 0)
						if (Array.BinarySearch(consequent, item) >= 0)
						{
							// add the utility of that item
							double utilityItem = sequence.Utilities[i][j];
							ruleUtilityInSequence += utilityItem;

							// increase the number of items from the consequent that we have seen 
							numberOfConsequentItemsAlreadySeen++;

							// if we have completely found the consequent Y 
							if (numberOfConsequentItemsAlreadySeen == consequent.Length)
							{
								// increase the support of the rule
								supportOfTheRule++;
								// increase the global utility of the rule in the database
								utilityOfTheRule += ruleUtilityInSequence;
								// and stop searching for items in the antecedent
								goto boucle2Break;
							}

						}
					}
			boucle2Continue:;
		}
		boucle2Break:;
			}

			// We now check if the support is the same as the support calculated by HUSRM
			if (support != supportOfTheRule)
			{
				//throw new Exception(" The support is incorrect for the rule : " + Arrays.toString(antecedent) + " ==>" + Arrays.toString(consequent) + "   support : " + support + " recalculated support: " + supportOfTheRule);
				throw new Exception(" The support is incorrect for the rule : " + ArrayToString(antecedent) + " ==>" + ArrayToString(consequent) + "   support : " + support + " recalculated support: " + supportOfTheRule);
			}

			// We now check  if the confidence is the same as the confidence calculated by HUSRM
			double recalculatedConfidence = supportOfTheRule / supportOfAntecedent;

			if (confidence != recalculatedConfidence)
			{
				//throw new Exception(" The confidence is incorrect for the rule :" + Arrays.toString(antecedent) + " ==>" + Arrays.toString(consequent) + "   confidence : " + confidence + " recalculated confidence: " + recalculatedConfidence);
				throw new Exception(" The confidence is incorrect for the rule :" + ArrayToString(antecedent) + " ==>" + ArrayToString(consequent) + "   confidence : " + confidence + " recalculated confidence: " + recalculatedConfidence);
			}

			// We now check  if the utility is the same as the utility calculated by HUSRM
			if (utility != utilityOfTheRule)
			{
				//throw new Exception(" The utility is incorrect for the rule :" + Arrays.toString(antecedent) + " ==>" + Arrays.toString(consequent) + "   utility : " + utility + " recalculated utility " + utilityOfTheRule);
				throw new Exception(" The utility is incorrect for the rule :" + ArrayToString(antecedent) + " ==>" + ArrayToString(consequent) + "   utility : " + utility + " recalculated utility " + utilityOfTheRule);
			}
		}

		/// <summary>
		/// This method is used to create new rule(s) by adding items to the right side of a rule </summary>
		/// <param name="table"> the utility-table of the rule </param>
		/// <param name="antecedent"> the rule antecedent </param>
		/// <param name="consequent"> the rule consequent </param>
		/// <param name="sequenceIdsAntecedent"> the list of ids of sequences containing the left side of the rule </param>
		/// <exception cref="IOException"> if an error occurs while writing to file </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private void expandRight(UtilityTable table, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIdsAntecedent) throws java.io.IOException
		private void expandRight(UtilityTable table, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIdsAntecedent)
		{

			// We first find the largest item in the left side and right side of the rule
			int largestItemInAntecedent = antecedent[antecedent.Length - 1];
			int largestItemInConsequent = consequent[consequent.Length - 1];

			// We create a new map where we will build the utility table for the new rules that
			// will be created by adding an item to the current rule.
			// Key: an item appended to the rule     Value: the utility-table of the corresponding new rule
			IDictionary<int, UtilityTable> mapItemsTables = new Dictionary<int, UtilityTable>();

	//		// for each sequence containing the original rule (according to its utility table)
			foreach (ElementOfTable element in table.elements)
			{

				// Optimisation: if the "rutil" is 0 for that rule in that sequence,
				// we do not need to scan this sequence.
				if (element.utilityLeft + element.utilityRight + element.utilityLeftRight == 0)
				{
					continue;
				}

				// Get the sequence
				SequenceWithUtility sequence = database.Sequences[element.numeroSequence];

				//============================================================
				// Case 1: for each itemset in BETA or AFTER BETA.....

				// For each itemset after beta:
				for (int i = element.positionBetaItemset; i < sequence.size(); i++)
				{
					// get the itemset
					IList<int> itemsetI = sequence.Itemsets[i];

					// For each item
					for (int j = 0; j < itemsetI.Count; j++)
					{
						int itemJ = itemsetI[j];

						// Check if the item is greater than items in the consequent of the rule 
						// according to the lexicographical order 
						if (itemJ <= largestItemInConsequent)
						{
							// if not, then we continue because that item cannot be added to the rule
							continue;
						}

						// ======= Otherwise, we need to update the utility table of the item ====================

						// Get the utility table of the item
						//UtilityTable tableItemJ = mapItemsTables[itemJ];
						UtilityTable tableItemJ = mapItemsTables.ContainsKey(itemJ) ? mapItemsTables[itemJ] : null;
						if (tableItemJ == null)
						{
							// if no utility table, we create one
							tableItemJ = new UtilityTable(this);
							mapItemsTables[itemJ] = tableItemJ;
						}

						//==========

						// We will add a new element (line) in the utility table
						ElementOfTable newElement = new ElementOfTable(this, element.numeroSequence);

						// We will update the utility by adding the utility of item J
						double profitItemJ = sequence.Utilities[i][j];
						newElement.utility = element.utility + profitItemJ;

						// we will copy the "lutil" value from the original rule
						newElement.utilityLeft = element.utilityLeft;

						// we will copy the "lrutil" value from the original rule
						newElement.utilityLeftRight = element.utilityLeftRight;

						// we will copy the "rutil" value from the original rule
						// but we will subtract the utility of the item J
						newElement.utilityRight = element.utilityRight - profitItemJ;

						// we will copy the position of Alpha and Beta in that sequences because it
						// does not change
						newElement.positionBetaItemset = element.positionBetaItemset;
						newElement.positionAlphaItemset = element.positionAlphaItemset;

						// Then, we will scan itemsets after the beta position in the sequence
						// We will subtract the utility of items that are smaller than item J 
						// according to the lexicographical order from "rutil" because they
						// cannot be added anymore to the new rule.

						// for each such itemset
						for (int z = element.positionBetaItemset; z < sequence.size(); z++)
						{
							IList<int> itemsetZ = sequence.Itemsets[z];

							// for each item W
							for (int w = itemsetZ.Count - 1; w >= 0 ; w--)
							{
								// Optimisation: 
								// if the item is smaller than the larger item in the right side of the rule
								int? itemW = itemsetZ[w];
								if (itemW.Value <= largestItemInConsequent)
								{
									// we break;
									break;
								}

								// otherwise, if item W is smaller than item J
								 if (itemW < itemJ)
								 {

									// We will subtract the utility of W from "rutil"
									double profitItemW = sequence.Utilities[z][w];
									newElement.utilityRight -= profitItemW;
								 }
							}
						}
						// end


						// Now that we have created the element for that sequence and that new rule
						// , we will add the utility table of that new rule
						tableItemJ.addElement(newElement);
					}
				}

				//============================================================
				// CAS 2 : For each itemset from itemset BETA - 1 to itemset ALPHA + 1
				// in the sequence
				//.....
				// For each itemset before the BETA itemset, we will scan the sequence

				// We will look here for the case where an item J is added to the right side of a rule
				// but it is an item found between the left side and right side of the rule in the sequence.
				// In that case, the position beta will change to a new position that we will call beta prime.

				// These two variable will be used to sum the utility of lrutil and lutil
				// after beta has changed
				int sumUtilityLeftRightUntilBetaPrime = 0;
				int sumUtilityLeftUntilBetaPrime = 0;

				// For each itemset from itemset BETA - 1 to itemset ALPHA + 1
				for (int i = element.positionBetaItemset - 1; i > element.positionAlphaItemset; i--)
				{
					// Get the itemset
					IList<int> itemsetI = sequence.Itemsets[i];

					// Get the item
					for (int j = 0; j < itemsetI.Count; j++)
					{
						int itemJ = itemsetI[j];

						//Check if the item could be added to the left side, 
						// right side, or left and right side of the rule according to the lexicographical order
						bool isLeft = itemJ > largestItemInAntecedent && itemJ < largestItemInConsequent;
						bool isLeftRight = itemJ > largestItemInAntecedent && itemJ > largestItemInConsequent;
						bool isRight = itemJ > largestItemInConsequent && itemJ < largestItemInAntecedent;

						// if the item can only be added to left side
						if (isLeft)
						{
							// add the utility of that item to the "lutil"
							double profitItemJ = sequence.Utilities[i][j];
							sumUtilityLeftUntilBetaPrime += (int)profitItemJ;

						}
						else if (isRight)
						{
							// if the item can only be added to right side
							//===========
							// We will need to update the utility table of the new rule
							// that could be generated with that item
							// Get the utility table
							UtilityTable tableItemJ = mapItemsTables[itemJ];
							if (tableItemJ == null)
							{
								// if it does not exist, create a new utility table
								tableItemJ = new UtilityTable(this);
								mapItemsTables[itemJ] = tableItemJ;
							}

							// Create a new element (line) in the utility table for that sequence
							ElementOfTable newElement = new ElementOfTable(this, element.numeroSequence);

							//  Add the utility of the item to the utility of the new rule
							double profitItemJ = sequence.Utilities[i][j];
							newElement.utility = element.utility + profitItemJ;

							// Set the "lutil" value for the new rule
							// which is the utility of the left side of the original rule minus
							// the utility of items that could be append to left side until the current itemset
							newElement.utilityLeft = element.utilityLeft - sumUtilityLeftUntilBetaPrime;

							// Set the "rutil" value similarly
							newElement.utilityLeftRight = element.utilityLeftRight - sumUtilityLeftRightUntilBetaPrime;

							// Now we will scan the sequence from position beta prime and after
							// to calculate:
							// 1) the utility of all items D that are smaller than item J in beta prime
							// or after and can be added to the right side of the rule
							int sumUtilityRUtilItemsSmallerThanX = 0;
							// 2) the utility of all items D that are smaller than item J  in beta prime
							// or afters and can be added to the left or right side of the rule
							int sumUtilityLRUtilItemsSmallerThanX = 0;

							// for each such itemset
							for (int z = i; z < element.positionBetaItemset; z++)
							{
								IList<int> itemsetZ = sequence.Itemsets[z];

								// for each item W
								for (int w = 0; w < itemsetZ.Count; w++)
								{
									int? itemW = itemsetZ[w];

									// check if the item can be appended to the left or right side of the rule
									bool wIsLeftRight = itemW.Value > largestItemInAntecedent && itemW.Value > largestItemInConsequent;
									// check if the item can only be appended to the right side of the rule
									bool wIsRight = itemW.Value > largestItemInConsequent && itemW.Value < largestItemInAntecedent;

									// if the item can only be appended to the right side of the original rule
									// but is smaller than item W that is appended to the right side of the
									// new rule
									if (wIsRight && itemW < itemJ)
									{
										// We will add its profit to the sum for RUtil
										double profitItemW = sequence.Utilities[z][w];
										sumUtilityRUtilItemsSmallerThanX += (int)profitItemW;
									}
									else if (wIsLeftRight && itemW > itemJ)
									{
										// If it is an item that can be appended to the left or right side of
										// the original rule and is greater than the current item J
										// we will add it to the sum for LRUtil
										double profitItemW = sequence.Utilities[z][w];
										sumUtilityLRUtilItemsSmallerThanX += (int)profitItemW;
									}
								}
							}
							// Then we will update the RUTIL for the new rule as follows:
							newElement.utilityRight = element.utilityRight - profitItemJ + sumUtilityLRUtilItemsSmallerThanX - sumUtilityRUtilItemsSmallerThanX;

							// We will update the position of Beta and alpha in the sequence
							newElement.positionBetaItemset = i;
							newElement.positionAlphaItemset = element.positionAlphaItemset;

							// We have finished creating the element for that sequence for the new rule
							// so we will add it to the utility table
							tableItemJ.addElement(newElement);
							//===========

						}
						else if (isLeftRight)
						{
							// If the item can be added to the left or right side of the rule
							//===========
							// ======= We will update the utility table the new rule with this item on the
							//  right side ====================
							// Get the table
							//UtilityTable tableItemJ = mapItemsTables[itemJ];
							UtilityTable tableItemJ = mapItemsTables.ContainsKey(itemJ) ? mapItemsTables[itemJ] : null;
							if (tableItemJ == null)
							{
								// if it does not exist, create a new utility table
								tableItemJ = new UtilityTable(this);
								mapItemsTables[itemJ] = tableItemJ;
							}

							// Create a new element (line) in the table
							ElementOfTable newElement = new ElementOfTable(this, element.numeroSequence);

							// Copy the utility of the original rule and add the utility of the item
							// in the current sequence.
							double profitItemJ = sequence.Utilities[i][j];
							newElement.utility = element.utility + profitItemJ;

							// Set the lutil value as the lutil of the original rule
							// minus the utility of items until the beta prime itemset that could
							// be appended only on the left side of the rule
							newElement.utilityLeft = element.utilityLeft - sumUtilityLeftUntilBetaPrime;

							// Set the lrutil value as the lrutil of the original rule
							// minus the utility of items until the beta prime itemset that could
							// be appended  on the right or left side of the rule
							newElement.utilityLeftRight = element.utilityLeftRight - profitItemJ - sumUtilityLeftRightUntilBetaPrime;

							// We will scan the beta prime itemset and the following itemsets
							// to calculate
							// 1) the profit of all items that can be added on the right side of the rule
							//  which are smaller than J in the beta prime itemset, or appear in a following
							// itemset

							// 
							int sumUtilityRigthItemSmallerThanX = 0;

							// For each itemset 
							for (int z = i; z < element.positionBetaItemset; z++)
							{
								IList<int> itemsetZ = sequence.Itemsets[z];

								//for each item W in that itemset
								for (int w = 0; w < itemsetZ.Count; w++)
								{
									// If w is greater than J according to the lexicographical
									// order, we skip it because we are not interested here.
									int? itemW = itemsetZ[w];
									if (itemW > itemJ)
									{
										break; // optimisatin car itemset est trie
									}
									// Otherwise, we check if the item could be append on the right side
									// but not on the left side
									bool wEstD = itemW.Value > largestItemInConsequent && itemW.Value < largestItemInAntecedent;

									// IF it is the case
									if (wEstD)
									{
										// then we add the sum of the utility of item W in our
										// temporary variable
										double profitItemW = sequence.Utilities[z][w];
										sumUtilityRigthItemSmallerThanX += (int)profitItemW;
									}
								}
							}

							// After that we have the informatoin to update the "RUTIL" value.
							// It is the "rutil" of the original rule minus the content of the temporary
							// variable that we calculated above
							newElement.utilityRight = element.utilityRight - sumUtilityRigthItemSmallerThanX;

							// The first itemset of the right side of the rule has now changed.
							// We thus set beta to the new value "i"
							newElement.positionBetaItemset = i;
							// The left side of the rule has not changed, so Alpha stay the same.
							newElement.positionAlphaItemset = element.positionAlphaItemset;

							// Finally, we add the element that we just created to the utility-table
							// of the new rule.
							tableItemJ.addElement(newElement);
							//===========
						}

					}
				}

			}

			// For each new rule
			foreach (KeyValuePair<int, UtilityTable> entryItemTable in mapItemsTables.SetOfKeyValuePairs())
			{
				// We get the item and its utility table
				int? item = entryItemTable.Key;
				UtilityTable utilityTable = entryItemTable.Value;

				// We check if we should try to expand its left side
				bool shouldExpandLeftSide;
				// We check if we should try to expand its right side
				bool shouldExpandRightSide;

				// If the user deactivate strategy 4, we use a worst upper bound to check that
				if (deactivateStrategy4)
				{
					shouldExpandLeftSide = utilityTable.totalUtility + utilityTable.totalUtilityLeft + utilityTable.totalUtilityLeftRight + utilityTable.totalUtilityRight >= minutil && antecedent.Length + 1 < maxSizeAntecedent;
					shouldExpandRightSide = utilityTable.totalUtility + utilityTable.totalUtilityRight + utilityTable.totalUtilityLeftRight + utilityTable.totalUtilityLeft >= minutil && consequent.Length + 1 < maxSizeConsequent;
				}
				else
				{
					// Otherwise, we use the best upper bound.
					shouldExpandLeftSide = utilityTable.totalUtility + utilityTable.totalUtilityLeft + utilityTable.totalUtilityLeftRight >= minutil && antecedent.Length + 1 < maxSizeAntecedent;
					shouldExpandRightSide = utilityTable.totalUtility + utilityTable.totalUtilityRight + utilityTable.totalUtilityLeftRight + utilityTable.totalUtilityLeft >= minutil && consequent.Length + 1 < maxSizeConsequent;

				}

				// check if the rule is high utility
				bool isHighUtility = utilityTable.totalUtility >= minutil;

				// We create the consequent for the new rule by appending the new item
				int[] newConsequent = new int[consequent.Length + 1];
				Array.Copy(consequent, 0, newConsequent, 0, consequent.Length);
				newConsequent[consequent.Length] = item.Value;

				// We calculate the confidence
				double confidence = (double) utilityTable.elements.Count / (double) sequenceIdsAntecedent.Size;

				// If the rule is high utility and high confidence
				if (isHighUtility && confidence >= minConfidence)
				{
					// We save the rule to file
					saveRule(antecedent, newConsequent, utilityTable.totalUtility, utilityTable.elements.Count, confidence);

					// If we are in debugging mode, we will show the rule in the console
					if (DEBUG)
					{
						//Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item.Value + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item.Value + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementOfTable element in utilityTable.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.numeroSequence + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft + " \t lrutil: " + element.utilityLeftRight + " \t rutil: " + element.utilityRight + " alpha : " + element.positionAlphaItemset + " beta : " + element.positionBetaItemset);
						}
					}

				}
				else
				{
					// If we are in debugging mode and the rule is not high utility and high confidence,
					// we will still show it in the console for debugging
					if (DEBUG)
					{
						//Console.WriteLine("\n\t  LOW UTILITY RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item.Value + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  LOW UTILITY RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item.Value + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementOfTable element in utilityTable.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.numeroSequence + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft + " \t lrutil: " + element.utilityLeftRight + " \t rutil: " + element.utilityRight + " alpha : " + element.positionAlphaItemset + " beta : " + element.positionBetaItemset);
						}
					}
				}

				// If we should try to expand the left side of this rule
				if (shouldExpandLeftSide)
				{
					expandFirstLeft(utilityTable, antecedent, newConsequent, sequenceIdsAntecedent);
				}

				// If we should try to expand the right side of this rule
				if (shouldExpandRightSide)
				{
					expandRight(utilityTable, antecedent, newConsequent, sequenceIdsAntecedent);
				}
			}

			// Check the maximum memory usage
			//MemoryLogger.Instance.checkMemory();
			MemoryLogger.getInstance().checkMemory();
		}

		/// <summary>
		/// This method will recursively try to append items to the left side of a rule to generate
		/// rules containing one more item on the left side.  This method is only called for rules
		/// of size 1*1, thus containing two items (e.g. a -> b) </summary>
		/// <param name="utilityTable"> the rule utility table </param>
		/// <param name="antecedent"> the rule antecedent </param>
		/// <param name="consequent"> the rule consequent </param>
		/// <param name="sequenceIDsConsequent"> the list of sequences ids of sequences containing the rule antecedent </param>
		/// <exception cref="IOException"> if error while writting to file </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private void expandFirstLeft(UtilityTable utilityTable, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIDsConsequent) throws java.io.IOException
		private void expandFirstLeft(UtilityTable utilityTable, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIDsConsequent)
		{

			// We first find the largest item in the left side aof the rule
			int largestItemInAntecedent = antecedent[antecedent.Length - 1];

			// We create a new map where we will build the utility table for the new rules that
			// will be created by adding an item to the current rule.
			// Key: an item appended to the rule     Value: the utility-table of the corresponding new rule
			IDictionary<int, UtilityTableLeft> mapItemUtilityTable = new Dictionary<int, UtilityTableLeft>();

			// for each sequence containing the rule (a line in the utility table of the original rule)
			foreach (ElementOfTable element in utilityTable.elements)
			{
				// Optimisation: if the "lutil" is 0 for that rule in that sequence,
				// we do not need to scan this sequence.
				if (element.utilityLeft == 0)
				{
					continue;
				}

				// Get the sequence
				SequenceWithUtility sequence = database.Sequences[element.numeroSequence];

				// For each itemset before beta
				for (int i = 0; i < element.positionBetaItemset; i++)
				{
					IList<int> itemsetI = sequence.Itemsets[i];

					// For each item
					for (int j = 0; j < itemsetI.Count; j++)
					{
						int itemJ = itemsetI[j];

						// Check if the item is greater than items in the antecedent of the rule 
						// according to the lexicographical order 
						if (itemJ <= largestItemInAntecedent)
						{
							continue;
						}

						// ======= Otherwise, we need to update the utility table of the item ====================
						// Get the utility table of the item
						//UtilityTableLeft tableItemJ = mapItemUtilityTable[itemJ];
						UtilityTableLeft tableItemJ = mapItemUtilityTable.ContainsKey(itemJ) ? mapItemUtilityTable[itemJ] : null;
						if (tableItemJ == null)
						{
							// if no utility table, we create one
							tableItemJ = new UtilityTableLeft(this);
							mapItemUtilityTable[itemJ] = tableItemJ;
						}


						// We will add a new element (line) in the utility table
						ElementTableLeft newElement = new ElementTableLeft(this, element.numeroSequence);

						// we will update the utility vlaue of that rule by adding the utility of the item
						// in that sequence
						double profitItemJ = sequence.Utilities[i][j];
						newElement.utility = element.utility + profitItemJ;


						// If the user deactivate strategy 4, we will store the lrutil in the column
						// called lutil
						if (deactivateStrategy4)
						{
							newElement.utilityLeft = element.utilityLeft + element.utilityLeftRight + element.utilityRight - profitItemJ;
						}
						else
						{
							// otherwise we really calculate the lutil
							newElement.utilityLeft = element.utilityLeft + element.utilityLeftRight - profitItemJ;
						}


						// Then, we will scan itemsets from the first one until the beta -1  itemset 
						// in the sequence.
						// We will subtract the utility of items that are smaller than item J 
						// according to the lexicographical order from "lutil" because they
						// cannot be added anymore to the new rule.

						// For each itemset before the beta itemset
						for (int z = 0; z < element.positionBetaItemset; z++)
						{
							IList<int> itemsetZ = sequence.Itemsets[z];

							// For each item W in that itemset
							for (int w = itemsetZ.Count - 1; w >= 0 ; w--)
							{
								int? itemW = itemsetZ[w];

								// if the item is smaller than the larger item in the left side of the rule
								if (itemW.Value <= largestItemInAntecedent)
								{
									// we break;
									break;
								}

								// otherwise, if item W is smaller than item J
								if (itemJ > itemW)
								{
									// We will subtract the utility of W from "rutil"
									double profitItemW = sequence.Utilities[z][w];
									newElement.utilityLeft -= profitItemW;
								}
							}
						}
						// end


						// Now that we have created the element for that sequence and that new rule
						// we will add the utility table of that new rule
						tableItemJ.addElement(newElement);

					}
				}
			}

			// After that for each new rule, we create a table to store the beta values 
			// for each sequence where the new rule appears.
			// The reason is that the "beta" column of any new rules that will be generated
			// by recursively adding to the left, will staty the same. So we don't store it in the
			// utility tble of the rule directly but in a separated structure.

			// Beta is a map where the key is a sequence id
			//   and the key is the position of an itemset in the sequence.
			IDictionary<int, int> tableBeta = null;


			// For each new rule
			foreach (KeyValuePair<int, UtilityTableLeft> entryItemTable in mapItemUtilityTable.SetOfKeyValuePairs())
			{
				// We get the item that was added to create the new rule
				int item = entryItemTable.Key;
				// We get the utility table of the new rule
				UtilityTableLeft tableItem = entryItemTable.Value;


				// We check if we should try to expand its left side
				bool shouldExpandLeftSide = tableItem.utility + tableItem.utilityLeft >= minutil && antecedent.Length + 1 < maxSizeAntecedent;

				// We need to calculate the list of sequences ids containing the antecedent of the new
				// rule since the antecedent has changed
				ListSequenceIDs sequenceIdentifiersNewAntecedent = null;

				// To calculate the confidence
				double confidence = 0;

				// If we should try to expand the left side of the rule
				// or if the rule is high utility, we recalculate the sequences ids containing
				// the antecedent
				if (shouldExpandLeftSide || tableItem.utility >= minutil)
				{
					// We obtain the list of sequence ids for the item
					ListSequenceIDs sequencesIdsItem = mapItemSequences[item];

					// We perform the intersection of the sequences ids of the antecedent
					// with those of the item to obtain the sequence ids of the new antecedent.
					sequenceIdentifiersNewAntecedent = sequenceIDsConsequent.intersection(sequencesIdsItem);

					// we calculate the confidence
					confidence = (double) tableItem.elements.Count / (double) sequenceIdentifiersNewAntecedent.Size;
				}

				// if the new rule is high utility and has a high confidence
				bool isHighUtilityAndHighConfidence = tableItem.utility >= minutil && confidence >= minConfidence;
				if (isHighUtilityAndHighConfidence)
				{

					// We create the antecedent for the new rule by appending the new item
					int[] nouvelAntecedent = new int[antecedent.Length + 1];
					Array.Copy(antecedent, 0, nouvelAntecedent, 0, antecedent.Length);
					nouvelAntecedent[antecedent.Length] = item;

					// We save the rule to file
					saveRule(nouvelAntecedent, consequent, tableItem.utility, tableItem.elements.Count, confidence);

					// If we are in debugging mode, we will show the rule in the console
					if (DEBUG)
					{
						//Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementTableLeft element in tableItem.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.sequenceID + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft);
						}
					}

				}
				else
				{
					// if we are in debuging mode
					if (DEBUG)
					{
						//Console.WriteLine("\n\t  LOW UTILITY SEQ. RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item.Value + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  LOW UTILITY SEQ. RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item + "   utility " + utilityTable.totalUtility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementTableLeft element in tableItem.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.sequenceID + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft);
						}
					}
				}
				// If we should try to expand the left side of this rule
				if (shouldExpandLeftSide)
				{
					// We create the antecedent for the new rule by appending the new item
					int[] newAntecedent = new int[antecedent.Length + 1];
					Array.Copy(antecedent, 0, newAntecedent, 0, antecedent.Length);
					newAntecedent[antecedent.Length] = item;

					// We create the table for storing the beta position in each sequence
					if (tableBeta == null)
					{
						tableBeta = new Dictionary<int, int>();
						// We loop over each line from the original utility table and copy the 
						// beta value for each line

						// For each element of the utility of the original rule
						foreach (ElementOfTable element in utilityTable.elements)
						{
							// copy the beta position
							tableBeta[element.numeroSequence] = element.positionBetaItemset;
						}
					}

					// we recursively try to expand this rule
					expandSecondLeft(tableItem, newAntecedent, consequent, sequenceIdentifiersNewAntecedent, tableBeta);

				}
			}
			// We check the memory usage for statistics
			//MemoryLogger.Instance.checkMemory();
			MemoryLogger.getInstance().checkMemory();
		}

		/// <summary>
		/// This method will recursively try to append items to the left side of a rule to generate
		/// rules containing one more item on the left side.  This method is called for rules
		/// containing at least 2 items on their left side already. For rules having 1 item on their left side
		/// another method is used instead.
		/// </summary>
		/// <param name="utilityTable"> the rule utility table </param>
		/// <param name="antecedent"> the rule antecedent </param>
		/// <param name="consequent"> the rule consequent </param>
		/// <param name="sequenceIDsConsequent"> the list of sequences ids of sequences containing the rule antecedent </param>
		/// <exception cref="IOException"> if error while writting to file </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private void expandSecondLeft(UtilityTableLeft utilityTable, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIDsConsequent, java.util.Map<int, int> tableBeta) throws java.io.IOException
		private void expandSecondLeft(UtilityTableLeft utilityTable, int[] antecedent, int[] consequent, ListSequenceIDs sequenceIDsConsequent, IDictionary<int, int> tableBeta)
		{


			// We first find the largest item in the left side aof the rule
			int largestItemInAntecedent = antecedent[antecedent.Length - 1];

			// We create a new map where we will build the utility table for the new rules that
			// will be created by adding an item to the current rule.
			// Key: an item appended to the rule     Value: the utility-table of the corresponding new rule
			IDictionary<int, UtilityTableLeft> mapItemUtilityTable = new Dictionary<int, UtilityTableLeft>();

			// for each sequence containing the rule (a line in the utility table of the original rule)
			foreach (ElementTableLeft element in utilityTable.elements)
			{
				// Optimisation: if the "lutil" is 0 for that rule in that sequence,
				// we do not need to scan this sequence.
				if (element.utilityLeft == 0)
				{
					continue;
				}

				// Get the sequence
				SequenceWithUtility sequence = database.Sequences[element.sequenceID];

				// Get the beta position in that sequence
				int? positionBetaItemset = tableBeta[element.sequenceID];

				// For each itemset before beta
				for (int i = 0; i < positionBetaItemset.Value; i++)
				{
					IList<int> itemsetI = sequence.Itemsets[i];

					//for each  item
					for (int j = 0; j < itemsetI.Count; j++)
					{
						int itemJ = itemsetI[j];

						// Check if the item is greater than items in the antecedent of the rule 
						// according to the lexicographical order 
						if (itemJ <= largestItemInAntecedent)
						{
							continue;
						}

						// ======= Otherwise, we need to update the utility table of the item ====================
						// Get the utility table of the item
						UtilityTableLeft tableItemJ = mapItemUtilityTable.ContainsKey(itemJ) ? mapItemUtilityTable[itemJ] : null;
						if (tableItemJ == null)
						{
							// if no utility table, we create one
							tableItemJ = new UtilityTableLeft(this);
							mapItemUtilityTable[itemJ] = tableItemJ;
						}

						// We will add a new element (line) in the utility table
						ElementTableLeft newElement = new ElementTableLeft(this, element.sequenceID);


						// we will update the utility vlaue of that rule by adding the utility of the item
						// in that sequence
						double utilityItemJ = sequence.Utilities[i][j];
						newElement.utility = element.utility + utilityItemJ;

						// The lutil value is updated by subtracting the utility of the item
						newElement.utilityLeft = element.utilityLeft - utilityItemJ;

						// Then, we will scan itemsets from the first one until the beta -1  itemset 
						// in the sequence.
						// We will subtract the utility of items that are smaller than item J 
						// according to the lexicographical order from "lutil" because they
						// cannot be added anymore to the new rule.

						// for each itemset
						for (int z = 0; z < positionBetaItemset.Value; z++)
						{
							IList<int> itemsetZ = sequence.Itemsets[z];

							// for each item
							for (int w = itemsetZ.Count - 1; w >= 0 ; w--)
							{
								int? itemW = itemsetZ[w];
								// if the item is smaller than the larger item in the left side of the rule
								if (itemW.Value <= largestItemInAntecedent)
								{
									break;
								}
								// otherwise, if item W is smaller than item J
								if (itemW < itemJ)
								{
									// We will subtract the utility of W from "rutil"
									double utilityItemW = sequence.Utilities[z][w];
									newElement.utilityLeft -= utilityItemW;
								}
							}
						}
						// end

						// Now that we have created the element for that sequence and that new rule
						// we will add that element to tthe utility table of that new rule
						tableItemJ.addElement(newElement);

					}
				}
			}

			// For each new rule
			foreach (KeyValuePair<int, UtilityTableLeft> entryItemTable in mapItemUtilityTable.SetOfKeyValuePairs())
			{
				// We get the item that was added to create the new rule
				int item = entryItemTable.Key;
				// We get the utility table of the new rule
				UtilityTableLeft tableItem = entryItemTable.Value;


				// We check if we should try to expand its left side
				bool shouldExpandLeft = tableItem.utility + tableItem.utilityLeft >= minutil && antecedent.Length + 1 < maxSizeAntecedent;

				// We check if the rule is high utility
				bool isHighUtility = tableItem.utility >= minutil;

				double confidence = 0;

				// We need to calculate the list of sequences ids containing the antecedent of the new
				// rule since the antecedent has changed
				ListSequenceIDs sequenceIdentifiersNewAntecedent = null;

				// If we should try to expand the left side of the rule
				// or if the rule is high utility, we recalculate the sequences ids containing
				// the antecedent
				if (shouldExpandLeft || isHighUtility)
				{
					// We obtain the list of sequence ids for the item
					ListSequenceIDs numerosequencesItem = mapItemSequences[item];

					// We perform the intersection of the sequences ids of the antecedent
					// with those of the item to obtain the sequence ids of the new antecedent.
					sequenceIdentifiersNewAntecedent = sequenceIDsConsequent.intersection(numerosequencesItem);

					// we calculate the confidence
					confidence = (double) tableItem.elements.Count / (double) sequenceIdentifiersNewAntecedent.Size;
				}

				// if the new rule is high utility and has a high confidence
				if (isHighUtility && confidence >= minConfidence)
				{

					// We create the antecedent for the new rule by appending the new item
					int[] newAntecedent = new int[antecedent.Length + 1];
					Array.Copy(antecedent, 0, newAntecedent, 0, antecedent.Length);
					newAntecedent[antecedent.Length] = item;

					// We save the rule to file
					saveRule(newAntecedent, consequent, tableItem.utility, tableItem.elements.Count, confidence);

					// If we are in debugging mode, we will show the rule in the console
					if (DEBUG)
					{
						// print the rule
						//Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item.Value + "   utility " + utilityTable.utility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  HIGH UTILITY SEQ. RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item + "   utility " + utilityTable.utility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementTableLeft element in tableItem.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.sequenceID + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft);
						}
					}
				}
				else
				{
					// if we are in debuging mode
					if (DEBUG)
					{
						// print the rule
						//Console.WriteLine("\n\t  LOW UTILITY SEQ. RULE: " + Arrays.toString(antecedent) + " --> " + Arrays.toString(consequent) + "," + item.Value + "   utility " + utilityTable.utility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);
						Console.WriteLine("\n\t  LOW UTILITY SEQ. RULE: " + ArrayToString(antecedent) + " --> " + ArrayToString(consequent) + "," + item + "   utility " + utilityTable.utility + " support : " + utilityTable.elements.Count + " confidence : " + confidence);

						foreach (ElementTableLeft element in tableItem.elements)
						{
							Console.WriteLine("\t      SEQ:" + element.sequenceID + " \t utility: " + element.utility + " \t lutil: " + element.utilityLeft);
						}
					}
				}

				// If we should try to expand the left side of this rule
				if (shouldExpandLeft)
				{
					// We create the antecedent for the new rule by appending the new item
					int[] nouvelAntecedent = new int[antecedent.Length + 1];
					Array.Copy(antecedent, 0, nouvelAntecedent, 0, antecedent.Length);
					nouvelAntecedent[antecedent.Length] = item;

					// we recursively call this method
					expandSecondLeft(tableItem, nouvelAntecedent, consequent, sequenceIdentifiersNewAntecedent, tableBeta);
				}
			}
			// We check the memory usage
			//MemoryLogger.Instance.checkMemory();
			MemoryLogger.getInstance().checkMemory();
		}


		/// <summary>
		/// Print statistics about the last algorithm execution to System.out.
		/// </summary>
		public virtual void printStats()
		{
			Console.WriteLine("==============================================================================");
			Console.WriteLine("-------------- HUSRM algorithm for high utility sequential rule mining -------------");
			Console.WriteLine("==============================================================================");
			Console.WriteLine("\tminutil: " + minutil);
			Console.WriteLine("\tSequential rules count: " + ruleCount);
			Console.WriteLine("\tTotal time : " + (timeEnd - timeStart) + " ms");
			//Console.WriteLine("\tMax memory (mb) : " + MemoryLogger.Instance.MaxMemory);
			Console.WriteLine("\tMax memory (mb) : " + MemoryLogger.getInstance().getMaxMemory());
			Console.WriteLine("==============================================================================");
		}

		//============================================================================================================================
		// =========================================== CLASSES FOR STORING LISTS OF SEQUENCE IDs===================
		//============================================================================================================================

		/// <summary>
		/// This interface represents a list of sequences ids
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
		public interface ListSequenceIDs
		{

			/// <summary>
			/// This method adds a sequence id to this list </summary>
			/// <param name="int"> the sequence id </param>
			void addSequenceID(int noSequence);

			/// <summary>
			/// Get the number of sequence ids </summary>
			/// <returns> the number of sequence ids </returns>
			int Size {get;}

			/// <summary>
			///  Method to intersect two lists of sequences ids </summary>
			/// <param name="vector"> another list </param>
			/// <returns> the intersection of this list and the other list. </returns>
			ListSequenceIDs intersection(ListSequenceIDs vector2);
		}

		/// <summary>
		/// This class represents a list of sequences ids implemented by a bit vector
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
		public class ListSequenceIDsBitVector : ListSequenceIDs
		{
			private readonly AlgoHUSRM outerInstance;

            // the internal bitset
            //internal BitArray bitset = new BitArray();
            internal BitArray bitset = new BitArray(4096); //64
			// the number of bit set to 1 in the bitset
			internal int size = -1;

			/// <summary>
			/// Constructor
			/// </summary>
			public ListSequenceIDsBitVector(AlgoHUSRM outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public virtual void addSequenceID(int bit)
			{
				bitset.Set(bit, true);
			}

			/// <summary>
			/// Get the number of sequence ids </summary>
			/// <returns> the number of sequence ids </returns>
			public virtual int Size
			{
				get
				{
					// if we don't know the size
					if (size == -1)
					{
                        // we calculate it but remember it in variable "size" for future use.
                        //size = bitset.cardinality();

                        int count = 0;

                        for (int i = 0; i < bitset.Count; i++)
                        {
                            if (bitset[i] == true)
                                count++;
                        }

                        size = count;
					}
					// return the size
					return size;
				}
			}

			/// <summary>
			///  Method to intersect two lists of sequences ids </summary>
			/// <param name="vector"> another list </param>
			/// <returns> the intersection of this list and the other list. </returns>
			public virtual ListSequenceIDs intersection(ListSequenceIDs vector2)
			{
				//  we get the first vector
				ListSequenceIDsBitVector bitVector2 = (ListSequenceIDsBitVector) vector2;

				// we create a new vector for the result
				ListSequenceIDsBitVector result = new ListSequenceIDsBitVector(outerInstance);
				// we clone the first bit vecotr
				//result.bitset = (BitArray) bitset.clone();
				result.bitset = (BitArray) bitset.Clone();
				// we intersect both bit vector
				result.bitset.And(bitVector2.bitset);
				// Return the result
				return result;
			}

			/// <summary>
			/// Get a string representation of this list </summary>
			/// <returns> a string </returns>
			public override string ToString()
			{
				return bitset.ToString();
			}
		}

		//==================================
		/// <summary>
		/// This class represents a list of sequences ids implemented by an array list
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
			public class ListSequenceIDsArrayList : ListSequenceIDs
			{
				private readonly AlgoHUSRM outerInstance;

				// the internal array list representation
				internal IList<int> list = new List<int>();

				/// <summary>
				/// Constructor
				/// </summary>
				public ListSequenceIDsArrayList(AlgoHUSRM outerInstance)
				{
					this.outerInstance = outerInstance;
				}

				/// <summary>
				/// This method adds a sequence id to this list </summary>
				/// <param name="int"> the sequence id </param>
				public virtual void addSequenceID(int noSequence)
				{
					list.Add(noSequence);
				}


				/// <summary>
				/// Get the number of sequence ids </summary>
				/// <returns> the number of sequence ids </returns>
				public virtual int Size
				{
					get
					{
						return list.Count;
					}
				}

				/// <summary>
				///  Method to intersect two lists of sequences ids </summary>
				/// <param name="vector"> another list </param>
				/// <returns> the intersection of this list and the other list. </returns>
				public virtual ListSequenceIDs intersection(ListSequenceIDs list2)
				{
					// Get the second list
					ListSequenceIDsArrayList arrayList2 = (ListSequenceIDsArrayList) list2;
					// Create a new list for the result
					ListSequenceIDs result = new ListSequenceIDsArrayList(outerInstance);

					// for each sequence id in this list
					foreach (int no in list)
					{
						// if it appear in the second list
						//bool appearInSecondList = Collections.binarySearch(arrayList2.list, no) >= 0;
						bool appearInSecondList = Array.BinarySearch(arrayList2.list.ToArray(), no) >= 0;
						if (appearInSecondList)
						{
							// then we add it to the new list
							result.addSequenceID(no);
						}
					}
					// return the result
					return result;
				}

				/// <summary>
				/// Get a string representation of this list </summary>
				/// <returns> a string </returns>
				public override string ToString()
				{
					return list.ToString();
				}
			}

        //============================================================================================================================
        // =========================================== CLASS FOR LEFT-UTILITY-TABLES ===========================================
        //============================================================================================================================


        private static string ArrayToString<T>(IEnumerable<T> list)
        {
            return "[" + string.Join(",", list) + "]";
        }

        /// <summary>
        /// This class represents a utility-table used for left expansions (what we call a left-utility table)
        /// @author Souleymane Zida, Philippe Fournier-Viger
        /// </summary>
        public class UtilityTableLeft
		{
			private readonly AlgoHUSRM outerInstance;

			// the list of elements (lines) in that utility table
			internal IList<ElementTableLeft> elements = new List<ElementTableLeft>();
			// the total utility in that table
			internal int utility = 0;
			// the toal lutil values of elements in that table
			internal int utilityLeft = 0;

			/// <summary>
			/// Constructor
			/// </summary>
			public UtilityTableLeft(AlgoHUSRM outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			/// <summary>
			/// Add a new element (line) to that table </summary>
			/// <param name="element"> the new element </param>
			public virtual void addElement(ElementTableLeft element)
			{
				// add the element
				elements.Add(element);
				// add the utility of this element to the total utility of that table
				utility += (int)element.utility;
				// add the "lutil" utilit of this element to the total for that table
				utilityLeft += (int)element.utilityLeft;
			}
		}

		/// <summary>
		/// This class represents a element(line) of a utility-table used for left expansions
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
		public class ElementTableLeft
		{
			private readonly AlgoHUSRM outerInstance;

			// the corresponding sequence id
			internal int sequenceID;
			// the utility
			internal double utility;
			// the "lutil" value
			internal double utilityLeft;

			/// <summary>
			/// Constructor </summary>
			/// <param name="sequenceID"> the sequence id </param>
			public ElementTableLeft(AlgoHUSRM outerInstance, int sequenceID)
			{
				this.outerInstance = outerInstance;
				this.sequenceID = sequenceID;
				this.utility = 0;
				this.utilityLeft = 0;
			}

			/// <summary>
			/// Constructor </summary>
			/// <param name="sequenceID"> a sequence id </param>
			/// <param name="utility"> the utility </param>
			/// <param name="utilityLeft"> the lutil value </param>
			public ElementTableLeft(AlgoHUSRM outerInstance, int sequenceID, int utility, int utilityLeft)
			{
				this.outerInstance = outerInstance;
				this.sequenceID = sequenceID;
				this.utility = utility;
				this.utilityLeft = utilityLeft;
			}
		}


		//============================================================================================================================
		// ===========================================  CLASS FOR LEFT-RIGHT UTILITY-TABLES===========================================
		//============================================================================================================================


		/// <summary>
		/// This class represents a utility-table used for left or right expansions (what we call a left-right utility table)
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
		public class UtilityTable
		{
			private readonly AlgoHUSRM outerInstance;

			// the list of elements (lines) in that utility table
			internal IList<ElementOfTable> elements = new List<ElementOfTable>();
			// the total utility in that table
			internal double totalUtility = 0;
			// the toal lutil values of elements in that table
			internal double totalUtilityLeft = 0;
			// the toal lrutil values of elements in that table
			internal double totalUtilityLeftRight = 0;
			// the toal rutil values of elements in that table
			internal double totalUtilityRight = 0;

			/// <summary>
			/// Constructor
			/// </summary>
			public UtilityTable(AlgoHUSRM outerInstance)
			{
				this.outerInstance = outerInstance;

			}

			/// <summary>
			/// Add a new element (line) to that table </summary>
			/// <param name="element"> the new element </param>
			public virtual void addElement(ElementOfTable element)
			{
				// add the element
				elements.Add(element);
				// make the sum of the utility, lutil, rutil and lrutil values
				totalUtility += element.utility;
				totalUtilityLeft += element.utilityLeft;
				totalUtilityLeftRight += element.utilityLeftRight;
				totalUtilityRight += element.utilityRight;
			}
		}

		/// <summary>
		/// This class represents a element(line) of a utility-table used for left or right expansions
		/// @author Souleymane Zida, Philippe Fournier-Viger
		/// </summary>
		public class ElementOfTable
		{
			private readonly AlgoHUSRM outerInstance;

			// the corresponding sequence id
			internal int numeroSequence;
			// the utility
			internal double utility;
			// the lutil value
			internal double utilityLeft;
			// the lrutil value
			internal double utilityLeftRight;
			// the rutil value
			internal double utilityRight;
			// the alpha and beta values
			internal int positionAlphaItemset = -1;
			internal int positionBetaItemset = -1;

			/// <summary>
			/// Constructor </summary>
			/// <param name="sequenceID"> the sequence id </param>
			public ElementOfTable(AlgoHUSRM outerInstance, int sequenceID)
			{
				this.outerInstance = outerInstance;
				this.numeroSequence = sequenceID;
				this.utility = 0;
				this.utilityLeft = 0;
				this.utilityLeftRight = 0;
				this.utilityRight = 0;
			}

			/// <summary>
			/// Constructor </summary>
			/// <param name="sequenceID"> a sequence id </param>
			/// <param name="utility"> the utility </param>
			/// <param name="utilityLeft"> the lutil value </param>
			/// <param name="utilityLeftRight"> the lrutil value </param>
			/// <param name="utilityRight"> the rutil value </param>
			public ElementOfTable(AlgoHUSRM outerInstance, int sequenceID, double utility, double utilityLeft, double utilityLeftRight, double utilityRight)
			{
				this.outerInstance = outerInstance;
				this.numeroSequence = sequenceID;
				this.utility = utility;
				this.utilityLeft = utilityLeft;
				this.utilityLeftRight = utilityLeftRight;
				this.utilityRight = utilityRight;
			}
		}
	}

}