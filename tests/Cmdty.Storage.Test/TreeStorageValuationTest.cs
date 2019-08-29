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
using System.Collections.Generic;
using Cmdty.TimePeriodValueTypes;
using Cmdty.TimeSeries;
using MathNet.Numerics.Distributions;
using Xunit;

namespace Cmdty.Storage.Test
{
    public sealed class TreeStorageValuationTest
    {

        private static (DoubleTimeSeries<Day> forwardCurve, DoubleTimeSeries<Day> spotVolCurve) CreateDailyTestForwardAndSpotVolCurves(Day start, Day end)
        {
            const double baseForwardPrice = 53.5;
            const double baseSpotVol = 0.78;
            const double forwardSeasonalFactor = 24.6;
            const double spotVolSeasonalFactor = 0.35;

            int numPoints = end.OffsetFrom(start) + 1;

            var days = new Day[numPoints];
            var forwardPrices = new double[numPoints];
            var spotVols = new double[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                days[i] = start.Offset(i);
                forwardPrices[i] = baseForwardPrice + Math.Sin(2.0 * Math.PI / 365.0 * i) * forwardSeasonalFactor;
                spotVols[i] = baseSpotVol + Math.Sin(2.0 * Math.PI / 365.0 * i) * spotVolSeasonalFactor;
            }

            return (forwardCurve: new DoubleTimeSeries<Day>(days, forwardPrices), spotVolCurve: new DoubleTimeSeries<Day>(days, spotVols));
        }

        [Fact]
        public void Calculate_StorageLooksLikeCallOptions_NpvEqualsBlack76()
        {
            const double percentTolerance = 0.005; // 0.5% tolerance
            var currentDate = new Day(2019, 8, 29);

            var storageStart = new Day(2019, 12, 1);
            var storageEnd = new Day(2020, 4, 1);

            var callOption1Date = new Day(2019, 12, 15);
            var callOption2Date = new Day(2020, 1, 20);
            var callOption3Date = new Day(2020, 3, 31);

            const double callOption1Notional = 1200.0;
            const double callOption2Notional = 800.0;
            const double callOption3Notional = 900.0;

            const double storageStartingInventory = callOption1Notional + callOption2Notional + callOption3Notional;

            TimeSeries<Month, Day> settlementDates = new TimeSeries<Month, Day>.Builder()
            {
                { new Month(2019, 12),  new Day(2020, 1, 20)},
                { new Month(2020, 1),  new Day(2020, 2, 18)},
                { new Month(2020, 2),  new Day(2020, 3, 21)},
                { new Month(2020, 3),  new Day(2020, 4, 22)}
            }.Build();

            (DoubleTimeSeries<Day> forwardCurve, DoubleTimeSeries<Day> spotVolCurve) = CreateDailyTestForwardAndSpotVolCurves(currentDate, storageEnd);
            const double meanReversion = 16.5;
            const double timeDelta = 1.0 / 365.0;
            const double interestRate = 0.09;

            // Make all call options at or out of the money so we are only looking at extrinsic value
            double callOption1Strike = forwardCurve[callOption1Date];
            double callOption2Strike = forwardCurve[callOption2Date] + 2.0;
            double callOption3Strike = forwardCurve[callOption3Date] + 2.8;

            var injectWithdrawConstraints = new List<InjectWithdrawRangeByInventoryAndPeriod<Day>>
            {
                (period: storageStart, injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption1Date, injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: -callOption1Notional, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: -callOption1Notional, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption1Date.Offset(1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption2Date, injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: -callOption2Notional, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: -callOption2Notional, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption2Date.Offset(1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption3Date, injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: -callOption3Notional, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: -callOption3Notional, maxInjectWithdrawRate: 0.0)),
                }),
                (period: callOption3Date.Offset(1), injectWithdrawRanges: new List<InjectWithdrawRangeByInventory>
                {
                    (inventory: 0.0, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                    (inventory: storageStartingInventory, (minInjectWithdrawRate: 0.0, maxInjectWithdrawRate: 0.0)),
                })
            };

            IReadOnlyList<DomesticCashFlow> WithdrawalCost(Day withdrawDate, double inventory, double withdrawnVolume)
            {
                double cashFlowAmount = 0.0;
                if (withdrawDate.Equals(callOption1Date))
                    cashFlowAmount = callOption1Strike * withdrawnVolume;
                else if (withdrawDate.Equals(callOption2Date))
                    cashFlowAmount = callOption2Strike * withdrawnVolume;
                else if (withdrawDate.Equals(callOption3Date))
                    cashFlowAmount = callOption3Strike * withdrawnVolume;
                Day cashFlowDate = settlementDates[Month.FromDateTime(withdrawDate.Start)];
                return new[] {new DomesticCashFlow(cashFlowDate, cashFlowAmount)};
            }

            CmdtyStorage<Day> storage = CmdtyStorage<Day>.Builder
                .WithActiveTimePeriod(storageStart, storageEnd)
                .WithTimeAndInventoryVaryingInjectWithdrawRates(injectWithdrawConstraints)
                .WithPerUnitInjectionCost(0.0, injectionDate => injectionDate)
                .WithNoCmdtyConsumedOnInject()
                .WithWithdrawalCost(WithdrawalCost)
                .WithNoCmdtyConsumedOnWithdraw()
                .WithTerminalInventoryNpv((cmdtyPrice, inventory) => 0.0) // Value in storage at end is worthless
                .Build();

            TreeStorageValuationResults<Day> valuationResults = TreeStorageValuation<Day>.ForStorage(storage)
                .WithStartingInventory(storageStartingInventory)
                .ForCurrentPeriod(currentDate)
                .WithForwardCurve(forwardCurve)
                .WithOneFactorTrinomialTree(spotVolCurve, meanReversion, timeDelta)
                .WithMonthlySettlement(settlementDates)
                .WithDiscountFactorFunc(day => Math.Exp(-day.OffsetFrom(currentDate) / 365.0 * interestRate))
                .WithFixedNumberOfPointsOnGlobalInventoryRange(100)
                .WithLinearInventorySpaceInterpolation()
                .WithNumericalTolerance(1E-10)
                .Calculate();

            // Calculate value of equivalent call options
            double callOption1ImpliedVol = OneFactorImpliedVol(currentDate, callOption1Date, spotVolCurve, meanReversion);
            double callOption1Value = Black76CallOptionValue(currentDate, forwardCurve[callOption1Date],
                                          callOption1ImpliedVol, interestRate, callOption1Strike, callOption1Date,
                                          settlementDates[new Month(2019, 12)]) * callOption1Notional;

            double callOption2ImpliedVol = OneFactorImpliedVol(currentDate, callOption2Date, spotVolCurve, meanReversion);
            double callOption2Value = Black76CallOptionValue(currentDate, forwardCurve[callOption2Date],
                                          callOption2ImpliedVol, interestRate, callOption2Strike, callOption2Date,
                                          settlementDates[new Month(2020, 1)]) * callOption2Notional;

            double callOption3ImpliedVol = OneFactorImpliedVol(currentDate, callOption3Date, spotVolCurve, meanReversion);
            double callOption3Value = Black76CallOptionValue(currentDate, forwardCurve[callOption3Date],
                                          callOption3ImpliedVol, interestRate, callOption3Strike, callOption3Date,
                                          settlementDates[new Month(2020, 3)]) * callOption3Notional;

            double expectStorageValue = callOption1Value + callOption2Value + callOption3Value;

            double percentError = (valuationResults.NetPresentValue - expectStorageValue) / expectStorageValue;

            Assert.InRange(percentError, -percentTolerance, percentTolerance);
            
        }

