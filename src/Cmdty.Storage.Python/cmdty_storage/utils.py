# Copyright(c) 2020 Jake Fowler
#
# Permission is hereby granted, free of charge, to any person
# obtaining a copy of this software and associated documentation
# files (the "Software"), to deal in the Software without
# restriction, including without limitation the rights to use, 
# copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the
# Software is furnished to do so, subject to the following
# conditions:
#
# The above copyright notice and this permission notice shall be
# included in all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
# EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
# NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
# HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
# WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
# FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
# OTHER DEALINGS IN THE SOFTWARE.

import pandas as pd
from datetime import datetime
import clr
import System as dotnet
from pathlib import Path
clr.AddReference(str(Path("cmdty_storage/lib/Cmdty.TimePeriodValueTypes")))
import Cmdty.TimePeriodValueTypes as tp
clr.AddReference(str(Path('cmdty_storage/lib/Cmdty.TimeSeries')))
import Cmdty.TimeSeries as ts


def from_datetime_like(datetime_like, time_period_type):
    """ Converts either a pandas Period, datetime or date to a .NET Time Period"""
    if (hasattr(datetime_like, 'hour')):
        time_args = (datetime_like.hour, datetime_like.minute, datetime_like.second)
    else:
        time_args = (0, 0, 0)

    date_time = dotnet.DateTime(datetime_like.year, datetime_like.month, datetime_like.day, *time_args)
    return tp.TimePeriodFactory.FromDateTime[time_period_type](date_time)


def net_datetime_to_py_datetime(net_datetime):
    return datetime(net_datetime.Year, net_datetime.Month, net_datetime.Day, net_datetime.Hour, net_datetime.Minute, net_datetime.Second, net_datetime.Millisecond * 1000)


def net_time_period_to_pandas_period(net_time_period, freq):
    start_datetime = net_datetime_to_py_datetime(net_time_period.Start)
    return pd.Period(start_datetime, freq=freq)


def series_to_double_time_series(series, time_period_type):
    """Converts an instance of pandas Series to a Cmdty.TimeSeries.TimeSeries type with Double data type."""
    return series_to_time_series(series, time_period_type, dotnet.Double, lambda x: x)


def series_to_time_series(series, time_period_type, net_data_type, data_selector):
    """Converts an instance of pandas Series to a Cmdty.TimeSeries.TimeSeries."""
    series_len = len(series)
    net_indices = dotnet.Array.CreateInstance(time_period_type, series_len)
    net_values = dotnet.Array.CreateInstance(net_data_type, series_len)

    for i in range(series_len):
        net_indices[i] = from_datetime_like(series.index[i], time_period_type)
        net_values[i] = data_selector(series.values[i])

    return ts.TimeSeries[time_period_type, net_data_type](net_indices, net_values)


def net_time_series_to_pandas_series(net_time_series, freq):
    """Converts an instance of class Cmdty.TimeSeries.TimeSeries to a pandas Series"""
    curve_start = net_time_series.Indices[0].Start
    curve_start_datetime = net_datetime_to_py_datetime(curve_start)
    index = pd.period_range(start=curve_start_datetime, freq=freq, periods=net_time_series.Count)
    prices = [net_time_series.Data[idx] for idx in range(0, net_time_series.Count)]
    return pd.Series(prices, index)


def is_scalar(arg):
    return isinstance(arg, int) or isinstance(arg, float)


def raise_if_none(arg, error_message):
    if arg is None:
        raise ValueError(error_message)


def raise_if_not_none(arg, error_message):
    if arg is not None:
        raise ValueError(error_message)


FREQ_TO_PERIOD_TYPE = {
        "15min" : tp.QuarterHour,
        "30min" : tp.HalfHour,
        "H" : tp.Hour,
        "D" : tp.Day,
        "M" : tp.Month,
        "Q" : tp.Quarter
    }
""" dict of str: .NET time period type.
Each item describes an allowable granularity of curves constructed, as specified by the 
freq parameter in the curves public methods.

The keys represent the pandas Offset Alias which describe the granularity, and will generally be used
    as the freq of the pandas Series objects returned by the curve construction methods.
The values are the associated .NET time period types used in behind-the-scenes calculations.
"""

