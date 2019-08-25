#region License
// Copyright (c) 2019 Jake Fowler
//
// Permission is hereby granted, free of charge, to any person 
// obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without 
// restriction, including without limitation the rights to use, 
// copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following 
// conditions:
//
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage.Core
{
    public static class CmdtyStorageBuilderExtensions
    {
        public static CmdtyStorage<T>.IAddMinInventory WithConstantInjectWithdrawRange<T>([NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                            double minInjectWithdrawRate, double maxInjectWithdrawRate)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var constantInjectWithdrawConstraint = new ConstantInjectWithdrawConstraint(minInjectWithdrawRate, maxInjectWithdrawRate);
            return builder.WithInjectWithdrawConstraint(constantInjectWithdrawConstraint);
        }

        public static CmdtyStorage<T>.IAddMinInventory WithInventoryDependentInjectWithdrawRange<T>([NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                            IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawRanges);
            return builder.WithInjectWithdrawConstraint(polynomialInjectWithdrawConstraint);
        }

        public static CmdtyStorage<T>.IAddInjectionCost WithTimeAndInventoryVaryingInjectWithdrawRates<T>(
                    [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                    [NotNull] IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            var injectWithdrawSortedList = new SortedList<T, IInjectWithdrawConstraint>();
            var inventoryRangeList = new List<InventoryRange>();

            foreach ((T period, IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRange) in injectWithdrawRanges)
            {
                if (period == null)
                    throw new ArgumentException("Null Period in collection.", nameof(injectWithdrawRanges));
                if (injectWithdrawRange == null)
                    throw new ArgumentException("Null InjectWithdrawRanges in collection.", nameof(injectWithdrawRange));

                var injectWithdrawRangeArray = injectWithdrawRange.ToArray();
                if (injectWithdrawRangeArray.Length < 2)
                    throw new ArgumentException($"Period {period} contains less than 2 inject/withdraw/inventory constraints.", nameof(injectWithdrawRanges));

                IInjectWithdrawConstraint constraint = new PolynomialInjectWithdrawConstraint(injectWithdrawRangeArray);

                double minInventory = injectWithdrawRangeArray.Min(inventoryRange => inventoryRange.Inventory);
                double maxInventory = injectWithdrawRangeArray.Max(inventoryRange => inventoryRange.Inventory);

                try
                {
                    injectWithdrawSortedList.Add(period, constraint);
                    inventoryRangeList.Add(new InventoryRange(minInventory, maxInventory));
                }
                catch (ArgumentException) // TODO unit test
                {
                    throw new ArgumentException("Repeated periods found in inject/withdraw ranges", nameof(injectWithdrawRanges));
                }
            }

            if (injectWithdrawSortedList.Count == 0)
                throw new ArgumentException("No inject/withdraw constrains provided.", nameof(injectWithdrawRanges));

            // TODO create helper method (in Cmdty.TimeSeries) to create TimeSeries from piecewise data?
            T firstPeriod = injectWithdrawSortedList.Keys[0];
            T lastPeriod = injectWithdrawSortedList.Keys[injectWithdrawSortedList.Count - 1];
            int numPeriods = lastPeriod.OffsetFrom(firstPeriod) + 1;

            var timeSeriesInjectWithdrawValues = new IInjectWithdrawConstraint[numPeriods];
            var timeSeriesInventoryRangeValues = new InventoryRange[numPeriods];

            T periodLoop = firstPeriod;
            IInjectWithdrawConstraint constraintLoop = injectWithdrawSortedList.Values[0];
            InventoryRange inventoryRangeLoop = inventoryRangeList[0];

            int arrayCounter = 0;
            int sortedListCounter = 0;
            do
            {
                if (periodLoop.Equals(injectWithdrawSortedList.Keys[sortedListCounter]))
                {
                    constraintLoop = injectWithdrawSortedList.Values[sortedListCounter];
                    inventoryRangeLoop = inventoryRangeList[sortedListCounter];
                    sortedListCounter++;
                }
                timeSeriesInjectWithdrawValues[arrayCounter] = constraintLoop;
                timeSeriesInventoryRangeValues[arrayCounter] = inventoryRangeLoop;

                periodLoop = periodLoop.Offset(1);
                arrayCounter++;
            } while (periodLoop.CompareTo(lastPeriod) <= 0);

            var injectWithdrawTimeSeries = new TimeSeries<T, IInjectWithdrawConstraint>(firstPeriod, timeSeriesInjectWithdrawValues);
            var inventoryRangeTimeSeries = new TimeSeries<T, InventoryRange>(firstPeriod, timeSeriesInventoryRangeValues);

            IInjectWithdrawConstraint GetInjectWithdrawConstraint(T period)
            {
                if (period.CompareTo(injectWithdrawTimeSeries.End) > 0)
                    return injectWithdrawTimeSeries[injectWithdrawTimeSeries.End];
                return injectWithdrawTimeSeries[period];
            }

            CmdtyStorage<T>.IAddMinInventory addMinInventory = builder.WithInjectWithdrawConstraint(GetInjectWithdrawConstraint);

            double GetMinInventory(T period)
            {
                if (period.CompareTo(inventoryRangeTimeSeries.End) > 0)
                    return inventoryRangeTimeSeries[inventoryRangeTimeSeries.End].MaxInventory;
                return inventoryRangeTimeSeries[period].MaxInventory;
            }

            CmdtyStorage<T>.IAddMaxInventory addMaxInventory = addMinInventory.WithMinInventory(GetMinInventory);

            double GetMaxInventory(T period)
            {
                if (period.CompareTo(inventoryRangeTimeSeries.End) > 0)
                    return inventoryRangeTimeSeries[inventoryRangeTimeSeries.End].MaxInventory;
                return inventoryRangeTimeSeries[period].MaxInventory;
            }
            
            return addMaxInventory.WithMaxInventory(GetMaxInventory);
        }

        // TODO delete?
        //public static CmdtyStorage<T>.IAddTerminalStorageState WithStoragePropertyByPeriod<T>(
        //    [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder, 
        //    [NotNull] IEnumerable<StoragePropertiesByPeriod<T>> storageProperties)
        //    where T : ITimePeriod<T>
        //{
        //    if (builder == null) throw new ArgumentNullException(nameof(builder));
        //    if (storageProperties == null) throw new ArgumentNullException(nameof(storageProperties));

        //    CmdtyStorage<T>.IAddMinInventory addMinInventory = builder.WithTimeDependentInjectWithdrawRange(null);
        //    CmdtyStorage<T>.IAddMaxInventory addMaxInventory = addMinInventory.WithMinInventory(null);

        //    CmdtyStorage<T>.IAddInjectionCost withMaxInventory = addMaxInventory.WithMaxInventory(null);

        //    CmdtyStorage<T>.IAddCmdtyConsumedOnInject addCmdtyConsumedOnInject = withMaxInventory.WithInjectionCost(null);


        //}

    }
}
