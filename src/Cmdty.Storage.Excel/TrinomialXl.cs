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
using ExcelDna.Integration;

namespace Cmdty.Storage.Excel
{
    public static class TrinomialXl
    {
        [ExcelFunction(Name = AddIn.ExcelFunctionNamePrefix + nameof(StorageValueTrinomialTree),
            Category = AddIn.ExcelFunctionCategory, IsThreadSafe = true, IsVolatile = false, IsExceptionSafe = true)]
        public static object StorageValueTrinomialTree(
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
            object spotVolatilityCurve,
            double meanReversion,
            object interestRateCurve,
            object numGlobalGridPoints, // TODO excel argument says default is 100
            object numericalTolerance,
            [ExcelArgument(Name = "Granularity")] object granularity)
        {
            return StorageExcelHelper.ExecuteExcelFunction(() =>
                TrinomialStorageValuation<Day>(valuationDate, storageStart, storageEnd, injectWithdrawConstraints,
                    injectionCostRate, cmdtyConsumedOnInjection, withdrawalCostRate,
                    cmdtyConsumedOnWithdrawal, currentInventory, forwardCurve, spotVolatilityCurve, 
                    meanReversion, interestRateCurve, numGlobalGridPoints, numericalTolerance).NetPresentValue);
        }

        private static TreeStorageValuationResults<T> TrinomialStorageValuation<T>(
                            DateTime valuationDateTime,
                            DateTime storageStartDateTime,
                            DateTime storageEndDateTime,
                            object injectWithdrawConstraints,
                            double injectionCostRate,
                            double cmdtyConsumedOnInjection,
                            double withdrawalCostRate,
                            double cmdtyConsumedOnWithdrawal,
                            double currentInventory,
                            object forwardCurveIn,
                            object spotVolatilityCurveIn,
                            double meanReversion,
                            object interestRateCurve,
                            object numGlobalGridPointsIn,
                            object numericalToleranceIn)
            where T : ITimePeriod<T>
        {
            double numericalTolerance = StorageExcelHelper.DefaultIfExcelEmptyOrMissing(numericalToleranceIn, 1E-10,
                            "Numerical_tolerance");

            var storage = StorageExcelHelper.CreateCmdtyStorageFromExcelInputs<T>(storageStartDateTime,
                storageEndDateTime, injectWithdrawConstraints, injectionCostRate, cmdtyConsumedOnInjection,
                withdrawalCostRate, cmdtyConsumedOnWithdrawal, numericalTolerance);

            T currentPeriod = TimePeriodFactory.FromDateTime<T>(valuationDateTime);

            DoubleTimeSeries<T> forwardCurve = StorageExcelHelper.CreateDoubleTimeSeries<T>(forwardCurveIn, "Forward_curve");
            DoubleTimeSeries<T> spotVolatilityCurve = StorageExcelHelper.CreateDoubleTimeSeries<T>(spotVolatilityCurveIn, "Spot_volatility_curve");

            // TODO input settlement dates and use interest rates
            int numGridPoints =
                StorageExcelHelper.DefaultIfExcelEmptyOrMissing<int>(numGlobalGridPointsIn, 100, "Num_global_grid_points");

            double timeDelta = 1.0 / 365.0; // TODO remove this hard coding

            TreeStorageValuationResults<T> valuationResults = TreeStorageValuation<T>
                        .ForStorage(storage)
                        .WithStartingInventory(currentInventory)
                        .ForCurrentPeriod(currentPeriod)
                        .WithForwardCurve(forwardCurve)
                        .WithOneFactorTrinomialTree(spotVolatilityCurve, meanReversion, timeDelta)
                        .WithCmdtySettlementRule(period => period.First<Day>()) // TODO get rid if this
                        .WithDiscountFactorFunc(day => 1.0)
                        .WithFixedNumberOfPointsOnGlobalInventoryRange(numGridPoints)
                        .WithLinearInventorySpaceInterpolation()
                        .WithNumericalTolerance(numericalTolerance)
                        .Calculate();

            return valuationResults;
        }

    }
}
