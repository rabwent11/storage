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
using JetBrains.Annotations;

namespace Cmdty.Storage
{
    public sealed class IntrinsicStorageValuationResults<T>
        where T : ITimePeriod<T>
    {
        public double NetPresentValue { get; }
        public DoubleTimeSeries<T> DecisionProfile { get; } // TODO decide whether injection is positive or negative number
        public DoubleTimeSeries<T> CmdtyVolumeConsumed { get; } // TODO develop Time Series pane type and include data for DecisionProfile and CmdtyVolumeConsumed in single member of this type

        public IntrinsicStorageValuationResults(double netPresentValue, [NotNull] DoubleTimeSeries<T> decisionProfile,
            [NotNull] DoubleTimeSeries<T> cmdtyVolumeConsumed)
        {
            NetPresentValue = netPresentValue;
            DecisionProfile = decisionProfile ?? throw new ArgumentNullException(nameof(decisionProfile));
            CmdtyVolumeConsumed = cmdtyVolumeConsumed ?? throw new ArgumentNullException(nameof(cmdtyVolumeConsumed));
        }

        public override string ToString()
        {
            return $"{nameof(NetPresentValue)}: {NetPresentValue}, {nameof(DecisionProfile)}.Count = {DecisionProfile.Count}";
        }

        public void Deconstruct(out double netPresentValue, out DoubleTimeSeries<T> decisionProfile)
        {
            netPresentValue = NetPresentValue;
            decisionProfile = DecisionProfile;
        }

    }
}
