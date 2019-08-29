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
using JetBrains.Annotations;
using MathNet.Numerics.Interpolation;
// ReSharper disable PossibleMultipleEnumeration

namespace Cmdty.Storage
{
    public sealed class LinearInterpolatorFactory : IInterpolatorFactory  // TODO move to Cmdty.Core
    {
        public Func<double, double> CreateInterpolator([NotNull] IEnumerable<double> xCoords, [NotNull] IEnumerable<double> yCoords)
        {
            if (xCoords == null) throw new ArgumentNullException(nameof(xCoords));
            if (yCoords == null) throw new ArgumentNullException(nameof(yCoords));
            if (xCoords.Count() != yCoords.Count())
                throw new ArgumentException("xCoords and yCoords must have the same number of elements.");

            if (xCoords.Count() == 1) // Trivial case of a single point
            {
                double singleY = yCoords.Single();
                return x => singleY;
            }
            LinearSpline linearSpline = LinearSpline.Interpolate(xCoords, yCoords); // TODO use InterpolateSorted method?
            return linearSpline.Interpolate;
        }

    }
}