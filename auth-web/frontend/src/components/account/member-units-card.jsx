import React from 'react';

export default React.memo(({member}) => (
<div className='card'>
<h6>Unit Membership</h6>
{member
  ? (member.meta||{}).notAMember
    ? null
    : <div>
        {(member.data.attributes.units || []).map(u => <div key={u.unit.id}>{u.unit.name} - {u.status}</div>)}
      </div>
  : <div><i className='fas fa-spin fa-spinner'></i></div>}
</div>));