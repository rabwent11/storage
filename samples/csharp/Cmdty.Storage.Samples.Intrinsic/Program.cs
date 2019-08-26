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

namespace Cmdty.Storage.Samples.Intrinsic
{
    class Program
    {
        private const double ConstantMaxInjectRate = 5.26;
        private const double ConstantMaxWithdrawRate = 14.74;
        private const double ConstantMaxInventory = 1100.74;
        private const double ConstantMinInventory = 0.0;
        private const double ConstantInjectionCost = 0.48;
        private const double ConstantWithdrawalCost = 0.74;

        static void Main(string[] args)
        {
            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                .WithActiveTimePeriod(new Day(2019, 9, 1), new Day(2019, 10, 1))
                .WithConstantInjectWithdrawRange(-ConstantMaxWithdrawRate, ConstantMaxInjectRate)
                .WithConstantMinInventory(ConstantMinInventory)
                .WithConstantMaxInventory(ConstantMaxInventory)
                .WithPerUnitInjectionCost(ConstantInjectionCost, injectionDate => injectionDate)
                .WithNoCmdtyConsumedOnInject()
                .WithPerUnitWithdrawalCost(ConstantWithdrawalCost, withdrawalDate => withdrawalDate)
                .WithNoCmdtyConsumedOnWithdraw()
                .MustBeEmptyAtEnd()
                .Build();

            var currentPeriod = new Day(2019, 9, 15);

            const double lowerForwardPrice = 56.6;
            const double forwardSpread = 87.81;

            double higherForwardPrice = lowerForwardPrice + forwardSpread;

            var forwardCurveBuilder = new TimeSeries<Day, double>.Builder();

            foreach (var day in new Day(2019, 9, 15).EnumerateTo(new Day(2019, 9, 22)))
            {
                forwardCurveBuilder.Add(day, lowerForwardPrice);
            }

            foreach (var day in new Day(2019, 9, 23).EnumerateTo(new Day(2019, 10, 1)))
            {
                forwardCurveBuilder.Add(day, higherForwardPrice);
            }

            const double startingInventory = 50.0;

            IntrinsicStorageValuationResults<Day> valuationResults = IntrinsicStorageValuation<Day>
                .ForStorage(storage)
                .WithStartingInventory(startingInventory)
                .ForCurrentPeriod(currentPeriod)
                .WithForwardCurve(forwardCurveBuilder.Build())
                .WithCmdtySettlementRule(day => day.First<Month>().Offset(1).First<Day>().Offset(5))
                .WithDiscountFactorFunc(day => 1.0)
                .WithGridSpacing(10.0)
                .Calculate();

            Console.WriteLine("Calculated intrinsic storage NPV: " + valuationResults.NetPresentValue.ToString("F2"));
            Console.WriteLine();
            Console.WriteLine("Decision profile:");
            Console.WriteLine(valuationResults.DecisionProfile.FormatData("F2", -1));

            Console.ReadKey();
        }
    }
}
