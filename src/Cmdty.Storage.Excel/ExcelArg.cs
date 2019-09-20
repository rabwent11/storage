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

namespace Cmdty.Storage.Excel
{
    internal static class ExcelArg
    {
        internal static class InterestRateCurve
        {
            public const string Name = "Ir_curve";
            public const string Description = "Interest rate curve used to discount cash flows to present value, following Act/365 day count and continuous compounding. Any gaps in the curve are linearly interpolated.";
        }

        internal static class ValDate
        {
            public const string Name = "Val_date";
            public const string Description = "Current date assumed for valuation.";
        }

        internal static class StorageStart
        {
            public const string Name = "Storage_start";
            public const string Description = "Date-time on which storage facility first becomes active.";
        }

        internal static class StorageEnd
        {
            public const string Name = "Storage_end";
            public const string Description = "Date-time on which storage facility ceases being active. Injections/withdrawal are only allowed on time periods before the end.";
        }

    }
}
