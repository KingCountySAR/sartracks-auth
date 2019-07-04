import React from 'react';

export default React.memo(({member, token}) => (
<div className='card'>
<h6>Member Information</h6>
{member
  ? (member.meta||{}).notAMember
    ? <div><i>No member found</i></div>
    : <div className='d-flex flex-row'>
        <img className='badge-photo' alt='Member headshot' src={`${window.reactConfig.apis.data.url}/members/${member.data.id}/photo?access_token=${token}`} />
        <div>
          <div className={'wacbar wac_' + member.data.attributes.wacLevel}>{member.data.attributes.wacLevel}</div>
          <div>{member.data.attributes.name}</div>
          <div>ID#: {member.data.attributes.workerNumber}</div>
        </div>
      </div>
  : <div><i className='fas fa-spin fa-spinner'></i></div>}
</div>
));