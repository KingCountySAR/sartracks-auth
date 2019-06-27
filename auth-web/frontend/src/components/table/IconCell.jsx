import React from 'react';

const IconCell = (props) => {
  const { value, faSet, onIcon, offIcon } = props;
   return value ? onIcon ? <i className={`${faSet} ${onIcon}`}></i> : ''
                : offIcon ? <i className={`${faSet} ${offIcon}`}></i> : ''
}

export default IconCell