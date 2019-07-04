import React from 'react';

export default React.memo(({logins}) => (
  <div className='card'>
    <h6>External Logins</h6>
    {logins && logins.data
      ? logins.data.length
        ? logins.data.map(login => <div key={login.id}><i style={{color: login.meta.color}} className={`fab ${login.meta.icon}`} /> {login.attributes.displayName}</div>)
        : <div><i>No external logins</i></div>
      : <div><i className="fas fa-spin fa-spinner"></i></div>}
  </div>));