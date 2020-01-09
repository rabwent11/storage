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
using Cmdty.TimePeriodValueTypes;
using Xunit;

namespace Cmdty.Storage.Test
{
    public sealed class CmdtyStorageBuilderTest
    {
        private const double ConstantMaxInjectRate = 5.26;
        private const double ConstantMaxWithdrawRate = 14.74;
        private const double ConstantMaxInventory = 1100.74;
        private const double ConstantMinInventory = 50.058;
        private const double ConstantInjectionCost = 0.48;
        private const double ConstantWithdrawalCost = 0.74;


        [Fact]
        public void GetInjectWithdrawRange_ConstantInjectWithdrawRange_HasConstantRates()
        {
            CmdtyStorage<Day> cmdtyStorage = BuildCmdtyStorageWithAllConstantValues();
            // TODO make parameterised
            var date = new Day(2019, 6, 1);
            double inventory = 548.54;
            InjectWithdrawRange injectWithdrawRange = cmdtyStorage.GetInjectWithdrawRange(date, inventory);

            Assert.Equal(ConstantMaxInjectRate, injectWithdrawRange.MaxInjectWithdrawRate);
            Assert.Equal(-ConstantMaxWithdrawRate, injectWithdrawRange.MinInjectWithdrawRate);
        }

        private static CmdtyStorage<Day> BuildCmdtyStorageWithAllConstantValues()
        {
            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                                .WithActiveTimePeriod(new Day(2019, 10, 1), new Day(2019, 11, 1))
                                .WithConstantInjectWithdrawRange(-ConstantMaxWithdrawRate, ConstantMaxInjectRate)
                                .WithConstantMinInventory(ConstantMinInventory)
                                .WithConstantMaxInventory(ConstantMaxInventory)
                                .WithPerUnitInjectionCost(ConstantInjectionCost, injectionDate => injectionDate)
                                .WithNoCmdtyConsumedOnInject()
                                .WithPerUnitWithdrawalCost(ConstantWithdrawalCost, withdrawalDate => withdrawalDate)
                                .WithNoCmdtyConsumedOnWithdraw()
                                .WithNoCmdtyInventoryLoss()
                                .WithNoCmdtyInventoryCost()
                                .MustBeEmptyAtEnd()
                                .Build();
            return storage;
        }

        [Fact]
        public void Build_WithTimeAndInventoryVaryingInjectWithdrawRates_InjectWithdrawRangeAsExpected()
        {
            var injectWithdrawConstraints = new List<InjectWithdrawRangeByInventoryAndPeriod<Day>>
            {
                (period: new Day(2019, 10, 1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                        {
                            (inventory: 0.0, (minInjectWithdrawRate: -44.85, maxInjectWithdrawRate: 56.8)), // Inventory empty, highest injection rate
                            (inventory: 100.0, (minInjectWithdrawRate: -45.01, maxInjectWithdrawRate: 54.5)),
                            (inventory: 300.0, (minInjectWithdrawRate: -45.78, maxInjectWithdrawRate: 52.01)),
                            (inventory: 600.0, (minInjectWithdrawRate: -46.17, maxInjectWithdrawRate: 51.9)),
                            (inventory: 800.0, (minInjectWithdrawRate: -46.99, maxInjectWithdrawRate: 50.8)),
                            (inventory: 1000.0, (minInjectWithdrawRate: -47.12, maxInjectWithdrawRate: 50.01)) // Inventory full, highest withdrawal rate
                        }),
                (period: new Day(2019, 10, 17), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: -130.0, maxInjectWithdrawRate: 133.06)),  // Constant inject/withdraw rates require 2 rows to be provided
                    (inventory: 600.0, (minInjectWithdrawRate: -130.0, maxInjectWithdrawRate: 133.06))
                })
            };

            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                                .WithActiveTimePeriod(new Day(2019, 10, 1), new Day(2019, 11, 1))
                                .WithTimeAndInventoryVaryingInjectWithdrawRatesPolynomial(injectWithdrawConstraints)
                                .WithPerUnitInjectionCost(ConstantInjectionCost, injectionDate => injectionDate)
                                .WithNoCmdtyConsumedOnInject()
                                .WithPerUnitWithdrawalCost(ConstantWithdrawalCost, withdrawalDate => withdrawalDate)
                                .WithNoCmdtyConsumedOnWithdraw()
                                .WithNoCmdtyInventoryLoss()
                                .WithNoCmdtyInventoryCost()
                                .MustBeEmptyAtEnd()
                                .Build();

            // Inject/withdraw on first date
            var injectWithdrawRangeOnFirstDate = storage.GetInjectWithdrawRange(new Day(2019, 10, 1), 100);
            Assert.Equal(-45.01, injectWithdrawRangeOnFirstDate.MinInjectWithdrawRate, 12);
            Assert.Equal(54.5, injectWithdrawRangeOnFirstDate.MaxInjectWithdrawRate, 12);

            // Inject/withdraw between dates (same as on first date)
            var injectWithdrawRangeBetweenDates = storage.GetInjectWithdrawRange(new Day(2019, 10, 16), 100);
            Assert.Equal(-45.01, injectWithdrawRangeBetweenDates.MinInjectWithdrawRate, 12);
            Assert.Equal(54.5, injectWithdrawRangeBetweenDates.MaxInjectWithdrawRate, 12);

