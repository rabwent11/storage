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

namespace Cmdty.Storage.Core.Test
{
    public sealed class StorageHelperTest
    {

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeUnconstrained_ReturnsMinAndMaxRateWithZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 1070.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                nextStepMinInventory, nextStepMaxInventory);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, 0.0, injectWithdrawRange.MaxInjectWithdrawRate};
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeBothPositiveUnconstrained_ReturnsMinAndMaxRate()
        {
            var injectWithdrawRange = new InjectWithdrawRange(15.5, 65.685);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 1070.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                        nextStepMinInventory, nextStepMaxInventory);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, injectWithdrawRange.MaxInjectWithdrawRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_InjectWithdrawRangeBothNegativeUnconstrained_ReturnsMinAndMaxRate()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-65.685, -41.5);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 900.0;
            const double nextStepMaxInventory = 1070.0;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                        nextStepMinInventory, nextStepMaxInventory);
            double[] expectedDecisionSet = new[] { injectWithdrawRange.MinInjectWithdrawRate, injectWithdrawRange.MaxInjectWithdrawRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void
            CalculateBangBangDecisionSet_NextStepInventoryConstrainsInjectionAndWithdrawalAroundCurrentInventory_ReturnsAdjustedInjectWithdrawRangeAndZero() // TODO rename!
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 991.87;
            const double nextStepMaxInventory = 1051.8;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                        nextStepMinInventory, nextStepMaxInventory);
            double expectedWithdrawalRate = nextStepMaxInventory - currentInventory;
            double expectedInjectionRate = nextStepMinInventory - currentInventory;
            double[] expectedDecisionSet = new[] { expectedInjectionRate, 0.0, expectedWithdrawalRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_NextStepInventoryConstrainsInjectionLowerThanCurrent_ReturnsArrayWithTwoValuesNoneZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 900.00;
            const double nextStepMaxInventory = 995.8;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                        nextStepMinInventory, nextStepMaxInventory);
            double expectedWithdrawalRate = injectWithdrawRange.MinInjectWithdrawRate;
            double expectedInjectionRate = nextStepMaxInventory - currentInventory;     // Negative injection, i.e. withdrawal
            double[] expectedDecisionSet = new[] { expectedWithdrawalRate, expectedInjectionRate };
            Assert.Equal(expectedDecisionSet, decisionSet);
        }

        [Fact]
        public void CalculateBangBangDecisionSet_NextStepInventoryConstrainsWithdrawalHigherThanCurrent_ReturnsArrayWithTwoValuesNoneZero()
        {
            var injectWithdrawRange = new InjectWithdrawRange(-15.5, 65.685);
            const double currentInventory = 1000.0;
            const double nextStepMinInventory = 1001.8;
            const double nextStepMaxInventory= 1009.51;

            double[] decisionSet = StorageHelper.CalculateBangBangDecisionSet(injectWithdrawRange, currentInventory,
                                    nextStepMinInventory, nextStepMaxInventory);
            double expectedWithdrawalRate = nextStepMaxInventory - currentInventory;
            double expectedInjectionRate = nextStepMinInventory - currentInventory;     // Negative injection, i.e. withdrawal
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
        public void CalculateInventorySpace_CurrentPeriodAfterStorageStartPeriod_AsExpected() // TODO rename
        {
            const double injectionRate = 5.0;
            const double withdrawalRate = 6.0;
            const double startingInventory = 8.0;

            const double minInventory = 0.0;
            const double maxInventory = 23.5;

            var storageStart = new Day(2019, 8, 1);
            var storageEnd = new Day(2019, 8, 28);
            var currentPeriod = new Day(2019, 8,  20);

            var storage = CmdtyStorage<Day>.Builder
                        .WithActiveTimePeriod(storageStart, storageEnd)
                        .WithConstantInjectWithdrawRange(-withdrawalRate, injectionRate)
                        .WithConstantMaxInventory(maxInventory)
                        .WithConstantMinInventory(minInventory)
                        .WithPerUnitInjectionCost(1.5)
                        .WithPerUnitWithdrawalCost(0.8)
                        .WithTerminalStorageValue((cmdtyPrice, inventory) => 0.0)
                        .Build();

            TimeSeries<Day, InventoryRange> inventorySpace =
                        StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            int expectedInventorySpaceCount = storageEnd.OffsetFrom(currentPeriod);
            Assert.Equal(expectedInventorySpaceCount, inventorySpace.Count);

            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 21)], 2.0, 13.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 22)], 0.0, 18.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 23)], 0.0, 23.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 24)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 25)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 26)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 27)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 28)], 0.0, 23.5);
            
        }

        [Fact]
        public void CalculateInventorySpace_CurrentPeriodBeforeStorageStartPeriod_AsExpected() // TODO rename
        {
            const double injectionRate = 5.0;
            const double withdrawalRate = 6.0;
            const double startingInventory = 11.0;

            const double minInventory = 0.0;
            const double maxInventory = 23.5;

            var storageStart = new Day(2019, 8, 19);
            var storageEnd = new Day(2019, 8, 28);
            var currentPeriod = new Day(2019, 8, 10);

            var storage = CmdtyStorage<Day>.Builder
                        .WithActiveTimePeriod(storageStart, storageEnd)
                        .WithConstantInjectWithdrawRange(-withdrawalRate, injectionRate)
                        .WithConstantMaxInventory(maxInventory)
                        .WithConstantMinInventory(minInventory)
                        .WithPerUnitInjectionCost(1.5)
                        .WithPerUnitWithdrawalCost(0.8)
                        .MustBeEmptyAtEnd()
                        .Build();

            TimeSeries<Day, InventoryRange> inventorySpace =
                        StorageHelper.CalculateInventorySpace(storage, startingInventory, currentPeriod);

            int expectedInventorySpaceCount = storageEnd.OffsetFrom(storageStart);
            Assert.Equal(expectedInventorySpaceCount, inventorySpace.Count);

            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 20)], 5.0, 16.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 21)], 0.0, 21.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 22)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 23)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 24)], 0.0, 23.5);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 25)], 0.0, 18.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 26)], 0.0, 12.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 27)], 0.0, 6.0);
            AssertInventoryRangeEqualsExpected(inventorySpace[new Day(2019, 8, 28)], 0.0, 0.0);

        }


        private void AssertInventoryRangeEqualsExpected(InventoryRange inventoryRange, 
                            double expectedMinInventory, double expectedMaxInventory)
        {
            Assert.Equal(expectedMinInventory, inventoryRange.MinInventory);
            Assert.Equal(expectedMaxInventory, inventoryRange.MaxInventory);
        }


    }
}
