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

namespace Cmdty.Storage
{
    public interface IAddWithdrawalCost<T> where T : ITimePeriod<T>
    {
        IAddCmdtyConsumedOnWithdraw<T> WithPerUnitWithdrawalCost(double withdrawalCost, Func<T, Day> cashFlowDate);
        /// <summary>
        /// Assumes cash flow date on start Day of decision period
        /// </summary>
        IAddCmdtyConsumedOnWithdraw<T> WithPerUnitWithdrawalCost(double withdrawalCost);
        IAddCmdtyConsumedOnWithdraw<T> WithPerUnitWithdrawalCostTimeSeries(TimeSeries<T, double> perVolumeUnitCostSeries);
        /// <summary>
        /// Adds the withdrawal cost rule.
        /// </summary>
        /// <param name="withdrawalCost">Function mapping from the period, inventory (before withdrawal) and
        /// withdrawn volume to the cost cash flows incurred for withdrawing this volume.</param>
        IAddCmdtyConsumedOnWithdraw<T> WithWithdrawalCost(Func<T, double, double, IReadOnlyList<DomesticCashFlow>> withdrawalCost);
    }
}