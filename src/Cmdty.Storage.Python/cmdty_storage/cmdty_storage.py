# Copyright(c) 2019 Jake Fowler
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

import clr
from System import DateTime
from pathlib import Path
clr.AddReference(str(Path("cmdty_storage/lib/Cmdty.TimePeriodValueTypes")))
from Cmdty.TimePeriodValueTypes import QuarterHour, HalfHour, Hour, Day, Month, Quarter, TimePeriodFactory

clr.AddReference(str(Path('cmdty_storage/lib/Cmdty.Storage')))
from Cmdty.Storage import CmdtyStorage, IBuilder

from collections import namedtuple

ValuationResults = namedtuple('ValuationResults', 'npv, decision_profile')


def intrinsic_storage_val(freq, storage_start, storage_end, constraints, injection_cost, injection_consumption, 
                          withdrawal_cost, withdrawal_consumption, tolerance=None):
    if freq not in FREQ_TO_PERIOD_TYPE:
        raise ValueError("freq parameter value of '{}' not supported. The allowable values can be found in the keys of the dict curves.FREQ_TO_PERIOD_TYPE.".format(freq))

    time_period_type = FREQ_TO_PERIOD_TYPE[freq]

    storage_object = _create_storage_object(time_period_type, storage_start, storage_end, 
                constraints, injection_cost, injection_consumption, withdrawal_cost, withdrawal_consumption, tolerance)

    pass


def from_datetime_like(datetime_like, time_period_type):
    """ Converts either a pandas Period, datetime or date to a .NET Time Period"""

    if (hasattr(datetime_like, 'hour')):
        time_args = (datetime_like.hour, datetime_like.minute, datetime_like.second)
    else:
        time_args = (0, 0, 0)

    date_time = DateTime(datetime_like.year, datetime_like.month, datetime_like.day, *time_args)
    return TimePeriodFactory.FromDateTime[time_period_type](date_time)


def _create_storage_object(time_period_type, storage_start, storage_end, constraints, injection_cost, injection_consumption, 
                          withdrawal_cost, withdrawal_consumption, nr_accuracy=None):

    start_period = from_datetime_like(storage_start, time_period_type)
    end_period = from_datetime_like(storage_end, time_period_type)

    builder = IBuilder[time_period_type](CmdtyStorage[time_period_type].Builder)
    
    builder = builder.WithActiveTimePeriod(start_period, end_period)

    pass


FREQ_TO_PERIOD_TYPE = {
        "15min" : QuarterHour,
        "30min" : HalfHour,
        "H" : Hour,
        "D" : Day,
        "M" : Month,
        "Q" : Quarter
    }
""" dict of str: .NET time period type.
Each item describes an allowable granularity of curves constructed, as specified by the 
freq parameter in the curves public methods.

The keys represent the pandas Offset Alias which describe the granularity, and will generally be used
    as the freq of the pandas Series objects returned by the curve construction methods.
The values are the associated .NET time period types used in behind-the-scenes calculations.
"""
