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
        public static CmdtyStorage<T>.IAddMaxInventory WithConstantInjectWithdrawRange<T>([NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                            double minInjectWithdrawRate, double maxInjectWithdrawRate)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var constantInjectWithdrawConstraint = new ConstantInjectWithdrawConstraint(minInjectWithdrawRate, maxInjectWithdrawRate);
            return builder.WithInjectWithdrawConstraint(constantInjectWithdrawConstraint);
        }

        public static CmdtyStorage<T>.IAddMaxInventory WithInventoryDependentInjectWithdrawRange<T>([NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                            IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawRanges);
            return builder.WithInjectWithdrawConstraint(polynomialInjectWithdrawConstraint);
        }

        public static CmdtyStorage<T>.IAddMaxInventory WithTimeAndInventoryVaryingInjectWithdrawRates<T>(
                    [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                    [NotNull] IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            var sortedList = new SortedList<T, IInjectWithdrawConstraint>();
            foreach ((T period, IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRange) in injectWithdrawRanges)
            {
                if (period == null)
                    throw new ArgumentException("Null Period in collection.", nameof(injectWithdrawRanges));
                if (injectWithdrawRange == null)
                    throw new ArgumentException("Null InjectWithdrawRanges in collection.", nameof(injectWithdrawRange));

                var injectWithdrawRangeArray = injectWithdrawRange.ToArray();
                if (injectWithdrawRangeArray.Length == 0)
                    throw new ArgumentException("", nameof(injectWithdrawRanges));

                IInjectWithdrawConstraint constraint;
                if (injectWithdrawRangeArray.Length == 1)
                {
                    (_, (double minInjectWithdraw, double maxInjectWithdraw)) = injectWithdrawRangeArray[0];
                    constraint = new ConstantInjectWithdrawConstraint(minInjectWithdraw, maxInjectWithdraw);
                }
                else
                {
                    constraint = new PolynomialInjectWithdrawConstraint(injectWithdrawRangeArray);
                }

                try
                {
                    sortedList.Add(period, constraint);
                }
                catch (ArgumentException) // TODO unit test
                {
                    throw new ArgumentException("Repeated periods found in inject/withdraw ranges", nameof(injectWithdrawRanges));
                }
            }

            if (sortedList.Count == 0)
                throw new ArgumentException("No inject/withdraw constrains provided.", nameof(injectWithdrawRanges));

            // TODO create helper method (in Cmdty.TimeSeries) to create TimeSeries from piecewise data?
            T firstPeriod = sortedList.Keys[0];
            T lastPeriod = sortedList.Keys[sortedList.Count - 1];
            int numPeriods = lastPeriod.OffsetFrom(firstPeriod) + 1;

            var timeSeriesValues = new IInjectWithdrawConstraint[numPeriods];

            T periodLoop = firstPeriod;
            IInjectWithdrawConstraint constraintLoop = sortedList.Values[0];

            int arrayCounter = 0;
            int sortedListCounter = 0;
            do
            {
                if (periodLoop.Equals(sortedList.Keys[sortedListCounter]))
                {
                    constraintLoop = sortedList.Values[sortedListCounter];
                    sortedListCounter++;
                }
                timeSeriesValues[arrayCounter] = constraintLoop;

                periodLoop = periodLoop.Offset(1);
                arrayCounter++;
            } while (periodLoop.CompareTo(lastPeriod) <= 0);

            var timeSeries = new TimeSeries<T, IInjectWithdrawConstraint>(firstPeriod, timeSeriesValues);

            IInjectWithdrawConstraint GetInjectWithdrawConstraint(T period)
            {
                if (period.CompareTo(timeSeries.End) > 0)
                    return timeSeries[timeSeries.End];
                return timeSeries[period];
            }

            return builder.WithInjectWithdrawConstraint(GetInjectWithdrawConstraint);
        }

    }
}
