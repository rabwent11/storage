#region License
// Copyright (c) 2020 Jake Fowler
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
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public interface ICmdtyStorage<T> where T : ITimePeriod<T>
    {
        bool MustBeEmptyAtEnd { get; }
        T StartPeriod { get; }
        T EndPeriod { get; }
        InjectWithdrawRange GetInjectWithdrawRange(T date, double inventory);
        double MaxInventory(T date);
        double MinInventory(T date);
        IReadOnlyList<DomesticCashFlow> InjectionCost(T date, double inventory, double injectedVolume);
        double CmdtyVolumeConsumedOnInject(T date, double inventory, double injectedVolume);
        IReadOnlyList<DomesticCashFlow> WithdrawalCost(T date, double inventory, double withdrawnVolume);
        double CmdtyVolumeConsumedOnWithdraw(T date, double inventory, double withdrawnVolume);
        double InventorySpaceUpperBound([NotNull] T period, double nextPeriodInventorySpaceLowerBound, double nextPeriodInventorySpaceUpperBound);
        double InventorySpaceLowerBound([NotNull] T period, double nextPeriodInventorySpaceLowerBound, double nextPeriodInventorySpaceUpperBound);
        double TerminalStorageNpv(double cmdtyPrice, double finalInventory);
        double CmdtyInventoryPercentLoss([NotNull] T period);
        IReadOnlyList<DomesticCashFlow> CmdtyInventoryCost([NotNull] T period, double inventory);
    }
}