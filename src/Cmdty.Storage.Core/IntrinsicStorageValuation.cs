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

namespace Cmdty.Storage.Core
{
    public sealed class IntrinsicStorageValuation<T> : IAddStartingInventory<T>, IAddCurrentPeriod<T>, IAddForwardCurve<T>, IAddDiscountFactorFunc<T>, IAddSpacing<T>, IAddInterpolatorOrCalculate<T> where T : ITimePeriod<T>
    {
        private CmdtyStorage<T> _storage;
        private double _startingInventory;
        private T _currentPeriod;
        private TimeSeries<T, double> _forwardCurve;
        private Func<T, double> _discountFactors;
        private IDoubleStateSpaceGridCalc _gridCalc;
        private IInterpolatorFactory _interpolatorFactory;
        private double _gridSpacing = 100;

        public IAddStartingInventory<T> ForStorage([NotNull] CmdtyStorage<T> storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            return this;
        }

        IAddCurrentPeriod<T> IAddStartingInventory<T>.WithStartingInventory(double inventory)
        {
            if (inventory < 0)
                throw new ArgumentException("Inventory cannot be negative", nameof(inventory));
            _startingInventory = inventory;
            return this;
        }

        IAddForwardCurve<T> IAddCurrentPeriod<T>.ForCurrentPeriod([NotNull] T currentPeriod)
        {
            if (currentPeriod == null)
                throw new ArgumentNullException(nameof(currentPeriod));
            _currentPeriod = currentPeriod;
            return this;
        }

        IAddDiscountFactorFunc<T> IAddForwardCurve<T>.WithForwardCurve([NotNull] TimeSeries<T, double> forwardCurve)
        {
            _forwardCurve = forwardCurve ?? throw new ArgumentNullException(nameof(forwardCurve));
            return this;
        }

        IAddSpacing<T> IAddDiscountFactorFunc<T>.WithDiscountFactorFunc([NotNull] Func<T, double> discountFactors)
        {
            _discountFactors = discountFactors ?? throw new ArgumentNullException(nameof(discountFactors));
            return this;
        }

        IAddInterpolatorOrCalculate<T> IAddSpacing<T>.WithGridSpacing(double gridSpacing)
        {
            if (gridSpacing <= 0.0)
                throw new ArgumentException($"Parameter {nameof(gridSpacing)} value must be positive", nameof(gridSpacing));
            _gridSpacing = gridSpacing;
            return this;
        }

        IAddInterpolatorOrCalculate<T> IAddSpacing<T>
                    .WithStateSpaceGridCalculation([NotNull] IDoubleStateSpaceGridCalc gridCalc)
        {
            _gridCalc = gridCalc ?? throw new ArgumentNullException(nameof(gridCalc));
            return this;
        }

        IAddInterpolatorOrCalculate<T> IAddInterpolatorOrCalculate<T>
                    .WithInterpolatorFactory([NotNull] IInterpolatorFactory interpolatorFactory)
        {
            _interpolatorFactory = interpolatorFactory ?? throw new ArgumentNullException(nameof(interpolatorFactory));
            return this;
        }

        IntrinsicStorageValuationResults<T> IAddInterpolatorOrCalculate<T>.Calculate()
        {
            return Calculate(_currentPeriod, _startingInventory, _forwardCurve, _storage, _discountFactors,
                    _gridCalc ?? new FixedSpacingStateSpaceGridCalc(_gridSpacing),
                    _interpolatorFactory ?? new LinearInterpolatorFactory());
        }

