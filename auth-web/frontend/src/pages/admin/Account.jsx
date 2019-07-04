import React, { Component } from 'react';
import { withRouter } from "react-router-dom";
import { connect } from 'react-redux'
import { Link } from 'react-router-dom';
//import { Button } from 'reactstrap';
import DataTable, { DateCell, IconCell } from '../../components/table/DataTable';
import { actions } from '../../redux/accounts';
import MemberProfileCard from '../../components/account/member-profile-card';
import MemberUnitsCard from '../../components/account/member-units-card';
import AccountLoginsCard from '../../components/account/account-logins-card';
import AccountGroupsCard from '../../components/account/account-groups-card';

import './Account.css';

const AccountInfoCard = React.memo(({data}) => (
  <div className='card'>
                  {!data.attributes ? null : <React.Fragment>
                    <div><strong>{data.attributes.name}</strong> {data.attributes.isLocked ? <i className='fas fa-lock'></i> : null}</div>
                    <div>Username: {data.attributes.userName.startsWith('@')
                         ? <i>Using external login</i>
                         : data.attributes.userName}</div>
                    <div>Email: {data.attributes.email}</div>
                  </React.Fragment>
                  
                  }
                </div>
));

class AccountPage extends Component {
  constructor(props) {
    super(props)
    props.doLoad();
  }

  render() {
    const { details, token } = this.props;

    return <div>
      <h2>Account Detail</h2>
      {details.isLoading
         ? <div><i className='fas fa-spin fa-spinner'></i> Loading ...</div> 
         : <React.Fragment>
            <div className='row'>
              <div className='col-md'>
                <AccountInfoCard data={details.data} />
              </div>
            </div>
            <div className='row'>
              <div className='col-md'>
                <MemberProfileCard member={details.member} token={token}/>
                <MemberUnitsCard member={details.member} />
              </div>
              <div className='col-md'>
                <AccountLoginsCard logins={details.logins} />
              </div>
              <div className='col-md'>
                <AccountGroupsCard groups={details.groups} />
              </div>
            </div>
          </React.Fragment>}
      </div>
  }
}

const storeToProps = (store, ownProps) => {
  return {
    token: store.oidc.user.access_token,
    details: store.accounts.details
  }
}

const dispatchToProps = (dispatch, ownProps) => {
  return {
    doLoad: () => dispatch(actions.loadAccountFull(ownProps.match.params.id)),
    doLoadDetails: (userId, memberId) => dispatch(actions.loadAccountDetails(userId, memberId))
  }
}

export default withRouter(connect(storeToProps, dispatchToProps)(AccountPage));