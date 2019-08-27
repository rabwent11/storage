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
using System.Linq;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public static class IntrinsicStorageValuationExtensions
    {

        public static IIntrinsicAddDiscountFactorFunc<T> WithMonthlySettlement<T>(
                [NotNull] this IIntrinsicAddCmdtySettlementRule<T> intrinsicAddCmdtySettlementRule, TimeSeries<Month, Day> settlementDates)
            where T : ITimePeriod<T>
        {
            if (intrinsicAddCmdtySettlementRule == null) throw new ArgumentNullException(nameof(intrinsicAddCmdtySettlementRule));

            return intrinsicAddCmdtySettlementRule.WithCmdtySettlementRule(deliveryDate =>
            {
                var deliveryMonth = Month.FromDateTime(deliveryDate.Start); // TODO could this not work sometimes? UK Power EFA periods?
                return settlementDates[deliveryMonth];
            });
        }

        public static IIntrinsicNumericalTolerance<T> WithFixedGridSpacing<T>([NotNull] this IIntrinsicAddSpacing<T> intrinsicAddSpacing, double gridSpacing)
            where T : ITimePeriod<T>
        {
            if (intrinsicAddSpacing == null) throw new ArgumentNullException(nameof(intrinsicAddSpacing));
            if (gridSpacing <= 0.0)
                throw new ArgumentException($"Parameter {nameof(gridSpacing)} value must be positive.", nameof(gridSpacing));

            return intrinsicAddSpacing.WithStateSpaceGridCalculation(storage => new FixedSpacingStateSpaceGridCalc(gridSpacing));
        }

        public static IIntrinsicNumericalTolerance<T> WithFixedNumberOfPointsOnGlobalInventoryRange<T>(
                [NotNull] this IIntrinsicAddSpacing<T> intrinsicAddSpacing, int numGridPointsOverGlobalInventoryRange)
            where T : ITimePeriod<T>
        {
            if (intrinsicAddSpacing == null) throw new ArgumentNullException(nameof(intrinsicAddSpacing));
            if (numGridPointsOverGlobalInventoryRange < 3)
                throw new ArgumentException($"Parameter {nameof(numGridPointsOverGlobalInventoryRange)} value must be at least 3.", nameof(numGridPointsOverGlobalInventoryRange));
            
            IDoubleStateSpaceGridCalc GridCalcFactory(CmdtyStorage<T> storage)
            {
                T[] storagePeriods = storage.StartPeriod.EnumerateTo(storage.EndPeriod).ToArray();

                double globalMaxInventory = storagePeriods.Max(period => storage.MaxInventory(period));
                double globalMinInventory = storagePeriods.Min(period => storage.MaxInventory(period));
                double gridSpacing = (globalMaxInventory - globalMinInventory) /
                                     (numGridPointsOverGlobalInventoryRange - 1);
                return new FixedSpacingStateSpaceGridCalc(gridSpacing);
            }

            return intrinsicAddSpacing.WithStateSpaceGridCalculation(GridCalcFactory);
        }

    }
}