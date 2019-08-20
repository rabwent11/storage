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

using System.Collections.Generic;
using Xunit;

namespace Cmdty.Storage.Core.Test
{
    public sealed class PolynomialInjectWithdrawConstraintTest
    {

        [Fact]
        public void InventorySpaceUpperBound_ConstantInjectWithdrawRate_EqualsNextPeriodInventoryPlusMaxWithdrawalRate()
        {
            const double maxInjectionRate = 56.8;
            const double maxWithdrawalRate = 47.12;

            var injectWithdrawRange = new InjectWithdrawRange(-maxWithdrawalRate, maxInjectionRate);

            const double minInventory = 0.0;
            const double maxInventory = 1000.0;
            
            var injectWithdrawalRanges = new List<InjectWithdrawRangeByInventory>
            {
                (inventory: 0.0, injectWithdrawRange),
                (inventory: 100.0, injectWithdrawRange),
                (inventory: 300.0, injectWithdrawRange),
                (inventory: 600.0, injectWithdrawRange),
                (inventory: 800.0, injectWithdrawRange),
                (inventory: 1000.0, injectWithdrawRange),
            };

            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawalRanges);

            const double nextPeriodInventory = 620.0;
            double thisPeriodMaxInventory = polynomialInjectWithdrawConstraint.InventorySpaceUpperBound(nextPeriodInventory, minInventory, maxInventory);

            const double expectedThisPeriodMaxInventory = nextPeriodInventory + maxWithdrawalRate;
            Assert.Equal(expectedThisPeriodMaxInventory, thisPeriodMaxInventory, 12);
        }

        [Fact]
        public void InventorySpaceLowerBound_ConstantInjectWithdrawRate_EqualsNextPeriodInventoryMinusMaxInjectRate()
        {
            const double maxInjectionRate = 56.8;
            const double maxWithdrawalRate = 47.12;

            var injectWithdrawRange = new InjectWithdrawRange(-maxWithdrawalRate, maxInjectionRate);

            const double minInventory = 0.0;
            const double maxInventory = 1000.0;
            
            var injectWithdrawalRanges = new List<InjectWithdrawRangeByInventory>
            {
                (inventory: 0.0, injectWithdrawRange),
                (inventory: 100.0, injectWithdrawRange),
                (inventory: 300.0, injectWithdrawRange),
                (inventory: 600.0, injectWithdrawRange),
                (inventory: 800.0, injectWithdrawRange),
                (inventory: 1000.0, injectWithdrawRange),
            };

            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawalRanges);

            const double nextPeriodInventory = 620.0;
            double thisPeriodMinInventory = polynomialInjectWithdrawConstraint.InventorySpaceLowerBound(nextPeriodInventory, minInventory, maxInventory);

            const double expectedThisPeriodMinInventory = nextPeriodInventory - maxInjectionRate;
            Assert.Equal(expectedThisPeriodMinInventory, thisPeriodMinInventory, 12);
        }

        [Fact]
        public void InventorySpaceUpperBound_InventoryDependentInjectWithdrawRate_ConsistentWithGetInjectWithdrawRange()
        {
            const double minInventory = 0.0;
            const double maxInventory = 1000.0;

            var injectWithdrawalRanges = new List<InjectWithdrawRangeByInventory>
            {
                (inventory: 0.0, (minInjectWithdrawRate: -44.85, maxInjectWithdrawRate: 56.8)), // Inventory empty, highest injection rate
                (inventory: 100.0, (minInjectWithdrawRate: -45.01, maxInjectWithdrawRate: 54.5)),
                (inventory: 300.0, (minInjectWithdrawRate: -45.78, maxInjectWithdrawRate: 52.01)),
                (inventory: 600.0, (minInjectWithdrawRate: -46.17, maxInjectWithdrawRate: 51.9)),
                (inventory: 800.0, (minInjectWithdrawRate: -46.99, maxInjectWithdrawRate: 50.8)),
                (inventory: 1000.0, (minInjectWithdrawRate: -47.12, maxInjectWithdrawRate: 50.01)) // Inventory full, highest withdrawal rate
            };

            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawalRanges);

            const double nextPeriodMaxInventory = 620.0;
            double thisPeriodMaxInventory = polynomialInjectWithdrawConstraint.InventorySpaceUpperBound(nextPeriodMaxInventory, minInventory, maxInventory);
            double thisPeriodMaxWithdrawalRateAtInventory = polynomialInjectWithdrawConstraint
                                            .GetInjectWithdrawRange(thisPeriodMaxInventory).MinInjectWithdrawRate;

            double derivedNextPeriodMaxInventory = thisPeriodMaxInventory + thisPeriodMaxWithdrawalRateAtInventory;
            Assert.Equal(nextPeriodMaxInventory, derivedNextPeriodMaxInventory, 12);
        }

        [Fact]
        public void InventorySpaceLowerBound_InventoryDependentInjectWithdrawRate_ConsistentWithGetInjectWithdrawRange()
        {
            const double minInventory = 0.0;
            const double maxInventory = 1000.0;

            var injectWithdrawalRanges = new List<InjectWithdrawRangeByInventory>
            {
                (inventory: 0.0, (minInjectWithdrawRate: -44.85, maxInjectWithdrawRate: 56.8)), // Inventory empty, highest injection rate
                (inventory: 100.0, (minInjectWithdrawRate: -45.01, maxInjectWithdrawRate: 54.5)),
                (inventory: 300.0, (minInjectWithdrawRate: -45.78, maxInjectWithdrawRate: 52.01)),
                (inventory: 600.0, (minInjectWithdrawRate: -46.17, maxInjectWithdrawRate: 51.9)),
                (inventory: 800.0, (minInjectWithdrawRate: -46.99, maxInjectWithdrawRate: 50.8)),
                (inventory: 1000.0, (minInjectWithdrawRate: -47.12, maxInjectWithdrawRate: 50.01)) // Inventory full, highest withdrawal rate
            };

            var polynomialInjectWithdrawConstraint = new PolynomialInjectWithdrawConstraint(injectWithdrawalRanges);

            const double nextPeriodMinInventory = 552.0;
            double thisPeriodMinInventory = polynomialInjectWithdrawConstraint.InventorySpaceLowerBound(nextPeriodMinInventory, minInventory, maxInventory);
            double thisPeriodMaxInjectRateAtInventory = polynomialInjectWithdrawConstraint
                                            .GetInjectWithdrawRange(thisPeriodMinInventory).MaxInjectWithdrawRate;

            double derivedNextPeriodMinInventory = thisPeriodMinInventory + thisPeriodMaxInjectRateAtInventory;
            Assert.Equal(nextPeriodMinInventory, derivedNextPeriodMinInventory, 12);
        }


        // TODO tests for being bounded by global max and min inventory

    }
}
