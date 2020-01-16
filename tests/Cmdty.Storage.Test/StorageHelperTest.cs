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
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using Xunit;

namespace Cmdty.Storage.Test
{
    public sealed class StorageHelperTest
    {
        private const double NumericalTolerance = 1E-10;

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeUnconstrained_ReturnsMinAndMaxRateWithZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1010.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 1070.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, 0.0, injectWithdrawRange.MaxInjectWithdrawRate};
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeBothPositiveUnconstrained_ReturnsMinAndMaxRate()
        {
            var injectWithdrawRange = new InjectWithdrawRange(15.5, 65.685);
            const double currentInventory = 1010.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 1070.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                        nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, injectWithdrawRange.MaxInjectWithdrawRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeBothNegativeUnconstrained_ReturnsMinAndMaxRate()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-65.685, -41.5);
            const double currentInventory = 1000.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 950.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                        nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, injectWithdrawRange.MaxInjectWithdrawRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void
            CalculateBangBangDecisionSet_NextStepInventoryConstrainsInjectionAndWithdrawalAroundCurrentInventory_ReturnsAdjustedInjectWithdrawRangeAndZero() // TODO rename!
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1010.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 991.87;
            const double nextStepMaxInventory = 1051.8;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                        nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double expectedWithdrawalRate = nextStepMaxInventory - currentInventory + inventoryLoss; 
            double expectedInjectionRate = nextStepMinInventory - currentInventory + inventoryLoss;
            double[] expectedDecisionSet = new[] { expectedInjectionRate, 0.0, expectedWithdrawalRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_NextStepInventoryConstrainsInjectionLowerThanCurrent_ReturnsArrayWithTwoValuesNoneZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1010.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 900.00;
            const double nextStepMaxInventory = 995.8;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                        nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double expectedWithdrawalRate = injectWithdrawRange.MinInjectWithdrawRate;
            double expectedInjectionRate = nextStepMaxInventory - currentInventory + inventoryLoss;     // Negative injection, i.e. withdrawal
            double[] expectedDecisionSet = new[] { expectedWithdrawalRate, expectedInjectionRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_NextStepInventoryConstrainsWithdrawalHigherThanCurrent_ReturnsArrayWithTwoValuesNoneZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1010.0;
            const double inventoryLoss = 10.0;
            const double nextStepMinInventory = 1001.8;
            const double nextStepMaxInventory= 1009.51;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory, inventoryLoss,
                                    nextStepMinInventory, nextStepMaxInventory, NumericalTolerance);
            double expectedWithdrawalRate = nextStepMaxInventory - currentInventory + inventoryLoss;
            double expectedInjectionRate = nextStepMinInventory - currentInventory + inventoryLoss;     // Negative injection, i.e. withdrawal
            double[] expectedDecisionSet = new[] { expectedInjectionRate, expectedWithdrawalRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        // TODO throws exception if constraints cannot be met

        [Fact]
        public void MaxValueAndIndex_ReturnsMaxValueAndIndex()
        {
            double[] array = {4.5, -1.2, 6.8, 3.2};
            (double maxValue, int indexOfMax) = StorageHelper.MaxValueAndIndex(array);

            Assert.Equal(6.8, maxValue);
            Assert.Equal(2, indexOfMax);
        }

        [Fact]
        public void MaxValueAndIndex_ArrayOfZeroLength_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => StorageHelper.MaxValueAndIndex(new double[0]));
        }

