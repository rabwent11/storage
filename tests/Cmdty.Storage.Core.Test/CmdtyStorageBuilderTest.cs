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

using Cmdty.TimePeriodValueTypes;
using Xunit;

namespace Cmdty.Storage.Core.Test
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
                                .WithConstantMaxInventory(ConstantMaxInventory)
                                .WithConstantMinInventory(ConstantMinInventory)
                                .WithPerUnitInjectionCost(ConstantInjectionCost)
                                .WithPerUnitWithdrawalCost(ConstantWithdrawalCost)
                                .Build();
            return storage;
        }

    }
}
