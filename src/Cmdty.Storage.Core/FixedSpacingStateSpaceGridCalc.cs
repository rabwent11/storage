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

namespace Cmdty.Storage.Core
{
    public sealed class FixedSpacingStateSpaceGridCalc : IDoubleStateSpaceGridCalc // TODO move to Cmdty.Core
    {
        public double Spacing { get; }

        public FixedSpacingStateSpaceGridCalc(double spacing)
        {
            if (spacing <= 0.0)
                throw new ArgumentException("Parameter must be positive", nameof(spacing));
            Spacing = spacing;
        }
        
        public IEnumerable<double> GetGridPoints(double stateSpaceLowerBound, double stateSpaceUpperBound)
        {
            if (stateSpaceLowerBound > stateSpaceUpperBound)
                throw new ArgumentException($"Parameter {nameof(stateSpaceLowerBound)} value cannot be above parameter {nameof(stateSpaceUpperBound)} value");
            
            yield return stateSpaceLowerBound;

            if (stateSpaceLowerBound < stateSpaceUpperBound)
            {
                double gridPoint = stateSpaceLowerBound;
                do
                {
                    gridPoint += Spacing;
                    double yieldValue = Math.Min(gridPoint, stateSpaceUpperBound);
                    yield return yieldValue;
                } while (gridPoint < stateSpaceUpperBound);
            }

        }

    }
}