        private static double OneFactorImpliedVol(Day valDate, Day expiryDate, TimeSeries<Day, double> spotVolCurve, double meanReversion)
        {
            double timeToExpiry = (expiryDate - valDate) / 365.0;
            double spotVol = spotVolCurve[expiryDate];

            double oneFactorVariance = (1 - Math.Exp(-2 * meanReversion * timeToExpiry)) / 2.0 / meanReversion;
            double annualisedVariance = oneFactorVariance / timeToExpiry;
            double impliedVol = spotVol * Math.Sqrt(annualisedVariance);
            return impliedVol;
        }

        private static double Black76CallOptionValue(Day valDate, double forwardPrice, double impliedVol, double interestRate,
                                                        double strikePrice, Day expiryDate, Day settlementDate)
        {
            double timeToDiscount = (settlementDate - valDate) / 365.0;
            double discountFactor = Math.Exp(-timeToDiscount * interestRate);

            double timeToExpiry = (expiryDate - valDate) / 365.0;
            double volRootTimeToExpiry = impliedVol * Math.Sqrt(timeToExpiry);

            double d1 = (Math.Log(forwardPrice / strikePrice) + impliedVol * impliedVol / 2 * timeToExpiry) / volRootTimeToExpiry;
            double d2 = d1 - volRootTimeToExpiry;

            double callOptionValue =
                discountFactor * (forwardPrice * Normal.CDF(0, 1, d1) - strikePrice * Normal.CDF(0, 1, d2));

            return callOptionValue;
        }

    }
}
