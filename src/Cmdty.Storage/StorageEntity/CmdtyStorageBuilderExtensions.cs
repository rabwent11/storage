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
using MathNet.Numerics;

namespace Cmdty.Storage
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

        public static CmdtyStorage<T>.IAddMinInventory WithTimeAndInventoryVaryingInjectWithdrawRatesPolynomial<T>([NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                            IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges,
                            double newtonRaphsonAccuracy = 1E-10, int newtonRaphsonMaxNumIterations = 100, 
                            int newtonRaphsonSubdivision = 20)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawRanges, 
                            newtonRaphsonAccuracy, newtonRaphsonMaxNumIterations, newtonRaphsonSubdivision);
            return builder.WithInjectWithdrawConstraint(polynomialInjectWithdrawConstraint);
        }


        // TODO think of shorter name
        public static CmdtyStorage<T>.IAddInjectionCost WithTimeAndInventoryVaryingInjectWithdrawRates<T>(
            [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
            [NotNull] IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges,
            InterpolationType interpolationType)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            CmdtyStorage<T>.IAddInjectionCost addInjectionCost;

            switch (interpolationType)
            {
                case InterpolationType.PiecewiseLinearType _:
                    addInjectionCost =
                        WithTimeAndInventoryVaryingInjectWithdrawRatesPiecewiseLinear(builder, injectWithdrawRanges);
                    break;
                case InterpolationType.PolynomialType polynomial:
                    addInjectionCost = WithTimeAndInventoryVaryingInjectWithdrawRatesPolynomial(builder, injectWithdrawRanges, polynomial.NewtonRaphsonAccuracy,
                                                        polynomial.NewtonRaphsonMaxNumIterations, polynomial.NewtonRaphsonSubdivision);
                    break;
                default:
                    throw new ArgumentException($"InterpolationType {interpolationType.GetType().Name} not recognised"); // Shouldn't actually be possible to reach here...
            }

            return addInjectionCost;
        }

        // TODO think of shorter name
        public static CmdtyStorage<T>.IAddInjectionCost WithTimeAndInventoryVaryingInjectWithdrawRatesPolynomial<T>(
                    [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
                    [NotNull] IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges, 
                    double newtonRaphsonAccuracy = 1E-10, int newtonRaphsonMaxNumIterations = 100, int newtonRaphsonSubdivision = 20)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));
            
            IInjectWithdrawConstraint ConstraintFactory(InjectWithdrawRangeByInventory[] injectWithdrawRangeArray) 
                => new PolynomialInjectWithdrawConstraint(injectWithdrawRangeArray, newtonRaphsonAccuracy, newtonRaphsonMaxNumIterations, newtonRaphsonSubdivision);
            
            var addInjectionCost = AddInjectWithdrawRanges(builder, injectWithdrawRanges, ConstraintFactory);

            return addInjectionCost;
        }

        // TODO think of shorter name
        public static CmdtyStorage<T>.IAddInjectionCost WithTimeAndInventoryVaryingInjectWithdrawRatesPiecewiseLinear<T>(
            [NotNull] this CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
            [NotNull] IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges)
            where T : ITimePeriod<T>
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            IInjectWithdrawConstraint ConstraintFactory(InjectWithdrawRangeByInventory[] injectWithdrawRangeArray)
                => new PiecewiseLinearInjectWithdrawConstraint(injectWithdrawRangeArray);

            CmdtyStorage<T>.IAddInjectionCost addInjectionCost = AddInjectWithdrawRanges(builder, injectWithdrawRanges, ConstraintFactory);

            return addInjectionCost;
        }

        private static CmdtyStorage<T>.IAddInjectionCost AddInjectWithdrawRanges<T>(
            CmdtyStorage<T>.IAddInjectWithdrawConstraints builder,
            IEnumerable<InjectWithdrawRangeByInventoryAndPeriod<T>> injectWithdrawRanges, 
            Func<InjectWithdrawRangeByInventory[], IInjectWithdrawConstraint> constraintFactory) where T : ITimePeriod<T>
        {
            var injectWithdrawSortedList = new SortedList<T, IInjectWithdrawConstraint>();
            var inventoryRangeList = new List<InventoryRange>();

            foreach ((T period, IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRange) in injectWithdrawRanges)
            {
                if (period == null)
                    throw new ArgumentException("Null Period in collection.", nameof(injectWithdrawRanges));
                if (injectWithdrawRange == null)
                    throw new ArgumentException("Null InjectWithdrawRanges in collection.", nameof(injectWithdrawRange));
                
                InjectWithdrawRangeByInventory[] injectWithdrawRangeArray = injectWithdrawRange.ToArray();
                if (injectWithdrawRangeArray.Length < 2)
                    throw new ArgumentException($"Period {period} contains less than 2 inject/withdraw/inventory constraints.",
                        nameof(injectWithdrawRanges));

                IInjectWithdrawConstraint constraint;
                if (injectWithdrawRangeArray.Length == 2 &&
                    injectWithdrawRangeArray[0].InjectWithdrawRange.MinInjectWithdrawRate.AlmostEqual(
                        injectWithdrawRangeArray[1].InjectWithdrawRange.MinInjectWithdrawRate, double.Epsilon) &&
                    injectWithdrawRangeArray[0].InjectWithdrawRange.MaxInjectWithdrawRate.AlmostEqual(
                        injectWithdrawRangeArray[1].InjectWithdrawRange.MaxInjectWithdrawRate, double.Epsilon))
                {
                    // Two rows which represent constant inject/withdraw constraints over all inventories
                    constraint = new ConstantInjectWithdrawConstraint(injectWithdrawRangeArray[0].InjectWithdrawRange);
                }
                else
                {
                    constraint = constraintFactory(injectWithdrawRangeArray);
                }

                double minInventory = injectWithdrawRangeArray.Min(inventoryRange => inventoryRange.Inventory);
                double maxInventory = injectWithdrawRangeArray.Max(inventoryRange => inventoryRange.Inventory);

                try
                {
                    injectWithdrawSortedList.Add(period, constraint);
                    inventoryRangeList.Add(new InventoryRange(minInventory, maxInventory));
                }
                catch (ArgumentException) // TODO unit test
                {
                    throw new ArgumentException("Repeated periods found in inject/withdraw ranges.",
                        nameof(injectWithdrawRanges));
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

            var injectWithdrawTimeSeries =
                new TimeSeries<T, IInjectWithdrawConstraint>(firstPeriod, timeSeriesInjectWithdrawValues);
            var inventoryRangeTimeSeries = new TimeSeries<T, InventoryRange>(firstPeriod, timeSeriesInventoryRangeValues);

            IInjectWithdrawConstraint GetInjectWithdrawConstraint(T period)
            {
                if (period.CompareTo(injectWithdrawTimeSeries.End) > 0)
                    return injectWithdrawTimeSeries[injectWithdrawTimeSeries.End];
                return injectWithdrawTimeSeries[period];
            }

            CmdtyStorage<T>.IAddMinInventory
                addMinInventory = builder.WithInjectWithdrawConstraint(GetInjectWithdrawConstraint);

            double GetMinInventory(T period)
            {
                if (period.CompareTo(inventoryRangeTimeSeries.End) > 0)
                    return inventoryRangeTimeSeries[inventoryRangeTimeSeries.End].MinInventory;
                return inventoryRangeTimeSeries[period].MinInventory;
            }

            CmdtyStorage<T>.IAddMaxInventory addMaxInventory = addMinInventory.WithMinInventory(GetMinInventory);

            double GetMaxInventory(T period)
            {
                if (period.CompareTo(inventoryRangeTimeSeries.End) > 0)
                    return inventoryRangeTimeSeries[inventoryRangeTimeSeries.End].MaxInventory;
                return inventoryRangeTimeSeries[period].MaxInventory;
            }

            CmdtyStorage<T>.IAddInjectionCost addInjectionCost = addMaxInventory.WithMaxInventory(GetMaxInventory);
            return addInjectionCost;
        }

    }
}
