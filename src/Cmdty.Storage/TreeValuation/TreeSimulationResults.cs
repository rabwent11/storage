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
    public sealed class TreeSimulationResults<T>
        where T : ITimePeriod<T>
    {
        public double StorageNpv { get; }
        public DoubleTimeSeries<T> DecisionProfile { get; }
        public DoubleTimeSeries<T> CmdtyVolumeConsumed { get; }

        public TreeSimulationResults(double storageNpv, [NotNull] DoubleTimeSeries<T> decisionProfile,
            [NotNull] DoubleTimeSeries<T> cmdtyVolumeConsumed)
        {
            StorageNpv = storageNpv;
            DecisionProfile = decisionProfile ?? throw new ArgumentNullException(nameof(decisionProfile));
            CmdtyVolumeConsumed = cmdtyVolumeConsumed ?? throw new ArgumentNullException(nameof(cmdtyVolumeConsumed));
        }

        public void Deconstruct(out double storageNpv, out DoubleTimeSeries<T> decisionProfile, out DoubleTimeSeries<T> cmdtyVolumeConsumed)
        {
            storageNpv = StorageNpv;
            decisionProfile = DecisionProfile;
            cmdtyVolumeConsumed = CmdtyVolumeConsumed;
        }

        public override string ToString()
        {
            return $"{nameof(StorageNpv)}: {StorageNpv}, {nameof(DecisionProfile)}.Count: {DecisionProfile.Count}, {nameof(CmdtyVolumeConsumed)}.Count: {CmdtyVolumeConsumed.Count}";
        }

    }
}
