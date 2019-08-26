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
using System.Linq;
using Cmdty.TimePeriodValueTypes;
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public sealed class InjectWithdrawRangeByInventoryAndPeriod<T>
        where T : ITimePeriod<T>
    {
        public T Period { get; }
        public IEnumerable<InjectWithdrawRangeByInventory> InjectWithdrawRanges { get; }

        public InjectWithdrawRangeByInventoryAndPeriod(T period, [NotNull] IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
        {
            if (period == null)
                throw new ArgumentNullException(nameof(period));
            Period = period;
            InjectWithdrawRanges = injectWithdrawRanges ?? throw new ArgumentNullException(nameof(injectWithdrawRanges));
        }

        public void Deconstruct(out T period, out IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges)
        {
            period = Period;
            injectWithdrawRanges = InjectWithdrawRanges;
        }

        public static implicit operator InjectWithdrawRangeByInventoryAndPeriod<T>(
            (T period, IEnumerable<InjectWithdrawRangeByInventory> injectWithdrawRanges) tuple)
                => new InjectWithdrawRangeByInventoryAndPeriod<T>(tuple.period, tuple.injectWithdrawRanges);

        public override string ToString()
        {
            return $"{nameof(Period)}: {Period}, {nameof(InjectWithdrawRanges)}.Count: {InjectWithdrawRanges.Count()}";
        }
    }
}