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
using Cmdty.Core.Trees;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    // TODO find way of not repeating similar code to IntrinsicStorageValuationExtensions. Use T4?
    public static class TreeStorageValuationExtensions
    {
        public static ITreeAddDiscountFactorFunc<T> WithMonthlySettlement<T>(
            [NotNull] this ITreeAddCmdtySettlementRule<T> treeAddCmdtySettlementRule, TimeSeries<Month, Day> settlementDates)
            where T : ITimePeriod<T>
        {
            if (treeAddCmdtySettlementRule == null) throw new ArgumentNullException(nameof(treeAddCmdtySettlementRule));

            return treeAddCmdtySettlementRule.WithCmdtySettlementRule(deliveryDate =>
            {
                var deliveryMonth = Month.FromDateTime(deliveryDate.Start); // TODO could this not work sometimes? UK Power EFA periods?
                return settlementDates[deliveryMonth];
            });
        }

        public static ITreeAddInterpolator<T> WithFixedGridSpacing<T>([NotNull] this ITreeAddInventoryGridCalculation<T> treeAddSpacing, double gridSpacing)
            where T : ITimePeriod<T>
        {
            if (treeAddSpacing == null) throw new ArgumentNullException(nameof(treeAddSpacing));
            if (gridSpacing <= 0.0)
                throw new ArgumentException($"Parameter {nameof(gridSpacing)} value must be positive.", nameof(gridSpacing));

            return treeAddSpacing.WithStateSpaceGridCalculation(storage => new FixedSpacingStateSpaceGridCalc(gridSpacing));
        }

        public static ITreeAddInterpolator<T> WithFixedNumberOfPointsOnGlobalInventoryRange<T>(
                [NotNull] this ITreeAddInventoryGridCalculation<T> treeAddSpacing, int numGridPointsOverGlobalInventoryRange)
            where T : ITimePeriod<T>
        {
            if (treeAddSpacing == null) throw new ArgumentNullException(nameof(treeAddSpacing));
            if (numGridPointsOverGlobalInventoryRange < 3)
                throw new ArgumentException($"Parameter {nameof(numGridPointsOverGlobalInventoryRange)} value must be at least 3.", nameof(numGridPointsOverGlobalInventoryRange));

            IDoubleStateSpaceGridCalc GridCalcFactory(CmdtyStorage<T> storage)
            {
                T[] storagePeriods = storage.StartPeriod.EnumerateTo(storage.EndPeriod).ToArray();

                double globalMaxInventory = storagePeriods.Max(period => storage.MaxInventory(period));
                double globalMinInventory = storagePeriods.Min(period => storage.MinInventory(period));
                double gridSpacing = (globalMaxInventory - globalMinInventory) /
                                     (numGridPointsOverGlobalInventoryRange - 1);
                return new FixedSpacingStateSpaceGridCalc(gridSpacing);
            }

            return treeAddSpacing.WithStateSpaceGridCalculation(GridCalcFactory);
        }

        public static ITreeAddNumericalTolerance<T> WithLinearInventorySpaceInterpolation<T>([NotNull] this ITreeAddInterpolator<T> addInterpolator)
            where T : ITimePeriod<T>
        {
            if (addInterpolator == null) throw new ArgumentNullException(nameof(addInterpolator));
            return addInterpolator.WithInterpolatorFactory(new LinearInterpolatorFactory());
        }

        // TODO unit test for this method. Initial testing showed that maybe should always use linear interpolation on the final step
        /// <summary>
        /// WARNING, TESTING HAS SHOWN THIS METHOD OF INTERPOLATION DOESN'T WORK VERY WELL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="addInterpolator"></param>
        /// <returns></returns>
        public static ITreeAddNumericalTolerance<T> WithCubicSplineInventorySpaceInterpolation<T>([NotNull] this ITreeAddInterpolator<T> addInterpolator)
            where T : ITimePeriod<T>
        {
            if (addInterpolator == null) throw new ArgumentNullException(nameof(addInterpolator));
            return addInterpolator.WithInterpolatorFactory(new NaturalCubicSplineInterpolatorFactory());
        }

        public static ITreeAddCmdtySettlementRule<T> WithOneFactorTrinomialTree<T>(
                [NotNull] this ITreeAddTreeFactory<T> addTreeFactory,
                TimeSeries<T, double> spotVolatilityCurve, double meanReversion, double onePeriodTimeDelta)
            where T : ITimePeriod<T>
        {
            if (addTreeFactory == null) throw new ArgumentNullException(nameof(addTreeFactory));

            return addTreeFactory.WithTreeFactory(forwardCurve => 
                OneFactorTrinomialTree.CreateTree(forwardCurve, meanReversion, spotVolatilityCurve, onePeriodTimeDelta));
        }

        public static ITreeAddCmdtySettlementRule<T> WithIntrinsicTree<T>([NotNull] this ITreeAddTreeFactory<T> addTreeFactory)
            where T : ITimePeriod<T>
        {
            if (addTreeFactory == null) throw new ArgumentNullException(nameof(addTreeFactory));

            TimeSeries<T, IReadOnlyList<TreeNode>> CreateIntrinsicTree(TimeSeries<T, double> forwardCurve)
            {
                var treeNodes = new IReadOnlyList<TreeNode>[forwardCurve.Count];
                treeNodes[forwardCurve.Count - 1] = new []{new TreeNode(forwardCurve[forwardCurve.Count - 1], 1.0, 0, new NodeTransition[0])};

                for (int i = forwardCurve.Count - 2; i >= 0; i--)
                {
                    double forwardPrice = forwardCurve[i];
                    treeNodes[i] = new[] {new TreeNode(forwardPrice, 1.0, 0, 
                        new[] {new NodeTransition(1.0, treeNodes[i + 1][0])})};
                }
                return new TimeSeries<T, IReadOnlyList<TreeNode>>(forwardCurve.Indices, treeNodes);
            }

            return addTreeFactory.WithTreeFactory(CreateIntrinsicTree);
        }

        public static ITreeAddInventoryGridCalculation<T> WithAct365ContinuouslyCompoundedInterestRate<T>(
            [NotNull] this ITreeAddDiscountFactorFunc<T> addDiscountFactorFunc, Func<Day, double> act365ContCompInterestRates)
            where T : ITimePeriod<T>
        {
            if (addDiscountFactorFunc == null) throw new ArgumentNullException(nameof(addDiscountFactorFunc));

            double DiscountFactor(Day presentDay, Day cashFlowDay)
            {
                if (cashFlowDay <= presentDay)
                    return 1.0;
                double interestRate = act365ContCompInterestRates(cashFlowDay);
                return Math.Exp(-cashFlowDay.OffsetFrom(presentDay) / 365.0 * interestRate);
            }

            return addDiscountFactorFunc.WithDiscountFactorFunc(DiscountFactor);
        }
    }
}
