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
using Cmdty.Storage.Core;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using ExcelDna.Integration;

namespace Cmdty.Storage.Excel
{
    public static class StorageXl
    {
        [ExcelFunction(Name = "cmdty.HelloStorage", Category = "Cmdty.Storage", IsThreadSafe = true, 
            IsVolatile = false, IsExceptionSafe = true)]
        public static object HelloStorage()
        {
            var currentPeriod = new Day(2019, 9, 15);

            var forwardCurveBuilder = new TimeSeries<Day, double>.Builder();

            foreach (var day in new Day(2019, 9, 15).EnumerateTo(new Day(2019, 9, 22)))
            {
                forwardCurveBuilder.Add(day, 56.6);
            }

            foreach (var day in new Day(2019, 9, 23).EnumerateTo(new Day(2019, 9, 30)))
            {
                forwardCurveBuilder.Add(day, 61.8);
            }

            var storageStart = new Day(2019, 9, 1);
            var storageEnd = new Day(2019, 9, 30);

            TimeSeries<Month, Day> settlementDates = new TimeSeries<Month, Day>.Builder
            {
                {new Month(2019, 9),  new Day(2019, 10, 5)}
            }.Build();

            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                .WithActiveTimePeriod(storageStart, storageEnd)
                .WithConstantInjectWithdrawRange(-45.5, 56.6)
                .WithConstantMinInventory(0.0)
                .WithConstantMaxInventory(1000.0)
                .WithPerUnitInjectionCost(0.8, injectionDate => injectionDate)
                .WithNoCmdtyConsumedOnInject()
                .WithPerUnitWithdrawalCost(1.2, withdrawalDate => withdrawalDate)
                .WithNoCmdtyConsumedOnWithdraw()
                .MustBeEmptyAtEnd()
                .Build();

            IntrinsicStorageValuationResults<Day> valuationResults = IntrinsicStorageValuation<Day>
                .ForStorage(storage)
                .WithStartingInventory(0.0)
                .ForCurrentPeriod(currentPeriod)
                .WithForwardCurve(forwardCurveBuilder.Build())
                .WithMonthlySettlement(settlementDates)
                .WithDiscountFactorFunc(day => 1.0)
                .WithGridSpacing(10.0)
                .Calculate();

            return valuationResults.NetPresentValue;
            //return "Hello Storage!";
        }


    }
}
