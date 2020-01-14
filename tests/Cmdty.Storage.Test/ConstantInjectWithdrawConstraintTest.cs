#region License
// Copyright (c) 2020 Jake Fowler
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

using Xunit;

namespace Cmdty.Storage.Test
{
    public sealed class ConstantInjectWithdrawConstraintTest
    {

        [Fact]
        public void InventorySpaceUpperBound_PositiveInventoryLoss_ConsistentWithGetInjectWithdrawRange()
        {
            const double minInventory = 0.0;
            const double maxInventory = 1000.0;
            const double inventoryPercentLoss = 0.03;
            const double minInjectWithdrawRate = -44.85;
            const double maxInjectWithdrawRate = 56.8;

            var injectWithdrawConstraint = new ConstantInjectWithdrawConstraint(minInjectWithdrawRate, maxInjectWithdrawRate);

            const double nextPeriodMinInventory = 320.0;
            const double nextPeriodMaxInventory = 620.0;
            double thisPeriodMaxInventory = injectWithdrawConstraint.InventorySpaceUpperBound(
                nextPeriodMinInventory, nextPeriodMaxInventory, 
                                        minInventory, maxInventory, inventoryPercentLoss);
            
            double thisPeriodMaxWithdrawalRateAtInventory = injectWithdrawConstraint
                                    .GetInjectWithdrawRange(thisPeriodMaxInventory).MinInjectWithdrawRate;

            double derivedNextPeriodMaxInventory = thisPeriodMaxInventory * (1 - inventoryPercentLoss) + thisPeriodMaxWithdrawalRateAtInventory;
            Assert.Equal(nextPeriodMaxInventory, derivedNextPeriodMaxInventory, 12);
        }

        [Fact]
        public void InventorySpaceLowerBound_PositiveInventoryLoss_ConsistentWithGetInjectWithdrawRange()
        {
            const double minInventory = 0.0;
            const double maxInventory = 1000.0;
            const double inventoryPercentLoss = 0.03;
            const double minInjectWithdrawRate = -44.85;
            const double maxInjectWithdrawRate = 56.8;

            var injectWithdrawConstraint = new ConstantInjectWithdrawConstraint(minInjectWithdrawRate, maxInjectWithdrawRate);

            const double nextPeriodMinInventory = 320.0;
            const double nextPeriodMaxInventory = 620.0;
            double thisPeriodMinInventory = injectWithdrawConstraint.InventorySpaceLowerBound(
                nextPeriodMinInventory, nextPeriodMaxInventory, minInventory, maxInventory, inventoryPercentLoss);

            double thisPeriodMaxInjectRateAtInventory = injectWithdrawConstraint
                .GetInjectWithdrawRange(thisPeriodMinInventory).MaxInjectWithdrawRate;

            double derivedNextPeriodMinInventory = thisPeriodMinInventory * (1 - inventoryPercentLoss) + thisPeriodMaxInjectRateAtInventory;
            Assert.Equal(nextPeriodMinInventory, derivedNextPeriodMinInventory, 12);
        }

    }
}
