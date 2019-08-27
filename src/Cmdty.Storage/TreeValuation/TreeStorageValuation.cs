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
using Cmdty.Core.Trees;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public sealed class TreeStorageValuation<T> : ITreeAddStartingInventory<T>, ITreeAddCurrentPeriod<T>, ITreeAddForwardCurve<T>,
        ITreeAddTreeFactory<T>, ITreeAddCmdtySettlementRule<T>, ITreeAddDiscountFactorFunc<T>, 
            ITreeAddSpacing<T>, ITreeAddNumericalTolerance<T>, IAddInterpolatorOrCalculate<T>
        where T : ITimePeriod<T>
    {
        private readonly CmdtyStorage<T> _storage;
        private double _startingInventory;
        private T _currentPeriod;
        private TimeSeries<T, double> _forwardCurve;
        private Func<TimeSeries<T, double>, TimeSeries<T, TreeNode>> _treeFactory;
        private Func<T, Day> _settleDateRule;
        private Func<Day, double> _discountFactors;
        private IDoubleStateSpaceGridCalc _gridCalc;
        private IInterpolatorFactory _interpolatorFactory;
        private double _gridSpacing = 100;
        private double _numericalTolerance;

        private TreeStorageValuation([NotNull] CmdtyStorage<T> storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public static ITreeAddStartingInventory<T> ForStorage([NotNull] CmdtyStorage<T> storage)
        {
            return new TreeStorageValuation<T>(storage);
        }

        ITreeAddCurrentPeriod<T> ITreeAddStartingInventory<T>.WithStartingInventory(double inventory)
        {
            if (inventory < 0)
                throw new ArgumentException("Inventory cannot be negative", nameof(inventory));
            _startingInventory = inventory;
            return this;
        }

        ITreeAddForwardCurve<T> ITreeAddCurrentPeriod<T>.ForCurrentPeriod([NotNull] T currentPeriod)
        {
            if (currentPeriod == null)
                throw new ArgumentNullException(nameof(currentPeriod));
            _currentPeriod = currentPeriod;
            return this;
        }

        ITreeAddTreeFactory<T> ITreeAddForwardCurve<T>.WithForwardCurve([NotNull] TimeSeries<T, double> forwardCurve)
        {
            _forwardCurve = forwardCurve ?? throw new ArgumentNullException(nameof(forwardCurve));
            return this;
        }

        ITreeAddCmdtySettlementRule<T> ITreeAddTreeFactory<T>.WithTreeFactory(
            [NotNull] Func<TimeSeries<T, double>, TimeSeries<T, TreeNode>> treeFactory)
        {
            _treeFactory = treeFactory ?? throw new ArgumentNullException(nameof(treeFactory));
            return this;
        }

        public ITreeAddDiscountFactorFunc<T> WithCmdtySettlementRule([NotNull] Func<T, Day> settleDateRule)
        {
            _settleDateRule = settleDateRule ?? throw new ArgumentNullException(nameof(settleDateRule));
            return this;
        }

        ITreeAddSpacing<T> ITreeAddDiscountFactorFunc<T>.WithDiscountFactorFunc([NotNull] Func<Day, double> discountFactors)
        {
            _discountFactors = discountFactors ?? throw new ArgumentNullException(nameof(discountFactors));
            return this;
        }

        ITreeAddNumericalTolerance<T> ITreeAddSpacing<T>.WithGridSpacing(double gridSpacing)
        {
            if (gridSpacing <= 0.0)
                throw new ArgumentException($"Parameter {nameof(gridSpacing)} value must be positive", nameof(gridSpacing));
            _gridSpacing = gridSpacing;
            return this;
        }

        ITreeAddNumericalTolerance<T> ITreeAddSpacing<T>
                    .WithStateSpaceGridCalculation([NotNull] IDoubleStateSpaceGridCalc gridCalc)
        {
            _gridCalc = gridCalc ?? throw new ArgumentNullException(nameof(gridCalc));
            return this;
        }

        IAddInterpolatorOrCalculate<T> ITreeAddNumericalTolerance<T>.WithNumericalTolerance(double numericalTolerance)
        {
            if (numericalTolerance <= 0)
                throw new ArgumentException("Numerical tolerance must be positive.", nameof(numericalTolerance));
            _numericalTolerance = numericalTolerance;
            return this;
        }

        IAddInterpolatorOrCalculate<T> IAddInterpolatorOrCalculate<T>
                    .WithInterpolatorFactory([NotNull] IInterpolatorFactory interpolatorFactory)
        {
            _interpolatorFactory = interpolatorFactory ?? throw new ArgumentNullException(nameof(interpolatorFactory));
            return this;
        }

        TreeStorageValuationResults<T> IAddInterpolatorOrCalculate<T>.Calculate()
        {
            return Calculate(_currentPeriod, _startingInventory, _forwardCurve, _treeFactory, _storage,
                _settleDateRule, _discountFactors, _gridCalc ?? new FixedSpacingStateSpaceGridCalc(_gridSpacing),
                    _interpolatorFactory ?? new LinearInterpolatorFactory(), _numericalTolerance);
        }

        private static TreeStorageValuationResults<T> Calculate(T currentPeriod, double startingInventory, TimeSeries<T, double> forwardCurve, 
            Func<TimeSeries<T, double>, TimeSeries<T, TreeNode>> treeFactory, CmdtyStorage<T> storage, Func<T, Day> settleDateRule, 
            Func<Day, double> discountFactors, IDoubleStateSpaceGridCalc doubleStateSpaceGridCalc, IInterpolatorFactory interpolatorFactory, 
            double numericalTolerance)
        {


            throw new NotImplementedException();
        }


    }


    public interface ITreeAddStartingInventory<T>
    where T : ITimePeriod<T>
    {
        ITreeAddCurrentPeriod<T> WithStartingInventory(double inventory);
    }

    public interface ITreeAddCurrentPeriod<T>
        where T : ITimePeriod<T>
    {
        ITreeAddForwardCurve<T> ForCurrentPeriod(T currentPeriod);
    }

    public interface ITreeAddForwardCurve<T>
        where T : ITimePeriod<T>
    {
        ITreeAddTreeFactory<T> WithForwardCurve(TimeSeries<T, double> forwardCurve);
    }

    public interface ITreeAddTreeFactory<T>
        where T : ITimePeriod<T>
    {
        /// <summary>
        /// Adds a tree factory function to the valuation.
        /// </summary>
        /// <param name="treeFactory">Function mapping from the forward curve to the price tree.</param>
        ITreeAddCmdtySettlementRule<T>
            WithTreeFactory(Func<TimeSeries<T, double>, TimeSeries<T, TreeNode>> treeFactory);
    }

    public interface ITreeAddCmdtySettlementRule<T>
        where T : ITimePeriod<T>
    {
        /// <summary>
        /// Adds a settlement date rule.
        /// </summary>
        /// <param name="settleDateRule">Function mapping from cmdty delivery date to settlement date.</param>
        ITreeAddDiscountFactorFunc<T> WithCmdtySettlementRule(Func<T, Day> settleDateRule);
    }

    public interface ITreeAddDiscountFactorFunc<T>
        where T : ITimePeriod<T>
    {
        /// <summary>
        /// Adds discount factor function.
        /// </summary>
        /// <param name="discountFactors">Function mapping from cash flow date to discount factor.</param>
        ITreeAddSpacing<T> WithDiscountFactorFunc(Func<Day, double> discountFactors);
    }

    public interface ITreeAddSpacing<T>
        where T : ITimePeriod<T>
    {
        ITreeAddNumericalTolerance<T> WithGridSpacing(double gridSpacing);
        ITreeAddNumericalTolerance<T> WithStateSpaceGridCalculation(IDoubleStateSpaceGridCalc gridCalc);
    }

    public interface ITreeAddNumericalTolerance<T>
        where T : ITimePeriod<T>
    {
        IAddInterpolatorOrCalculate<T> WithNumericalTolerance(double numericalTolerance);
    }

    public interface IAddInterpolatorOrCalculate<T>
        where T : ITimePeriod<T>
    {
        IAddInterpolatorOrCalculate<T> WithInterpolatorFactory(IInterpolatorFactory interpolatorFactory);
        TreeStorageValuationResults<T> Calculate();
    }

}