            // Inject/withdraw on second date
            var injectWithdrawRangeOnSecondDate = storage.GetInjectWithdrawRange(new Day(2019, 10, 17), 300);
            Assert.Equal(-130.0, injectWithdrawRangeOnSecondDate.MinInjectWithdrawRate, 12);
            Assert.Equal(133.06, injectWithdrawRangeOnSecondDate.MaxInjectWithdrawRate, 12);

            // Inject/withdraw between second date and end
            var injectWithdrawRangeBetweenSecondDateAndEnd = storage.GetInjectWithdrawRange(new Day(2019, 10, 29), 500);
            Assert.Equal(-130.0, injectWithdrawRangeBetweenSecondDateAndEnd.MinInjectWithdrawRate, 12);
            Assert.Equal(133.06, injectWithdrawRangeBetweenSecondDateAndEnd.MaxInjectWithdrawRate, 12);

            // Inject/withdraw after end of storage
            var injectWithdrawRangeAfterEnd = storage.GetInjectWithdrawRange(new Day(2019, 11, 5), 0);
            Assert.Equal(0.0, injectWithdrawRangeAfterEnd.MinInjectWithdrawRate, 12);
            Assert.Equal(0.0, injectWithdrawRangeAfterEnd.MaxInjectWithdrawRate, 12);

        }

        [Fact]
        public void Build_WithTimeAndInventoryVaryingInjectWithdrawRates_MinAndMaxInventoryAsExpected()
        {
            var injectWithdrawConstraints = new List<InjectWithdrawRangeByInventoryAndPeriod<Day>>
            {
                (period: new Day(2019, 10, 1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                        {
                            (inventory: 0.0, (minInjectWithdrawRate: -44.85, maxInjectWithdrawRate: 56.8)), // Inventory empty, highest injection rate
                            (inventory: 100.0, (minInjectWithdrawRate: -45.01, maxInjectWithdrawRate: 54.5)),
                            (inventory: 300.0, (minInjectWithdrawRate: -45.78, maxInjectWithdrawRate: 52.01)),
                            (inventory: 600.0, (minInjectWithdrawRate: -46.17, maxInjectWithdrawRate: 51.9)),
                            (inventory: 800.0, (minInjectWithdrawRate: -46.99, maxInjectWithdrawRate: 50.8)),
                            (inventory: 1000.0, (minInjectWithdrawRate: -47.12, maxInjectWithdrawRate: 50.01)) // Inventory full, highest withdrawal rate
                        }),
                (period: new Day(2019, 10, 17), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: -130.0, maxInjectWithdrawRate: 133.06)),  // Constant inject/withdraw rates require 2 rows to be provided
                    (inventory: 600.0, (minInjectWithdrawRate: -130.0, maxInjectWithdrawRate: 133.06))
                })
            };

            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                                .WithActiveTimePeriod(new Day(2019, 10, 1), new Day(2019, 11, 1))
                                .WithTimeAndInventoryVaryingInjectWithdrawRatesPolynomial(injectWithdrawConstraints)
                                .WithPerUnitInjectionCost(ConstantInjectionCost, injectionDate => injectionDate)
                                .WithNoCmdtyConsumedOnInject()
                                .WithPerUnitWithdrawalCost(ConstantWithdrawalCost, withdrawalDate => withdrawalDate)
                                .WithNoCmdtyConsumedOnWithdraw()
                                .WithNoCmdtyInventoryLoss()
                                .WithNoCmdtyInventoryCost()
                                .MustBeEmptyAtEnd()
                                .Build();

            // Min/max inventory on first date
            double minInventoryOnFirstDate = storage.MinInventory(new Day(2019, 10, 1));
            double maxInventoryOnFirstDate = storage.MaxInventory(new Day(2019, 10, 1));
            Assert.Equal(0.0, minInventoryOnFirstDate, 12);
            Assert.Equal(1000.0, maxInventoryOnFirstDate, 12);

            // Min/max inventory between dates (same as on first date)
            double minInventoryBetweenDates = storage.MinInventory(new Day(2019, 10, 16));
            double maxInventoryBetweenDates = storage.MaxInventory(new Day(2019, 10, 16));
            Assert.Equal(0.0, minInventoryBetweenDates, 12);
            Assert.Equal(1000.0, maxInventoryBetweenDates, 12);

            // Min/max inventory on second date
            double minInventoryOnSecondDate = storage.MinInventory(new Day(2019, 10, 17));
            double maxInventoryOnSecondDate = storage.MaxInventory(new Day(2019, 10, 17));
            Assert.Equal(0.0, minInventoryOnSecondDate, 12);
            Assert.Equal(600.0, maxInventoryOnSecondDate, 12);

            // Min/max inventory between second date and end
            double minInventoryBetweenSecondDateAndEnd = storage.MinInventory(new Day(2019, 10, 29));
            double maxInventoryBetweenSecondDateAndEnd = storage.MaxInventory(new Day(2019, 10, 29));
            Assert.Equal(0.0, minInventoryBetweenSecondDateAndEnd, 12);
            Assert.Equal(600.0, maxInventoryBetweenSecondDateAndEnd, 12);

            // Min/max inventory after end of storage
            double minInventoryAfterEnd = storage.MinInventory(new Day(2019, 11, 5));
            double maxInventoryAfterEnd = storage.MaxInventory(new Day(2019, 11, 5));
            Assert.Equal(0.0, minInventoryAfterEnd, 12);
            Assert.Equal(0.0, maxInventoryAfterEnd, 12);

        }



    }
}
