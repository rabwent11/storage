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
using Xunit;

namespace Cmdty.Storage.Core.Test
{
    public sealed class NaturalCubicSplineInterpolatorFactoryTest
    {
        [Fact]
        public void CreateInterpolator_ReturnsFunctionWhichEvaluatesToInputs()
        {
            var xCoords = new[] { 1.2, 1.8, 2.5, 8.65 };
            var yCoords = new[] { 46.56, -1.58, 8.556, 12.65 };

            var interpolatorFactor = new NaturalCubicSplineInterpolatorFactory();

            Func<double, double> interpolator = interpolatorFactor.CreateInterpolator(xCoords, yCoords);

            for (int i = 0; i < xCoords.Length; i++)
            {
                double x = xCoords[i];
                double y = interpolator(x);
                double expectedY = yCoords[i];
                Assert.Equal(expectedY, y, 12);
            }

        }

        [Fact]
        public void CreateInterpolator_FlatInputs_ReturnsFunctionWhichEvaluatesToConstantYValue()
        {
            const double constantY = 15.85;
            var xCoords = new[] { 1.2, 1.8, 2.5, 8.65 };
            var yCoords = new[] { constantY, constantY, constantY, constantY };

            var interpolatorFactor = new NaturalCubicSplineInterpolatorFactory();

            Func<double, double> interpolator = interpolatorFactor.CreateInterpolator(xCoords, yCoords);

            var xsToEvaluate = new[] {1.36, 2.15, 5.68, 7.99};

            foreach (double x in xsToEvaluate)
            {
                double y = interpolator(x);
                Assert.Equal(constantY, y);
            }

        }

    }
}