        private static IntrinsicStorageValuationResults<T> Calculate(T currentPeriod, double startingInventory, TimeSeries<T, double> forwardCurve, 
                    CmdtyStorage<T> storage, Func<T, double> discountFactors, IDoubleStateSpaceGridCalc gridCalc, 
                    IInterpolatorFactory interpolatorFactory)
        {
            // TODO validate inputs

            // TODO return empty results if expired

            TimeSeries<T, InventoryRange> inventorySpace = StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            // Perform backward induction
            var storageValueByInventory = new Func<double, double>[inventorySpace.Count];

            double cmdtyPriceAtEnd = forwardCurve[storage.EndPeriod];
            storageValueByInventory[inventorySpace.Count - 1] = 
                finalInventory => storage.TerminalStorageValue(cmdtyPriceAtEnd, finalInventory) ;

            int backCounter = inventorySpace.Count - 2;
            foreach (T periodLoop in inventorySpace.Indices.Reverse().Skip(1))
            {
                (double inventorySpaceMin, double inventorySpaceMax) = inventorySpace[periodLoop];
                double[] inventorySpaceGrid = gridCalc.GetGridPoints(inventorySpaceMin, inventorySpaceMax)
                                                        .ToArray();
                var storageValuesGrid = new double[inventorySpaceGrid.Length];

                double cmdtyPrice = forwardCurve[periodLoop];
                Func<double, double> continuationValueByInventory = storageValueByInventory[backCounter + 1];
                
                (double nextStepInventorySpaceMin, double nextStepInventorySpaceMax) = inventorySpace[periodLoop.Offset(1)];
                for (int i = 0; i < inventorySpaceGrid.Length; i++)
                {
                    double inventory = inventorySpaceGrid[i];
                    storageValuesGrid[i] = OptimalDecisionAndValue(storage, periodLoop, inventory, nextStepInventorySpaceMin, 
                                                nextStepInventorySpaceMax, cmdtyPrice, continuationValueByInventory).StorageNpv;
                    // TODO think about at discounting and what PV date the values are
                    // TODO save decisions?
                }

                storageValueByInventory[backCounter] =
                    interpolatorFactory.CreateInterpolator(inventorySpaceGrid, storageValuesGrid);
                backCounter--;
            }

            // Loop forward from start inventory choosing optimal decisions
            double storageNpv = 0.0;

            var decisionProfileBuilder = new DoubleTimeSeries<T>.Builder(inventorySpace.Count);
            // TODO remove duplicate evaluation of decision at first step?
            double inventoryLoop = startingInventory;
            for (int i = 0; i < inventorySpace.Count; i++)
            {
                T periodLoop = currentPeriod.Offset(i);
                double cmdtyPrice = forwardCurve[periodLoop];
                Func<double, double> continuationValueByInventory = storageValueByInventory[i];
                (double nextStepInventorySpaceMin, double nextStepInventorySpaceMax) = inventorySpace[periodLoop.Offset(1)];
                (double storageNpvLoop, double optimalInjectWithdraw) = OptimalDecisionAndValue(storage, periodLoop, inventoryLoop, nextStepInventorySpaceMin,
                                        nextStepInventorySpaceMax, cmdtyPrice, continuationValueByInventory);
                decisionProfileBuilder.Add(periodLoop, optimalInjectWithdraw);
                inventoryLoop += optimalInjectWithdraw;
                if (i == 0)
                {
                    storageNpv = storageNpvLoop;
                }
            }

            return new IntrinsicStorageValuationResults<T>(storageNpv, decisionProfileBuilder.Build());
        }

        private static (double StorageNpv, double OptimalInjectWithdraw) OptimalDecisionAndValue(CmdtyStorage<T> storage, T periodLoop, double inventory,
            double nextStepInventorySpaceMin, double nextStepInventorySpaceMax, double cmdtyPrice,
            Func<double, double> continuationValueByInventory)
        {
            InjectWithdrawRange injectWithdrawRange = storage.GetInjectWithdrawRange(periodLoop, inventory);
            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, inventory,
                nextStepInventorySpaceMin, nextStepInventorySpaceMax);
            var valuesForDecision = new double[decisionSet.Length];
            for (var j = 0; j < decisionSet.Length; j++)
            {
                double decisionInjectWithdraw = decisionSet[j];
                valuesForDecision[j] = StorageValueForDecision(storage, periodLoop, inventory,
                    decisionInjectWithdraw, cmdtyPrice, continuationValueByInventory);
            }

            (double storageNpv, int indexOfOptimalDecision) = StorageHelper.MaxValueAndIndex(valuesForDecision);

            return (StorageNpv: storageNpv, OptimalInjectWithdraw: decisionSet[indexOfOptimalDecision]);
        }



        private static double StorageValueForDecision(CmdtyStorage<T> storage, T period, double inventory,
                        double injectWithdrawVolume, double cmdtyPrice, Func<double, double> continuationValueInterpolated)
        {
            double inventoryAfterDecision = inventory + injectWithdrawVolume;
            double continuationFutureValue = continuationValueInterpolated(inventoryAfterDecision);
            // TODO discount future values?

            double injectWithdrawCashFlow = -injectWithdrawVolume * cmdtyPrice;
            // Assumes storage cost is incurred on the day TODO review
            double decisionStorageCost = injectWithdrawVolume > 0.0
                    ? storage.InjectionCost(period, inventory, injectWithdrawVolume, cmdtyPrice)
                    : storage.WithdrawalCost(period, inventory, -injectWithdrawVolume, cmdtyPrice);

            return continuationFutureValue + injectWithdrawCashFlow - decisionStorageCost;
        }
    }

    public interface IAddStartingInventory<T>
        where T : ITimePeriod<T>
    {
        IAddCurrentPeriod<T> WithStartingInventory(double inventory);
    }

    public interface IAddCurrentPeriod<T>
        where T : ITimePeriod<T>
    {
        IAddForwardCurve<T> ForCurrentPeriod(T currentPeriod);
    }

    public interface IAddForwardCurve<T>
        where T : ITimePeriod<T>
    {
        IAddDiscountFactorFunc<T> WithForwardCurve(TimeSeries<T, double> forwardCurve);
    }

    public interface IAddDiscountFactorFunc<T>
        where T : ITimePeriod<T>
    {
        IAddSpacing<T> WithDiscountFactorFunc(Func<T, double> discountFactors);
    }

    public interface IAddSpacing<T>
        where T : ITimePeriod<T>
    {
        IAddInterpolatorOrCalculate<T> WithGridSpacing(double gridSpacing);
        IAddInterpolatorOrCalculate<T> WithStateSpaceGridCalculation(IDoubleStateSpaceGridCalc gridCalc);
    }

    public interface IAddInterpolatorOrCalculate<T>
        where T : ITimePeriod<T>
    {
        IAddInterpolatorOrCalculate<T> WithInterpolatorFactory(IInterpolatorFactory interpolatorFactory);
        IntrinsicStorageValuationResults<T> Calculate();
    }

}
