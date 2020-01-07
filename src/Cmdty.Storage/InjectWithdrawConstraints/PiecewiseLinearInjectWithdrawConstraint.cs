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
using MathNet.Numerics.Interpolation;

namespace Cmdty.Storage
{
    public sealed class PiecewiseLinearInjectWithdrawConstraint : IInjectWithdrawConstraint
    {
        private readonly InjectWithdrawRangeByInventory[] _injectWithdrawRanges;

        private readonly LinearSpline _maxInjectWithdrawLinear;
        private readonly LinearSpline _minInjectWithdrawLinear;

        public PiecewiseLinearInjectWithdrawConstraint([NotNull] IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
        {
            if (injectWithdrawRanges == null) throw new ArgumentNullException(nameof(injectWithdrawRanges));

            _injectWithdrawRanges = injectWithdrawRanges.OrderBy(injectWithdrawRange => injectWithdrawRange.Inventory)
                                                .ToArray();

            if (_injectWithdrawRanges.Length < 2)
                throw new ArgumentException("Inject/withdraw ranges collection must contain at least two elements.", nameof(injectWithdrawRanges));

            double[] inventories = _injectWithdrawRanges.Select(injectWithdrawRange => injectWithdrawRange.Inventory)
                                                        .ToArray();

            double[] maxInjectWithdrawRates = _injectWithdrawRanges
                                                    .Select(injectWithdrawRange => injectWithdrawRange.InjectWithdrawRange.MaxInjectWithdrawRate)
                                                    .ToArray();

            double[] minInjectWithdrawRates = _injectWithdrawRanges
                                                    .Select(injectWithdrawRange => injectWithdrawRange.InjectWithdrawRange.MinInjectWithdrawRate)
                                                    .ToArray();

            _maxInjectWithdrawLinear = LinearSpline.InterpolateSorted(inventories, maxInjectWithdrawRates);
            _minInjectWithdrawLinear = LinearSpline.InterpolateSorted(inventories, minInjectWithdrawRates);

        }

        public InjectWithdrawRange GetInjectWithdrawRange(double inventory)
        {
            double maxInjectWithdrawRate = _maxInjectWithdrawLinear.Interpolate(inventory);
            double minInjectWithdrawRate = _minInjectWithdrawLinear.Interpolate(inventory);
            return new InjectWithdrawRange(minInjectWithdrawRate, maxInjectWithdrawRate);
        }

        public double InventorySpaceUpperBound(double nextPeriodInventorySpaceLowerBound, double nextPeriodInventorySpaceUpperBound,
            double currentPeriodMinInventory, double currentPeriodMaxInventory)
        {
            var currentPeriodInjectWithdrawRangeAtMaxInventory = GetInjectWithdrawRange(currentPeriodMaxInventory);

            double nextPeriodMaxInventoryFromThisPeriodMaxInventory = currentPeriodMaxInventory + currentPeriodInjectWithdrawRangeAtMaxInventory.MaxInjectWithdrawRate;
            double nextPeriodMinInventoryFromThisPeriodMaxInventory = currentPeriodMaxInventory + currentPeriodInjectWithdrawRangeAtMaxInventory.MinInjectWithdrawRate;

            if (nextPeriodMinInventoryFromThisPeriodMaxInventory <= nextPeriodInventorySpaceUpperBound &&
                nextPeriodInventorySpaceLowerBound <= nextPeriodMaxInventoryFromThisPeriodMaxInventory)
            {
                // No need to solve root as next period inventory space can be reached from the current period max inventory
                return currentPeriodMaxInventory;
            }

            // Search for inventory bracket
            // TODO could this be made more efficient using binary search?
            double bracketUpperInventory = _injectWithdrawRanges[_injectWithdrawRanges.Length - 1].Inventory;
            double bracketUpperInventoryAfterWithdraw = nextPeriodMinInventoryFromThisPeriodMaxInventory;
            for (int i = _injectWithdrawRanges.Length - 2; i >= 0; i--)
            {
                var bracketLowerDecisionRange = _injectWithdrawRanges[i];
                double bracketLowerInventory = bracketLowerDecisionRange.Inventory;
                double bracketLowerInventoryAfterWithdraw = bracketLowerInventory +
                                                bracketLowerDecisionRange.InjectWithdrawRange.MinInjectWithdrawRate;

                if (bracketLowerInventoryAfterWithdraw <= nextPeriodInventorySpaceUpperBound &&
                    nextPeriodInventorySpaceUpperBound <= bracketUpperInventoryAfterWithdraw)
                {
                    // Calculate m (gradient) and c (constant) coefficients of linear equation y = mx + c, where x is inventory this period, and y is inventory in the next period after max withdrawal
                    double gradient = (bracketUpperInventoryAfterWithdraw - bracketLowerInventoryAfterWithdraw) /
                                      (bracketUpperInventory - bracketLowerInventory);
                    double constant = bracketLowerInventoryAfterWithdraw - gradient * bracketLowerInventory;

                    // Solve for x, where y know, i.e. x = (y - c) / m
                    double inventorySpaceUpper = (nextPeriodInventorySpaceUpperBound - constant) / gradient;
                    return inventorySpaceUpper;
                }
                
                bracketUpperInventoryAfterWithdraw = bracketLowerInventoryAfterWithdraw;
                bracketUpperInventory = bracketLowerInventory;
            }
            
            throw new ApplicationException("Storage inventory constraints cannot be satisfied.");
        }

        public double InventorySpaceLowerBound(double nextPeriodInventorySpaceLowerBound, double nextPeriodInventorySpaceUpperBound,
            double currentPeriodMinInventory, double currentPeriodMaxInventory)
        {
            throw new NotImplementedException();
        }
    }
}
