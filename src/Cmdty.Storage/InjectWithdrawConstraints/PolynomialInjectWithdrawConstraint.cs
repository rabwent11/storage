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
using JetBrains.Annotations;
using MathNet.Numerics;
using MathNet.Numerics.RootFinding;

namespace Cmdty.Storage
{
    public sealed class PolynomialInjectWithdrawConstraint : IInjectWithdrawConstraint
    {
        private readonly Polynomial _maxInjectWithdrawPolynomial;
        private readonly Polynomial _minInjectWithdrawPolynomial;

        private readonly Polynomial _maxInjectWithdrawPolynomial1StDeriv;
        private readonly Polynomial _minInjectWithdrawPolynomial1StDeriv;

        public PolynomialInjectWithdrawConstraint([NotNull] IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
        {
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            List<InjectWithdrawRangeByInventory> injectWithdrawRangesList = injectWithdrawRanges.ToList();
            if (injectWithdrawRangesList.Count < 2)
                throw new ArgumentException("At least 2 inject/withdraw constraints must be specified", nameof(injectWithdrawRanges));

            double[] inventories = injectWithdrawRangesList.Select(iwi => iwi.Inventory).ToArray();
            double[] maxInjectWithdrawRates = injectWithdrawRangesList.Select(iwi => iwi.InjectWithdrawRange.MaxInjectWithdrawRate).ToArray();
            double[] minInjectWithdrawRates = injectWithdrawRangesList.Select(iwi => iwi.InjectWithdrawRange.MinInjectWithdrawRate).ToArray();

            // Polynomial of order n can be fitted exactly to n + 1 data points
            int polyOrder = injectWithdrawRangesList.Count - 1;

            _maxInjectWithdrawPolynomial = new Polynomial(Fit.Polynomial(inventories, maxInjectWithdrawRates, polyOrder));
            _maxInjectWithdrawPolynomial1StDeriv = _maxInjectWithdrawPolynomial.Differentiate();

            _minInjectWithdrawPolynomial = new Polynomial(Fit.Polynomial(inventories, minInjectWithdrawRates, polyOrder));
            _minInjectWithdrawPolynomial1StDeriv = _minInjectWithdrawPolynomial.Differentiate();
        }

        public InjectWithdrawRange GetInjectWithdrawRange(double inventory)
        {
            double maxInjectWithdrawRate = _maxInjectWithdrawPolynomial.Evaluate(inventory);
            double minInjectWithdrawRate = _minInjectWithdrawPolynomial.Evaluate(inventory);
            return new InjectWithdrawRange(minInjectWithdrawRate, maxInjectWithdrawRate);
        }
        
        // TODO create base class for finding min and max inventory from arbitrary functional inject/withdraw profiles
        public double InventorySpaceUpperBound(double nextPeriodInventorySpaceLowerBound,
                        double nextPeriodInventorySpaceUpperBound, double currentPeriodMinInventory,
                        double currentPeriodMaxInventory)
        {
            double currentPeriodMaxInjectWithdrawAtMaxInventory = _maxInjectWithdrawPolynomial.Evaluate(currentPeriodMaxInventory);
            double currentPeriodMinInjectWithdrawAtMaxInventory = _minInjectWithdrawPolynomial.Evaluate(currentPeriodMaxInventory);

            double nextPeriodMaxInventoryFromThisPeriodMaxInventory = currentPeriodMaxInventory + currentPeriodMaxInjectWithdrawAtMaxInventory;
            double nextPeriodMinInventoryFromThisPeriodMaxInventory = currentPeriodMaxInventory + currentPeriodMinInjectWithdrawAtMaxInventory;

            if (nextPeriodMinInventoryFromThisPeriodMaxInventory <= nextPeriodInventorySpaceUpperBound &&
                nextPeriodInventorySpaceLowerBound <= nextPeriodMaxInventoryFromThisPeriodMaxInventory)
            {
                // No need to solve root as next period inventory space can be reached from the current period max inventory
                return currentPeriodMaxInventory;
            }
            
            double PolyToSolve(double inventory) => inventory - nextPeriodInventorySpaceUpperBound + _minInjectWithdrawPolynomial.Evaluate(inventory);
            double PolyToSolve1StDeriv(double inventory) => 1 + _minInjectWithdrawPolynomial1StDeriv.Evaluate(inventory);

            // TODO remove hard coding of parameters
            if (!RobustNewtonRaphson.TryFindRoot(PolyToSolve, PolyToSolve1StDeriv, currentPeriodMinInventory,
                currentPeriodMaxInventory, 1E-10, 100, 20, out double thisPeriodMaxInventory))
            {
                throw new ApplicationException("Cannot solve for the current period maximum inventory"); // TODO better exception message
            }

            if (thisPeriodMaxInventory < currentPeriodMinInventory)
                throw new ApplicationException("Cannot solve for the current period maximum inventory");

            return Math.Min(thisPeriodMaxInventory, currentPeriodMaxInventory);
        }

        public double InventorySpaceLowerBound(double nextPeriodInventorySpaceLowerBound,
            double nextPeriodInventorySpaceUpperBound, double currentPeriodMinInventory,
            double currentPeriodMaxInventory)
        {
            double currentPeriodMaxInjectWithdrawAtMinInventory = _maxInjectWithdrawPolynomial.Evaluate(currentPeriodMinInventory);
            double currentPeriodMinInjectWithdrawAtMinInventory = _minInjectWithdrawPolynomial.Evaluate(currentPeriodMinInventory);

            double nextPeriodMaxInventoryFromThisPeriodMinInventory = currentPeriodMinInventory + currentPeriodMaxInjectWithdrawAtMinInventory;
            double nextPeriodMinInventoryFromThisPeriodMinInventory = currentPeriodMinInventory + currentPeriodMinInjectWithdrawAtMinInventory;

            if (nextPeriodMinInventoryFromThisPeriodMinInventory <= nextPeriodInventorySpaceUpperBound &&
                nextPeriodInventorySpaceLowerBound <= nextPeriodMaxInventoryFromThisPeriodMinInventory)
            {
                // No need to solve root as next period inventory space can be reached from the current period min inventory
                return currentPeriodMinInventory;
            }

            double PolyToSolve(double inventory) => inventory - nextPeriodInventorySpaceLowerBound + _maxInjectWithdrawPolynomial.Evaluate(inventory);
            double PolyToSolve1StDeriv(double inventory) => 1 + _maxInjectWithdrawPolynomial1StDeriv.Evaluate(inventory);

            // TODO remove hard coding of parameters
            if (!RobustNewtonRaphson.TryFindRoot(PolyToSolve, PolyToSolve1StDeriv, currentPeriodMinInventory,
                currentPeriodMaxInventory, 1E-12, 100, 20, out double thisPeriodMinInventory))
            {
                throw new ApplicationException("Cannot solve for the current period minimum inventory"); // TODO better exception message
            }

            if (thisPeriodMinInventory > currentPeriodMaxInventory)
                throw new ApplicationException("Cannot solve for the current period minimum inventory");

            return Math.Max(thisPeriodMinInventory, currentPeriodMinInventory);
        }

    }
}
