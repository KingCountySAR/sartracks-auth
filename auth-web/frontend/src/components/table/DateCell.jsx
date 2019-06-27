import React from 'react';
import moment from 'moment-timezone';

const DateCell = React.memo(
  (props) => props.value
   ? moment.tz(props.value, 'America/Los_Angeles').calendar()
   : (props.emptyText ? <i>{props.emptyText}</i> : '')
)

export default DateCell