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
    public static class IntrinsicXl
    {
        [ExcelFunction(Name = AddIn.ExcelFunctionNamePrefix + nameof(StorageIntrinsicValue), Category = AddIn.ExcelFunctionCategory, 
            IsThreadSafe = true, IsVolatile = false, IsExceptionSafe = true)]
        public static object StorageIntrinsicValue(
            DateTime valuationDate,
            DateTime storageStart,
            DateTime storageEnd,
            object injectWithdrawConstraints,
            double injectionCostRate,
            double cmdtyConsumedOnInjection,
            double withdrawalCostRate,
            double cmdtyConsumedOnWithdrawal,
            double currentInventory,
            object forwardCurve,
            object interestRateCurve,
            double gridSpacing,
            [ExcelArgument(Name = "Granularity")] object granularity
            )
        {

            return StorageNpv<Day>(valuationDate, storageStart, storageEnd, injectWithdrawConstraints, 
                injectionCostRate, cmdtyConsumedOnInjection, withdrawalCostRate, 
                cmdtyConsumedOnWithdrawal,
                currentInventory, forwardCurve, interestRateCurve, gridSpacing);
        }

        private static double StorageNpv<T>(DateTime valuationDateTime,
            DateTime storageStartDateTime,
            DateTime storageEndDateTime,
            object injectWithdrawConstraints,
            double injectionCostRate,
            double cmdtyConsumedOnInjection,
            double withdrawalCostRate,
            double cmdtyConsumedOnWithdrawal,
            double currentInventory,
            object forwardCurveIn,
            object interestRateCurve,
            double gridSpacing)
            where T : ITimePeriod<T>
        {
            T currentPeriod = TimePeriodFactory.FromDateTime<T>(valuationDateTime);

            DoubleTimeSeries<T> forwardCurve = StorageExcelHelper.CreateDoubleTimeSeries<T>(forwardCurveIn, "Forward_curve");
            
            T storageStart = TimePeriodFactory.FromDateTime<T>(storageStartDateTime);
            T storageEnd = TimePeriodFactory.FromDateTime<T>(storageEndDateTime);

            TimeSeries<Month, Day> settlementDates = new TimeSeries<Month, Day>.Builder
            {
                {new Month(2019, 9),  new Day(2019, 10, 5)}
            }.Build();

            CmdtyStorage<T> storage = CmdtyStorage<T>.Builder
                .WithActiveTimePeriod(storageStart, storageEnd)
                .WithConstantInjectWithdrawRange(-45.5, 56.6)
                .WithConstantMinInventory(0.0)
                .WithConstantMaxInventory(1000.0)
                .WithPerUnitInjectionCost(0.8, injectionDate => injectionDate.First<Day>())
                .WithNoCmdtyConsumedOnInject()
                .WithPerUnitWithdrawalCost(1.2, withdrawalDate => withdrawalDate.First<Day>())
                .WithNoCmdtyConsumedOnWithdraw()
                .MustBeEmptyAtEnd()
                .Build();

            IntrinsicStorageValuationResults<T> valuationResults = IntrinsicStorageValuation<T>
                .ForStorage(storage)
                .WithStartingInventory(currentInventory)
                .ForCurrentPeriod(currentPeriod)
                .WithForwardCurve(forwardCurve)
                .WithMonthlySettlement(settlementDates)
                .WithDiscountFactorFunc(day => 1.0)
                .WithGridSpacing(gridSpacing)
                .Calculate();

            return valuationResults.NetPresentValue;
        }



 
        
    }
}