        [Fact]
        public void CalculateInventorySpace_CurrentPeriodAfterStorageStartPeriod_AsExpected()
        {
            const double injectionRate = 5.0;
            const double withdrawalRate = 6.0;
            const double startingInventory = 8.0;

            const double inventoryPercentLoss = 0.03;
            const double minInventory = 0.0;
            const double maxInventory = 23.5;

            var storageStart = new Day(2019, 8, 1);
            var storageEnd = new Day(2019, 8, 28);
            var currentPeriod = new Day(2019, 8,  20);

            var storage = CmdtyStorage<Day>.Builder
                        .WithActiveTimePeriod(storageStart, storageEnd)
                        .WithConstantInjectWithdrawRange(-withdrawalRate, injectionRate)
                        .WithConstantMinInventory(minInventory)
                        .WithConstantMaxInventory(maxInventory)
                        .WithPerUnitInjectionCost(1.5)
                        .WithNoCmdtyConsumedOnInject()
                        .WithPerUnitWithdrawalCost(0.8)
                        .WithNoCmdtyConsumedOnWithdraw()
                        .WithFixedPercentCmdtyInventoryLoss(inventoryPercentLoss)
                        .WithNoCmdtyInventoryCost()
                        .WithTerminalInventoryNpv((cmdtyPrice, inventory) => 0.0)
                        .Build();

            TimeSeries<Day, InventoryRange> inventorySpace =
                        StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            int expectedInventorySpaceCount = storageEnd.OffsetFrom(currentPeriod);
            Assert.Equal(expectedInventorySpaceCount, inventorySpace.Count);

            double expectedInventoryLower = startingInventory * (1 - inventoryPercentLoss) - withdrawalRate;
            double expectedInventoryUpper = startingInventory * (1 - inventoryPercentLoss) + injectionRate;
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 21)], expectedInventoryLower, expectedInventoryUpper);
            
            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 22)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 23)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 24)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 25)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 26)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 27)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 28)], expectedInventoryLower, expectedInventoryUpper);
            
        }



        [Fact]
        public void CalculateInventorySpace_CurrentPeriodBeforeStorageStartPeriod_AsExpected()
        {
            const double injectionRate = 5.0;
            const double withdrawalRate = 6.0;
            const double startingInventory = 11.0;

            const double inventoryPercentLoss = 0.03;
            const double minInventory = 0.0;
            const double maxInventory = 23.5;

            var storageStart = new Day(2019, 8, 19);
            var storageEnd = new Day(2019, 8, 28);
            var currentPeriod = new Day(2019, 8, 10);

            var storage = CmdtyStorage<Day>.Builder
                        .WithActiveTimePeriod(storageStart, storageEnd)
                        .WithConstantInjectWithdrawRange(-withdrawalRate, injectionRate)
                        .WithConstantMinInventory(minInventory)
                        .WithConstantMaxInventory(maxInventory)
                        .WithPerUnitInjectionCost(1.5)
                        .WithNoCmdtyConsumedOnInject()
                        .WithPerUnitWithdrawalCost(0.8)
                        .WithNoCmdtyConsumedOnWithdraw()
                        .WithFixedPercentCmdtyInventoryLoss(inventoryPercentLoss)
                        .WithNoCmdtyInventoryCost()
                        .MustBeEmptyAtEnd()
                        .Build();

            TimeSeries<Day, InventoryRange> inventorySpace =
                        StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            int expectedInventorySpaceCount = storageEnd.OffsetFrom(storageStart);
            Assert.Equal(expectedInventorySpaceCount, inventorySpace.Count);
            
            double expectedInventoryLower = startingInventory * (1 - inventoryPercentLoss) - withdrawalRate;
            double expectedInventoryUpper = startingInventory * (1 - inventoryPercentLoss) + injectionRate;
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 20)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 21)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 22)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 23)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryLower = Math.Max(expectedInventoryLower * (1 - inventoryPercentLoss) - withdrawalRate, minInventory);
            expectedInventoryUpper = Math.Min(expectedInventoryUpper * (1 - inventoryPercentLoss) + injectionRate, maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 24)], expectedInventoryLower, expectedInventoryUpper);

            // At this point the backwardly derived reduced inventory space kicks in so we need to start going backwards in time
            expectedInventoryLower = 0.0;
            expectedInventoryUpper = 0.0;
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 28)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryUpper = Math.Min((expectedInventoryUpper + withdrawalRate) / (1 - inventoryPercentLoss), maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 27)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryUpper = Math.Min((expectedInventoryUpper + withdrawalRate) / (1 - inventoryPercentLoss), maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 26)], expectedInventoryLower, expectedInventoryUpper);

            expectedInventoryUpper = Math.Min((expectedInventoryUpper + withdrawalRate) / (1 - inventoryPercentLoss), maxInventory);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 25)], expectedInventoryLower, expectedInventoryUpper);

        }
        
        private void AssertInventoryRangeEqualsExpected(InventoryRange inventoryRange, 
                            double expectedInventoryLower, double expectedInventoryUpper)
        {
            Assert.Equal(expectedInventoryLower, inventoryRange.MinInventory);
            Assert.Equal(expectedInventoryUpper, inventoryRange.MaxInventory);
        }


    }
}
