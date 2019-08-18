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
using System.Linq;
using Xunit;

namespace Cmdty.Storage.Core.Test
{
    public sealed class FixedSpacingStateSpaceGridCalcTest
    {

        [Fact]
        public void GetGridPoints_StateSpaceLessThanSpacing_ReturnsStateSpaceLowerAndUpperBounds()
        {
            const double spacing = 50.0;
            const double stateSpaceLowerBound = 120.55;
            const double stateSpaceUpperBound = 161.89;

            var gridCalc = new FixedSpacingStateSpaceGridCalc(spacing);

            var gridPoints = gridCalc.GetGridPoints(stateSpaceLowerBound, stateSpaceUpperBound);

            var expectedGridPoints = new[] {stateSpaceLowerBound, stateSpaceUpperBound};
            Assert.Equal(expectedGridPoints, gridPoints);
        }

        [Fact]
        public void GetGridPoints_StateSpaceUpperEqualsLowerPlusSpacing_ReturnsStateSpaceLowerAndUpperBounds()
        {
            const double spacing = 15.0;
            const double stateSpaceLowerBound = 121.2;
            const double stateSpaceUpperBound = stateSpaceLowerBound + spacing;

            var gridCalc = new FixedSpacingStateSpaceGridCalc(spacing);

            var gridPoints = gridCalc.GetGridPoints(stateSpaceLowerBound, stateSpaceUpperBound);

            var expectedGridPoints = new[]
            {
                stateSpaceLowerBound,
                stateSpaceUpperBound
            };
            Assert.Equal(expectedGridPoints, gridPoints);
        }

        [Fact]
        public void GetGridPoints_ReturnsStateSpaceLowerAndUpperBoundsSeparatedBySpacing()
        {
            const double spacing = 15.0;
            const double stateSpaceLowerBound = 121.2;
            const double stateSpaceUpperBound = 174.89;

            var gridCalc = new FixedSpacingStateSpaceGridCalc(spacing);

            var gridPoints = gridCalc.GetGridPoints(stateSpaceLowerBound, stateSpaceUpperBound);

            var expectedGridPoints = new[]
            {
                stateSpaceLowerBound,
                136.2,
                151.2,
                166.2,
                stateSpaceUpperBound,
            };
            Assert.Equal(expectedGridPoints, gridPoints);
        }

        [Fact]
        public void GetGridPoints_StateSpaceLowerBoundEqualToUpperBound_ReturnsSinglePoint()
        {
            const double spacing = 15.0;
            const double stateSpaceLowerBound = 121.2;
            const double stateSpaceUpperBound = stateSpaceLowerBound;

            var gridCalc = new FixedSpacingStateSpaceGridCalc(spacing);

            var gridPoints = gridCalc.GetGridPoints(stateSpaceLowerBound, stateSpaceUpperBound);

            var expectedGridPoints = new[]
            {
                stateSpaceLowerBound
            };
            Assert.Equal(expectedGridPoints, gridPoints);
        }

        [Fact]
        public void GetGridPoints_StateSpaceLowerBoundHigherThanUpperBound_ThrowsArgumentException()
        {
            const double spacing = 15.0;
            const double stateSpaceLowerBound = 121.2;
            const double stateSpaceUpperBound = 121.19;

            var gridCalc = new FixedSpacingStateSpaceGridCalc(spacing);

            Assert.Throws<ArgumentException>(() => 
                                gridCalc.GetGridPoints(stateSpaceLowerBound, stateSpaceUpperBound).ToArray());
        }

        [Fact]
        public void Constructor_NegativeSpacingParameter_ThrowsArgumentException()
        {
            const double spacing = -15.0;
            Assert.Throws<ArgumentException>(() => new FixedSpacingStateSpaceGridCalc(spacing));
        }

        [Fact]
        public void Constructor_ZeroSpacingParameter_ThrowsArgumentException()
        {
            const double spacing = 0.0;
            Assert.Throws<ArgumentException>(() => new FixedSpacingStateSpaceGridCalc(spacing));
        }

    }
}
