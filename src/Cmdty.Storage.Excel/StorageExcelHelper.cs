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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using ExcelDna.Integration;
using MathNet.Numerics.Interpolation;

namespace Cmdty.Storage.Excel
{
    public static class StorageExcelHelper
    {
        public static object ExecuteExcelFunction(Func<object> functionBody)
        {
            if (ExcelDnaUtil.IsInFunctionWizard())
                return "Currently in Function Wizard.";

            try
            {
                return functionBody();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static T DefaultIfExcelEmptyOrMissing<T>(object excelArgument, T defaultValue, string argumentName)
        {
            if (excelArgument is ExcelMissing || excelArgument is ExcelEmpty)
                return defaultValue;

            try
            {
                return (T) Convert.ChangeType(excelArgument, typeof(T));
            }
            catch (Exception)
            {
                string typeName = typeof(T).Name;
                throw new ArgumentException($"Excel argument '{argumentName}' is not of type {typeName}, and cannot be converted to this type.");
            }
        }

        public static CmdtyStorage<T> CreateCmdtyStorageFromExcelInputs<T>(DateTime storageStartDateTime,
            DateTime storageEndDateTime,
            object injectWithdrawConstraintsIn,
            string injectWithdrawInterpolationIn,
            double injectionCostRate,
            double cmdtyConsumedOnInjection,
            double withdrawalCostRate,
            double cmdtyConsumedOnWithdrawal, 
            double newtonRaphsonAccuracy)
            where T : ITimePeriod<T>
        {
            T storageStart = TimePeriodFactory.FromDateTime<T>(storageStartDateTime);
            T storageEnd = TimePeriodFactory.FromDateTime<T>(storageEndDateTime);

            if (injectWithdrawConstraintsIn is ExcelMissing || injectWithdrawConstraintsIn is ExcelEmpty)
                throw new ArgumentException("Inject/withdraw constraints haven't been specified.");

            if (!(injectWithdrawConstraintsIn is object[,] injectWithdrawArray))
                throw new ArgumentException("Inject/withdraw constraints have been incorrectly entered. Argument value should be of a range with 4 columns, the first containing dates, the rest containing numbers.");

            if (injectWithdrawArray.GetLength(1) != 4)
                throw new ArgumentException("Inject/withdraw constraints have been incorrectly entered. Argument value should be a range 4 columns.");
            
            var injectWithdrawGrouped = TakeWhileNotEmptyOrError(injectWithdrawArray).Select((row, i) => new
            {
                period = ObjectToDateTime(row[0], $"Row {i + 1} of inject/withdraw/inventory constrains contains invalid date time in 1st column."),
                inventory = ObjectToDouble(row[1], $"Row {i + 1} of inject/withdraw/inventory constraints contains invalid inventory in 2nd column as is not a number."),
                injectRate = ObjectToDouble(row[2], $"Row {i + 1} of inject/withdraw/inventory constraints contains invalid injection rate in 3rd column as is not a number."),
                withdrawRate = ObjectToDouble(row[3], $"Row {i + 1} of inject/withdraw/inventory constraints contains invalid withdrawal in 4th column as is not a number.")
            }).GroupBy(arg => arg.period);

            var injectWithdrawConstraints = new List<InjectWithdrawRangeByInventoryAndPeriod<T>>();
            foreach (var injectWithdrawGroup in injectWithdrawGrouped)
            {
                T period = TimePeriodFactory.FromDateTime<T>(injectWithdrawGroup.Key);
                IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawByInventory = injectWithdrawGroup.Select(arg =>
                        new InjectWithdrawRangeByInventory(arg.inventory, new InjectWithdrawRange(-arg.withdrawRate, arg.injectRate)));
                injectWithdrawConstraints.Add(new InjectWithdrawRangeByInventoryAndPeriod<T>(period, injectWithdrawByInventory));
            }

            InterpolationType interpolationType;
            if (injectWithdrawInterpolationIn == "PiecewiseLinear")
            {
                interpolationType = InterpolationType.PiecewiseLinear;
            }
            else if (injectWithdrawInterpolationIn == "Polynomial")
            {
                interpolationType = InterpolationType.PolynomialWithParams(newtonRaphsonAccuracy);
            }
            else
            {
                throw new ArgumentException($"Value of Inject_withdraw_interpolation '{injectWithdrawInterpolationIn}' not recognised. Must be either 'PiecewiseLinear' or 'Polynomial'.");
            }

            CmdtyStorage<T> storage = CmdtyStorage<T>.Builder
                    .WithActiveTimePeriod(storageStart, storageEnd)
                    .WithTimeAndInventoryVaryingInjectWithdrawRates(injectWithdrawConstraints, interpolationType)
                    .WithPerUnitInjectionCost(injectionCostRate, injectionDate => injectionDate.First<Day>())
                    .WithFixedPercentCmdtyConsumedOnInject(cmdtyConsumedOnInjection)
                    .WithPerUnitWithdrawalCost(withdrawalCostRate, withdrawalDate => withdrawalDate.First<Day>())
                    .WithFixedPercentCmdtyConsumedOnWithdraw(cmdtyConsumedOnWithdrawal)
                    .WithNoCmdtyInventoryLoss()
                    .WithNoCmdtyInventoryCost()
                    .MustBeEmptyAtEnd()
                    .Build();

            return storage;
        }

        private static IEnumerable<object[]> TakeWhileNotEmptyOrError(object[,] excelInput)
        {
            int numColumns = excelInput.GetLength(1);
            for (int i = 0; i < excelInput.GetLength(0); i++)
            {
                if (excelInput[i, 0] is ExcelEmpty || excelInput[i, 0] is ExcelError)
                    yield break;

                var slice = new object[numColumns];
                for (int j = 0; j < numColumns; j++)
                {
                    slice[j] = excelInput[i, j];
                }

                yield return slice;
            }
        }

        public static DoubleTimeSeries<T> CreateDoubleTimeSeries<T>(object excelValues, string excelArgumentName)
            where T : ITimePeriod<T>
        {
            if (excelValues is ExcelMissing || excelValues is ExcelEmpty)
                throw new ArgumentException(excelArgumentName + " hasn't been specified.");

            if (!(excelValues is object[,] excelValuesArray))
                throw new ArgumentException(excelArgumentName + " has been incorrectly entered. Argument value should be of a range with 2 columns, the first containing dates, the second containing numbers.");

            if (excelValuesArray.GetLength(1) != 2)
                throw new ArgumentException(excelArgumentName + " has been incorrectly entered. Argument value should be a range with 2 columns.");

            var builder = new DoubleTimeSeries<T>.Builder();

            for (int i = 0; i < excelValuesArray.GetLength(0); i++)
            {
                if (excelValuesArray[i, 0] is ExcelEmpty || excelValuesArray[i, 0] is ExcelError)
                    break;

                if (!(excelValuesArray[i, 1] is double doubleValue))
                    throw new ArgumentException($"Value in the second column of row {i} for argument {excelArgumentName} is not a number.");

                DateTime curvePointDateTime = ObjectToDateTime(excelValuesArray[i, 0], $"Cannot create DateTime from value in first row of argument {excelArgumentName}.");
                T curvePointPeriod = TimePeriodFactory.FromDateTime<T>(curvePointDateTime);

                builder.Add(curvePointPeriod, doubleValue);
            }

            return builder.Build();
        }

        public static Func<Day, double> CreateLinearInterpolatedInterestRateFunc(object excelValues, string excelArgumentName)
        {
            if (excelValues is ExcelMissing || excelValues is ExcelEmpty)
                throw new ArgumentException(excelArgumentName + " hasn't been specified.");

            if (!(excelValues is object[,] excelValuesArray))
                throw new ArgumentException(excelArgumentName + " has been incorrectly entered. Argument value should be of a range with 2 columns, the first containing dates, the second containing numbers.");

            if (excelValuesArray.GetLength(1) != 2)
                throw new ArgumentException(excelArgumentName + " has been incorrectly entered. Argument value should be a range with 2 columns.");

            var interestRatePoints = new List<(Day Date, double InterestRate)>();

            for (int i = 0; i < excelValuesArray.GetLength(0); i++)
            {
                if (excelValuesArray[i, 0] is ExcelEmpty || excelValuesArray[i, 0] is ExcelError)
                    break;

                if (!(excelValuesArray[i, 1] is double doubleValue))
                    throw new ArgumentException($"Value in the second column of row {i} for argument {excelArgumentName} is not a number.");

                DateTime curvePointDateTime = ObjectToDateTime(excelValuesArray[i, 0], $"Cannot create DateTime from value in first row of argument {excelArgumentName}.");

                interestRatePoints.Add((Date: Day.FromDateTime(curvePointDateTime), InterestRate: doubleValue));
            }

            if (interestRatePoints.Count < 2)
                throw new ArgumentException(excelArgumentName + " must contain at least two points.");

            Day firstDate = interestRatePoints.Min(curvePoint => curvePoint.Date);
            IEnumerable<double> maturitiesToInterpolate =
                interestRatePoints.Select(curvePoint => curvePoint.Date.OffsetFrom(firstDate) / 365.0);

            LinearSpline linearSpline = LinearSpline.Interpolate(maturitiesToInterpolate, interestRatePoints.Select(curvePoint => curvePoint.InterestRate));

            Day lastDate = interestRatePoints.Max(curvePoint => curvePoint.Date);

            double InterpolatedCurve(Day cashFlowDate)
            {
                if (cashFlowDate < firstDate || cashFlowDate > lastDate)
                    throw new ArgumentException($"Interest rate for future cash flow {cashFlowDate} cannot be interpolated, as this date is outside of the bounds of the input rates curve.");
                double maturity = (cashFlowDate - firstDate) / 365.0;
                return linearSpline.Interpolate(maturity);
            }
            
            return InterpolatedCurve;
        }
        
        private static double ObjectToDouble(object excelNumber, string messageOneFail)
        {
            if (!(excelNumber is double doubleNumber))
                throw new ArgumentException(messageOneFail);
            return doubleNumber;
        }

        private static DateTime ObjectToDateTime(object excelDateTime, string errorMessage)
        {
            if (!(excelDateTime is double doubleValue))
                throw new ArgumentException(errorMessage);

            try
            {
                DateTime dateTime = DateTime.FromOADate(doubleValue);
                return dateTime;
            }
            catch (Exception)
            {
                throw new ArgumentException(errorMessage);
            }

        }

        public static object[,] TimeSeriesToExcelReturnValues<TIndex>(TimeSeries<TIndex, double> timeSeries, bool indicesAsText)
            where TIndex : ITimePeriod<TIndex>
        {
            var resultArray = new object[timeSeries.Count, 2];

            for (int i = 0; i < timeSeries.Count; i++)
            {
                resultArray[i, 0] = indicesAsText ? (object)timeSeries.Indices[i].ToString() : timeSeries.Indices[i].Start;
                resultArray[i, 1] = timeSeries[i];
            }

            return resultArray;
        }

    }
}
