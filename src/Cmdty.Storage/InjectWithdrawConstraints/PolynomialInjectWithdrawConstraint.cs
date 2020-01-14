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
        private readonly double _newtonRaphsonAccuracy;
        private readonly int _newtonRaphsonMaxNumIterations;
        private readonly int _newtonRaphsonSubdivision;

        public PolynomialInjectWithdrawConstraint([NotNull] IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges,
                        double newtonRaphsonAccuracy= 1E-10, int newtonRaphsonMaxNumIterations=100, int newtonRaphsonSubdivision=20)
        {
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            // TODO check in MATH.NET that this preconditions are consistent with their implementation
            if (newtonRaphsonAccuracy < 0)
                throw new ArgumentException("Newton Raphson Accuracy must be positive number.", nameof(newtonRaphsonAccuracy));
            if (newtonRaphsonMaxNumIterations < 2)
                throw new ArgumentException("Newton Raphson max number of iteration must be at least 2.", nameof(newtonRaphsonMaxNumIterations));
            if (newtonRaphsonSubdivision < 1)
                throw new ArgumentException("Newton Raphson subdivision must be at least 1.", nameof(newtonRaphsonSubdivision));

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

            _newtonRaphsonAccuracy = newtonRaphsonAccuracy;
            _newtonRaphsonMaxNumIterations = newtonRaphsonMaxNumIterations;
            _newtonRaphsonSubdivision = newtonRaphsonSubdivision;
        }

        public InjectWithdrawRange GetInjectWithdrawRange(double inventory)
        {
            double maxInjectWithdrawRate = _maxInjectWithdrawPolynomial.Evaluate(inventory);
            double minInjectWithdrawRate = _minInjectWithdrawPolynomial.Evaluate(inventory);
            return new InjectWithdrawRange(minInjectWithdrawRate, maxInjectWithdrawRate);
        }
        
        public double InventorySpaceUpperBound(double nextPeriodInventorySpaceLowerBound,
            double nextPeriodInventorySpaceUpperBound, double currentPeriodMinInventory,
            double currentPeriodMaxInventory, double inventoryPercentLoss)
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

            if (!RobustNewtonRaphson.TryFindRoot(PolyToSolve, PolyToSolve1StDeriv, currentPeriodMinInventory,
                currentPeriodMaxInventory, _newtonRaphsonAccuracy, _newtonRaphsonMaxNumIterations,
                        _newtonRaphsonSubdivision, out double thisPeriodMaxInventory))
            {
                throw new ApplicationException("Cannot solve for the current period inventory space upper bound. Try changing Newton Raphson parameters.");
            }

            if (thisPeriodMaxInventory < currentPeriodMinInventory)// TODO allow tolerance? If so, need to think how this will feed through to other parts of code.
                throw new ApplicationException("Inventory constraints cannot be satisfied.");

            return Math.Min(thisPeriodMaxInventory, currentPeriodMaxInventory);
        }

        public double InventorySpaceLowerBound(double nextPeriodInventorySpaceLowerBound,
            double nextPeriodInventorySpaceUpperBound, double currentPeriodMinInventory,
            double currentPeriodMaxInventory, double inventoryPercentLoss)
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

            if (!RobustNewtonRaphson.TryFindRoot(PolyToSolve, PolyToSolve1StDeriv, currentPeriodMinInventory,
                currentPeriodMaxInventory, _newtonRaphsonAccuracy, _newtonRaphsonMaxNumIterations, 
                        _newtonRaphsonSubdivision, out double thisPeriodMinInventory))
            {
                throw new ApplicationException("Cannot solve for the current period inventory space lower bound. Try changing Newton Raphson parameters.");
            }

            if (thisPeriodMinInventory > currentPeriodMaxInventory) // TODO allow tolerance? If so, need to think how this will feed through to other parts of code.
                throw new ApplicationException("Inventory constraints cannot be satisfied.");

            return Math.Max(thisPeriodMinInventory, currentPeriodMinInventory);
        }

    }
}
